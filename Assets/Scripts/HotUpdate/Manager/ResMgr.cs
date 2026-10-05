using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class ResMgr
{
    // 仍在加载/已加载的 AssetHandle，只有显式 Release 才会移除（用于正确释放）
    private readonly Dictionary<string, AssetHandle> _handleCache = new();

    // 已加载资源本体，命中后直接返回，避免重复走 handle
    private readonly Dictionary<string, object> _assetCache = new();

    // 精灵图缓存：列表项 UI 会反复请求同一张图
    private readonly Dictionary<string, Sprite> _spriteCache = new();

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

        if (_assetCache.TryGetValue(address, out object cacheObj) && cacheObj is T tObj)
        {
            onComplete?.Invoke(tObj);
            return;
        }

        if (_handleCache.TryGetValue(address, out AssetHandle handle))
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
        _handleCache.Add(address, loadHandle);
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

            GameObject instance = GameObject.Instantiate(prefab, parent);
            instance.name = prefab.name;
            onComplete?.Invoke(instance);
        });
    }

    // 加载精灵图
    public void LoadSpriteAsync(string address, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("ResMgr: 精灵图加载地址不能为空！");
            onComplete?.Invoke(null);
            return;
        }

        if (_spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
        {
            onComplete?.Invoke(cached);
            return;
        }

        bool inPackage = Package != null && Package.IsLocationValid(address);
        if (inPackage)
        {
            LoadAssetAsync<Sprite>(address, sprite =>
            {
                if (sprite != null) _spriteCache[address] = sprite;
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
            _spriteCache[address] = fallback;
        }

        onComplete?.Invoke(fallback);
    }

    public void LoadBatchAssetsAsync<T>(List<string> addressList, Action<Dictionary<string, T>> onComplete)
        where T : UnityEngine.Object
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
        if (_handleCache.TryGetValue(address, out AssetHandle handle))
        {
            handle.Release();
            _handleCache.Remove(address);
        }

        _assetCache.Remove(address);
        _spriteCache.Remove(address);
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handleCache.Values) handle.Release();
        _handleCache.Clear();
        _assetCache.Clear();
        _spriteCache.Clear();
    }

    #endregion

    #region 私有辅助方法

    private void OnLoadComplete<T>(AssetHandle handle, string address, Action<T> onComplete)
        where T : UnityEngine.Object
    {
        if (handle.Status == EOperationStatus.Succeeded)
        {
            T result = handle.AssetObject as T;
            if (result != null)
            {
                _assetCache[address] = result;
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

    #endregion
}