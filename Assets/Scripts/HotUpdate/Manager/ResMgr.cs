using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HotUpdate.Manager
{
    public class ResMgr
    {
        // 仍在加载/已加载的 handle，只有显式 Release 才会移除（用于正确释放）
        private readonly Dictionary<string, AsyncOperationHandle> _handleCache = new();

        // 已加载资源本体，命中后直接返回，避免重复走 handle
        private readonly Dictionary<string, object> _assetCache = new();

        // 精灵图缓存：列表项 UI 会反复请求同一张图
        private readonly Dictionary<string, Sprite> _spriteCache = new();

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

            if (_handleCache.TryGetValue(address, out AsyncOperationHandle existing))
            {
                var typed = existing.Convert<T>();
                if (typed.IsDone)
                    OnLoadComplete(typed, address, onComplete);
                else
                    typed.Completed += h => OnLoadComplete(h, address, onComplete);
                return;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            _handleCache.Add(address, handle);
            handle.Completed += h => OnLoadComplete(h, address, onComplete);
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

            // 先探测地址是否已被 Addressables 收集（未标记的老资源回退 Resources.Load）
            var locHandle = Addressables.LoadResourceLocationsAsync(address, typeof(Sprite));
            locHandle.Completed += h =>
            {
                Addressables.Release(locHandle);

                if (h.Result != null && h.Result.Count > 0)
                {
                    LoadAssetAsync<Sprite>(address, sprite =>
                    {
                        if (sprite != null) _spriteCache[address] = sprite;
                        onComplete?.Invoke(sprite);
                    });
                    return;
                }

                // 还没标记成 Addressable 的老资源，先兜底保证不炸图
                Sprite fallback = Resources.Load<Sprite>(address);
                if (fallback == null)
                {
                    Debug.LogError($"ResMgr: 精灵图加载失败 {address}（Addressables 与 Resources 中都没有）");
                }
                else
                {
                    Debug.LogWarning($"ResMgr: {address} 不在 Addressables 里，已回退 Resources.Load。" +
                                     "把资源标记为 Addressable（Tools/Addressables/一键标记资源）后即可热更。");
                    _spriteCache[address] = fallback;
                }

                onComplete?.Invoke(fallback);
            };
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
            if (_handleCache.TryGetValue(address, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _handleCache.Remove(address);
            }

            _assetCache.Remove(address);
            _spriteCache.Remove(address);
        }

        public void ReleaseAll()
        {
            foreach (var handle in _handleCache.Values) Addressables.Release(handle);
            _handleCache.Clear();
            _assetCache.Clear();
            _spriteCache.Clear();
        }

        #endregion

        #region 私有辅助方法

        private void OnLoadComplete<T>(AsyncOperationHandle<T> handle, string address, Action<T> onComplete)
            where T : UnityEngine.Object
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                T result = handle.Result;
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
                Debug.LogError($"ResMgr: 加载失败 {address} - {handle.OperationException}");
                onComplete?.Invoke(null);
            }
            // 注意：这里不能把 handle 从 handleCache 移除，否则 ReleaseAsset 找不到句柄，资源永远释放不掉
        }

        #endregion
    }
}
