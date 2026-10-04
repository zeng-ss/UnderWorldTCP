using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class ResMgr : UnitySingleTonMono<ResMgr>
{
    // 仍在加载/已加载的 AssetHandle，只有显式 Release 才会移除（用于正确释放）
    private Dictionary<string, AssetHandle> handleCache = new();
    // 已加载资源本体，命中后直接返回，避免重复走 handle
    private Dictionary<string, object> assetCache = new();
    // 精灵图缓存：列表项 UI 会反复请求同一张图
    private Dictionary<string, Sprite> spriteCache = new();

    private ResourcePackage Package => Global.Instance._YooPackage;

    #region 加载方法

    public void LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("ResMgr: 加载地址不能为空！");
            onComplete?.Invoke(null);
            return;
        }

        if (assetCache.TryGetValue(address, out object cacheObj) && cacheObj is T tObj)
        {
            onComplete?.Invoke(tObj);
            return;
        }

        if (handleCache.TryGetValue(address, out AssetHandle handle))
        {
            if (handle.IsDone)
            {
                OnLoadComplete(handle, address, onComplete);
            }
            else
            {
                handle.Completed += (h) => OnLoadComplete(h, address, onComplete);
            }
            return;
        }

        AssetHandle loadHandle = Package.LoadAssetAsync<T>(address);
        handleCache.Add(address, loadHandle);
        loadHandle.Completed += (h) => OnLoadComplete(h, address, onComplete);
    }

    public void LoadAndInstantiateAsync(string address, Transform parent = null, Action<GameObject> onComplete = null)
    {
        LoadAssetAsync<GameObject>(address, prefab =>
        {
            if (prefab == null)
            {
                onComplete?.Invoke(null);
                return;
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.name = prefab.name;
            onComplete?.Invoke(instance);
        });
    }

    /// <summary>
    /// 加载精灵图。优先走 YooAsset（可热更）；资源尚未进包时回退 Resources 兜底。
    /// 结果按地址缓存，同一张图不会重复加载。
    /// </summary>
    /// <param name="address">YooAsset 地址 / Resources 相对路径</param>
    public void LoadSpriteAsync(string address, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("ResMgr: 精灵图加载地址不能为空！");
            onComplete?.Invoke(null);
            return;
        }

        if (spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
        {
            onComplete?.Invoke(cached);
            return;
        }

        bool inPackage = Package != null && Package.IsLocationValid(address);
        if (inPackage)
        {
            LoadAssetAsync<Sprite>(address, sprite =>
            {
                if (sprite != null) spriteCache[address] = sprite;
                onComplete?.Invoke(sprite);
            });
            return;
        }

        // 还没挪进 YooAsset 收集目录的老资源，先兜底保证不炸图
        Sprite fallback = Resources.Load<Sprite>(address);
        if (fallback == null)
        {
            Debug.LogError($"ResMgr: 精灵图加载失败 {address}（YooAsset 包与 Resources 中都没有）");
        }
        else
        {
            Debug.LogWarning($"ResMgr: {address} 不在 YooAsset 包内，已回退 Resources.Load。" +
                             "把资源移到 Assets/Res 下被收集的目录并重新打包后即可热更。");
            spriteCache[address] = fallback;
        }

        onComplete?.Invoke(fallback);
    }

    public void LoadBatchAssetsAsync<T>(List<string> addressList, Action<Dictionary<string, T>> onComplete) where T : UnityEngine.Object
    {
        if (addressList == null || addressList.Count == 0)
        {
            onComplete?.Invoke(new Dictionary<string, T>());
            return;
        }

        Dictionary<string, T> resultDict = new Dictionary<string, T>();
        int loadedCount = 0;

        foreach (string address in addressList)
        {
            LoadAssetAsync<T>(address, asset =>
            {
                loadedCount++;
                if (asset != null) resultDict.Add(address, asset);
                if (loadedCount == addressList.Count) onComplete?.Invoke(resultDict);
            });
        }
    }

    #endregion

    #region 资源释放方法

    public void ReleaseAsset(string address)
    {
        if (handleCache.TryGetValue(address, out AssetHandle handle))
        {
            handle.Release();
            handleCache.Remove(address);
        }

        assetCache.Remove(address);
        spriteCache.Remove(address);
    }

    /// <summary>
    /// 销毁由 LoadAndInstantiateAsync 生成的实例。
    /// 实例不再被 ResMgr 持有，频繁创建销毁的对象请改用 PoolMgr。
    /// </summary>
    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;
        Destroy(instance);
    }

    public void ReleaseAll()
    {
        foreach (var handle in handleCache.Values) handle.Release();
        handleCache.Clear();
        assetCache.Clear();
        spriteCache.Clear();
    }

    #endregion

    #region 私有辅助方法

    private void OnLoadComplete<T>(AssetHandle handle, string address, Action<T> onComplete) where T : UnityEngine.Object
    {
        if (handle.Status == EOperationStatus.Succeeded)
        {
            T result = handle.AssetObject as T;
            if (result != null)
            {
                assetCache[address] = result;
                onComplete?.Invoke(result);
            }
            else
            {
                Debug.LogError($"ResMgr: 加载转换失败 {address}");
                onComplete?.Invoke(null);
            }
        }
        else
        {
            Debug.LogError($"ResMgr: 加载失败 {address} - {handle.Error}");
            onComplete?.Invoke(null);
        }
        // 注意：这里不能把 handle 从 handleCache 移除，否则 ReleaseAsset 找不到句柄，资源永远释放不掉
    }

    protected void OnDestroy()
    {
        ReleaseAll();
    }

    #endregion
}
