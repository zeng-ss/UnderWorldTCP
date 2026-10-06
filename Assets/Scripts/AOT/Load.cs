using HybridCLR;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 启动流程：
///   1. 初始化 → 2. 检查 catalog 更新 → 3. 加载热更窗口 → 4. 计算下载大小并下载
///   → 5. 加载 AOT 程序集元数据（HybridCLR）→ 6. 加载热更程序集 → 7. 实例化 GameLanuch 进入游戏
/// </summary>
public class Load : MonoBehaviour
{
    [Header("热更新窗口")] private IHotUpdateWindow _hotUpdateWindow;

    private const string DllConfigKey = "DllConfig";
    private const string HotUpdateWindowKey = "HotUpdateWindow";
    private const string GameLanuchKey = "GameLanuch";
    private const string PreloadLabel = "preload";

    private readonly HashSet<string> _loadedDlls = new();
    private DllConfig _dllConfig;

    private void Start()
    {
        StartCoroutine(HotUpdate());
    }

    private IEnumerator HotUpdate()
    {
        // 1. 初始化 Addressables
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;
        //Addressables.Release(initHandle);
        Debug.Log("Addressables 初始化完成");

#if UNITY_EDITOR
        // 编辑器：Use Asset Database 模式直接跑源代码，无需下载与加载 DLL
        yield return EnterGame();
        yield break;
#endif
        // 2. 检查 catalog 更新
        yield return CheckForCatalogUpdates();

        // 3. 加载热更窗口（展示下载进度）
        yield return LoadHotUpdateWindow();

        // 4. 计算下载大小并下载全部待更新资源
        var sizeHandle = Addressables.GetDownloadSizeAsync(PreloadLabel);
        yield return sizeHandle;
        long totalBytes = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        if (totalBytes > 0)
        {
            Debug.Log($"开始下载，共 {totalBytes / (1024f * 1024f):F1}M ...");
            _hotUpdateWindow?.Show(totalBytes, null);

            var downloadHandle =
                Addressables.DownloadDependenciesAsync(PreloadLabel, Addressables.MergeMode.Union, false);
            while (!downloadHandle.IsDone)
            {
                var status = downloadHandle.GetDownloadStatus();
                _hotUpdateWindow?.UpdateDownloadProgress(status.Percent);
                _hotUpdateWindow?.UpdateDownloadBytes(status.DownloadedBytes);
                yield return null;
            }

            if (downloadHandle.Status == AsyncOperationStatus.Failed)
                Debug.LogError($"资源下载失败: {downloadHandle.OperationException}");
            Addressables.Release(downloadHandle);
        }
        else
        {
            Debug.Log("没有资源更新，直接进入游戏");
            _hotUpdateWindow?.RefreshUI(1f, "没有资源更新");
        }

        // 5. 加载 AOT 程序集元数据（HybridCLR）
        LoadMetadataForAOTAssemblies();

        // 6. 加载热更程序集
        LoadHotUpdateAssemblies();

        // 7. 进入游戏
        yield return EnterGame();
    }

    /// <summary>检查 catalog 是否有新版本，有则拉取更新</summary>
    private IEnumerator CheckForCatalogUpdates()
    {
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"CheckForCatalogUpdates 失败: {checkHandle.OperationException}");
        }
        else
        {
            List<string> catalogs = checkHandle.Result;
            if (catalogs.Count > 0)
            {
                var updateHandle = Addressables.UpdateCatalogs(catalogs);
                yield return updateHandle;
                Addressables.Release(updateHandle);
                Debug.Log("catalog 更新完成");
            }
            else
            {
                Debug.Log("catalog 已是最新，无需更新");
            }
        }

        Addressables.Release(checkHandle);
    }

    private IEnumerator LoadHotUpdateWindow()
    {
        var handle = Addressables.InstantiateAsync(HotUpdateWindowKey);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
            _hotUpdateWindow = handle.Result.GetComponent<IHotUpdateWindow>();
        else
            Debug.LogError($"加载热更窗口失败: {handle.OperationException}");
    }

    /// <summary>加载 AOT 程序集元数据，为 HybridCLR 补齐跨程序集引用（仅真机）</summary>
    private void LoadMetadataForAOTAssemblies()
    {
        EnsureDllConfig();
        foreach (string dllName in _dllConfig.aot)
        {
            byte[] dllBytes = LoadDllBytes(dllName);
            if (dllBytes == null) continue;
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
            Debug.Log($"LoadMetadataForAOTAssembly:{dllName} ret:{err}");
        }
    }

    /// <summary>按 DllConfig 名单加载优先热更与普通热更程序集（仅真机）</summary>
    private void LoadHotUpdateAssemblies()
    {
        EnsureDllConfig();
        foreach (string dllName in _dllConfig.priorityHotUpdate)
            LoadDll(dllName);
        foreach (string dllName in _dllConfig.hotUpdate)
            LoadDll(dllName);
    }

    private void EnsureDllConfig()
    {
        if (_dllConfig != null) return;

        _dllConfig = Addressables.LoadAssetAsync<DllConfig>(DllConfigKey).WaitForCompletion();
        if (_dllConfig == null)
            Debug.LogError($"DllConfig 加载失败（地址 {DllConfigKey}），请检查 Addressables 分组中的地址");
    }

    /// <summary>按 DLL 名加载 .bytes 资产（地址即 DLL 全名，如 HotUpdate.dll）</summary>
    private byte[] LoadDllBytes(string dllName)
    {
        TextAsset textAsset = Addressables.LoadAssetAsync<TextAsset>(dllName).WaitForCompletion();
        if (textAsset == null)
        {
            Debug.LogError($"DLL 字节加载失败（地址 {dllName}），请在 Addressables 分组里为该 .bytes 设置同名地址");
            return null;
        }

        byte[] bytes = textAsset.bytes;
        Addressables.Release(textAsset);
        return bytes;
    }

    private void LoadDll(string dllName)
    {
        if (_loadedDlls.Contains(dllName)) return;

        byte[] bytes = LoadDllBytes(dllName);
        if (bytes == null) return;

        Assembly.Load(bytes);
        _loadedDlls.Add(dllName);
        Debug.Log($"已加载热更程序集: {dllName} size:{bytes.Length}");
    }

    private IEnumerator EnterGame()
    {
        var handle = Addressables.InstantiateAsync(GameLanuchKey);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
            Debug.Log("GameLanuch 已实例化，主流程启动");
        else
            Debug.LogError($"GameLanuch 实例化失败: {handle.OperationException}");
    }
}