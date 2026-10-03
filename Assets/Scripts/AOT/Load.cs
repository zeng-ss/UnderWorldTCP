using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using YooAsset;

public class Load : MonoBehaviour
{
    [SerializeField, Header("运行模式")] private EPlayMode _playMode = EPlayMode.EditorSimulateMode;
    [SerializeField, Header("资源系统地址")] private string defaultHostServer;
    [SerializeField, Header("备用地址")] private string fallbackHostServer;
    [Header("热更新窗口")] private IHotUpdateWindow hotUpdateWindow;
    private ResourcePackage package;

    private static Dictionary<string, byte[]> s_assetDatas = new();

    public static List<string> AOTMetaAssemblyNames { get; } = new()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
    };

    private void Awake()
    {
        InitYooAsset();
    }

    private void InitYooAsset()
    {
        YooAssets.Initialize();
        package = YooAssets.CreatePackage("DefaultPackage");
        StartCoroutine(InitPackage());
    }

    private IEnumerator InitPackage()
    {
        Debug.Log("初始化....");
        InitializePackageOperation operation;
        switch (_playMode)
        {
            case EPlayMode.EditorSimulateMode:
            {
                var buildResult =
                    EditorSimulateBuildInvoker.Build("DefaultPackage", (int)EBundleType.VirtualAssetBundle);
                var editorParams =
                    FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
                var options = new EditorSimulateModeOptions { EditorFileSystemParameters = editorParams };
                operation = package.InitializePackageAsync(options);
                break;
            }

            case EPlayMode.HostPlayMode:
            {
                // 指定到包含 yoo/DefaultPackage/ 的上级目录
                string buildinRoot = Path.Combine(Application.streamingAssetsPath, "yoo", "DefaultPackage");
    
                var builtinParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(buildinRoot);
                builtinParams.AddParameter(EFileSystemParameter.BuiltinFileAccessor, new GameBuiltinFileAccessor());

                var remoteService = new RemoteServiceAdapter(defaultHostServer, fallbackHostServer);
                var sandboxParams = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService, null);
                var decryptor = new GameBundleDecryption();
                sandboxParams.AddParameter(EFileSystemParameter.AssetBundleDecryptor, decryptor);

                var options = new HostPlayModeOptions
                {
                    BuiltinFileSystemParameters = builtinParams,
                    CacheFileSystemParameters = sandboxParams
                };
                operation = package.InitializePackageAsync(options);
                break;
            }

            default:
                yield break;
        }

        yield return operation;

        if (operation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"{operation.Error}");
            yield break;
        }

        // 请求资源版本
        var versionOperation = package.RequestPackageVersionAsync();
        yield return versionOperation;
        if (versionOperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError("RequestVersion Error::" + versionOperation.Error);
            yield break;
        }

        // 加载资源清单
        var manifestOptions = new LoadPackageManifestOptions(versionOperation.PackageVersion, 60);
        var manifestOperation = package.LoadPackageManifestAsync(manifestOptions);
        yield return manifestOperation;

        if (manifestOperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError("UpdateManifest Error::" + manifestOperation.Error);
            yield break;
        }

        AssetHandle handle = package.LoadAssetAsync<GameObject>("Assets/Res/Prefab/HotUpdateWindow");
        yield return handle;

        if (handle.Status == EOperationStatus.Succeeded)
        {
            GameObject prefab = handle.AssetObject as GameObject;
            yield return hotUpdateWindow = Instantiate(prefab).GetComponent<IHotUpdateWindow>();
            // 或者如果你有 UI 组件，可以用 GetComponent
        }
        else
        {
            Debug.LogError($"加载失败: {handle.Error}");
        }
        Debug.Log("开始下载...");

        yield return Download();
    }

    private IEnumerator Download()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var downloadOptions = new ResourceDownloaderOptions(downloadingMaxNum, failedTryAgain);
        var downloader = package.CreateResourceDownloader(downloadOptions);

        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("没有资源更新，直接进入游戏..");
            hotUpdateWindow.RefreshUI(1, "没有资源更新");
            yield return InitCode();
            yield break;
        }

        downloader.DownloadCompleted += OnDownloadCompleted;
        downloader.DownloadError += OnDownloadError;
        downloader.DownloadProgressChanged += OnDownloadProgress;
        downloader.DownloadFileStarted += OnStartDownloadFile;

        downloader.StartDownload();
        yield return downloader;

        downloader.DownloadCompleted -= OnDownloadCompleted;
        downloader.DownloadError -= OnDownloadError;
        downloader.DownloadProgressChanged -= OnDownloadProgress;
        downloader.DownloadFileStarted -= OnStartDownloadFile;

        if (downloader.Status == EOperationStatus.Succeeded)
        {
            yield return InitCode();
        }
        else
        {
            Debug.Log("下载失败...");
        }
    }

    private IEnumerator InitCode()
    {
        var assets = new List<string>
        {
            "HotUpdate.dll",
            "PriorityHotUpdate.dll"
        }.Concat(AOTMetaAssemblyNames);

        foreach (var asset in assets)
        {
            AssetHandle dllHandle;
            dllHandle = package.LoadAssetAsync<TextAsset>("Assets/DllBytes/" + asset);
            /*if (asset.StartsWith("HotUpdate") || asset.StartsWith("PriorityHotUpdate"))
            {
                dllHandle = package.LoadAssetAsync<TextAsset>("Assets/DllBytes/" + asset);
            }
            else dllHandle = package.LoadAssetAsync<TextAsset>("Assets/DllBytes/AOT/" + asset);*/
            yield return dllHandle;
            TextAsset textAsset = dllHandle.AssetObject as TextAsset;
            s_assetDatas[asset] = textAsset?.bytes;
            Debug.Log($"dll:{asset} size:{textAsset?.bytes.Length}");
        }

        LoadMetadataForAOTAssemblies();
#if !UNITY_EDITOR
        Assembly.Load(s_assetDatas["PriorityHotUpdate.dll"]);
        Assembly.Load(s_assetDatas["HotUpdate.dll"]);
#endif

        yield return EnterGame();
    }

    IEnumerator EnterGame()
    {
        SceneHandle handle = package.LoadSceneAsync("Assets/Scenes/StartScene");
        yield return handle;
        Debug.Log($"Scene name is {handle.SceneName}");
    }

    private void OnDownloadCompleted(DownloadCompletedEventArgs args)
    {
        Debug.Log("下载" + (args.Succeeded ? " 成功 " : "失败") + " ....");
    }

    private void OnDownloadError(DownloadErrorEventArgs args)
    {
        Debug.Log($"下载失败::{args.FileName}  Error::{args.ErrorInfo}");
    }

    private void OnDownloadProgress(DownloadProgressChangedEventArgs args)
    {
        float prgs = args.CurrentDownloadBytes * 1.0f / args.TotalDownloadBytes;
        hotUpdateWindow.RefreshUI(prgs,
            $"下载进度:{args.CurrentDownloadBytes / (1024 * 1024)}M/{args.TotalDownloadBytes / (1024 * 1024)}" +
            $"M【{prgs * 100}%】");
    }

    private void OnStartDownloadFile(DownloadFileStartedEventArgs args)
    {
        Debug.Log($"开始下载：{args.FileName}  大小：{args.FileSize / 1024f}KB");
    }

    private static void LoadMetadataForAOTAssemblies()
    {
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyNames)
        {
            byte[] dllBytes = s_assetDatas[aotDllName];
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }
}

internal class GameBundleDecryption : IBundleOffsetDecryptor, IBundleStreamDecryptor
{
    public long GetFileOffset(BundleDecryptArgs args)
    {
        return 32;
    }

    public int GetBufferSize(BundleDecryptArgs args)
    {
        return 1024;
    }

    public Stream CreateDecryptionStream(BundleDecryptArgs args)
    {
        return new FileStream(args.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}

internal class RemoteServiceAdapter : IRemoteService
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServiceAdapter(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = defaultHostServer;
        _fallbackHostServer = fallbackHostServer;
    }

    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        return new List<string>
        {
            $"{_defaultHostServer}/{fileName}",
            $"{_fallbackHostServer}/{fileName}"
        };
    }
}

internal class GameBuiltinFileAccessor : IBuiltinFileAccessor
{
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public byte[] ReadAllBytes(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }
}