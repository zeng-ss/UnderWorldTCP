using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class ResMgr : UnitySingleTonMono<ResMgr>
{
    private Dictionary<string, AssetHandle> handleCache = new();
    private Dictionary<string, object> assetCache = new();
    private List<GameObject> instanceCache = new();

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
            instanceCache.Add(instance);
            onComplete?.Invoke(instance);
        });
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
        if (assetCache.ContainsKey(address)) assetCache.Remove(address);
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;
        instanceCache.Remove(instance);
        Destroy(instance);
    }

    public void ReleaseAll()
    {
        foreach (var handle in handleCache.Values) handle.Release();
        handleCache.Clear();
        foreach (var instance in instanceCache)
        {
            if (instance != null) Destroy(instance);
        }
        instanceCache.Clear();
        assetCache.Clear();
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
        if (handleCache.ContainsKey(address)) handleCache.Remove(address);
    }

    protected void OnDestroy()
    {
        ReleaseAll();
    }

    #endregion
}
