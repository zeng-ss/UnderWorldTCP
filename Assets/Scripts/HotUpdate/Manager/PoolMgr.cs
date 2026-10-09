using System;
using System.Collections.Generic;
using HotUpdate.Core;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Manager
{
    /// <summary>
    /// 池子数据
    /// </summary>
    public class PoolData
    {
        public GameObject FatherObj { get; }
        private readonly List<GameObject> _poolList = new();

        public PoolData(GameObject obj, GameObject grandFatherObj)
        {
            FatherObj = new GameObject(obj.name);
            FatherObj.transform.parent = grandFatherObj.transform;
            PushObj(obj);
        }

        public void PushObj(GameObject obj)
        {
            obj.SetActive(false);
            _poolList.Add(obj);
            obj.transform.parent = FatherObj.transform;
        }

        public GameObject PopObj()
        {
            if (_poolList.Count == 0) return null;

            var obj = _poolList[^1]; // 尾取 O(1)
            _poolList.RemoveAt(_poolList.Count - 1); // 尾删 O(1)
            obj.transform.parent = null;
            obj.SetActive(true);
            return obj;
        }

        public int Count => _poolList.Count;
    }

    /// <summary>
    /// 缓存池管理器。普通 MonoBehaviour，不再自己当单例 —— 由 AppContext 统一创建与持有，访问走 AppContext.Pool。
    /// </summary>
    public class PoolMgr
    {
        private readonly Dictionary<string, PoolData> _poolDic = new();
        private GameObject _grandFatherObj;

        /// <summary>
        /// 从池子获取对象，池中没有则异步加载
        /// </summary>
        public void GetObj(string name, Action<GameObject> callback = null, string address = null)
        {
            if (_poolDic.TryGetValue(name, out var poolData) && poolData.Count > 0)
            {
                callback?.Invoke(poolData.PopObj());
                return;
            }

            AppContext.Res.LoadAndInstantiateAsync(address, null, obj =>
            {
                if (obj == null) obj = new GameObject();
                obj.name = name;
                callback?.Invoke(obj);
            });
        }

        /// <summary>
        /// 将不用的对象归还池子
        /// </summary>
        public void PushObj(string name, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"PushObj: 试图归还空对象到池子 {name}");
                return;
            }

            if (_grandFatherObj == null)
                _grandFatherObj = new GameObject("Pool");

            if (_poolDic.TryGetValue(name, out var poolData))
                poolData.PushObj(obj);
            else
                _poolDic.Add(name, new PoolData(obj, _grandFatherObj));
        }

        /// <summary>
        /// 清空所有池子
        /// </summary>
        public void Clear()
        {
            foreach (var pool in _poolDic.Values)
            {
                if (pool.FatherObj != null) GameObject.Destroy(pool.FatherObj);
            }

            _poolDic.Clear();

            if (_grandFatherObj != null) GameObject.Destroy(_grandFatherObj);
            _grandFatherObj = null;
        }

        /// <summary>
        /// 预加载指定数量的对象到池子
        /// </summary>
        public void Preload(string address, string poolName, int count)
        {
            for (var i = 0; i < count; i++)
            {
                AppContext.Res.LoadAndInstantiateAsync(address, null, obj =>
                {
                    if (obj != null)
                    {
                        obj.name = poolName;
                        PushObj(poolName, obj);
                    }
                    else
                    {
                        Debug.LogWarning($"预加载失败：{address} 不存在！");
                    }
                });
            }
        }

        /// <summary>
        /// 获取池子中当前缓存的对象数量
        /// </summary>
        public int GetPoolCount(string poolName)
        {
            return _poolDic.TryGetValue(poolName, out var poolData) ? poolData.Count : 0;
        }

        /// <summary>
        /// 检查池子是否存在
        /// </summary>
        public bool HasPool(string poolName)
        {
            return _poolDic.ContainsKey(poolName);
        }

        /// <summary>
        /// 延迟指定时间后将对象归还池子（需要 DOTween）
        /// </summary>
        public void ReturnAfter(GameObject obj, float delay)
        {
            if (obj == null) return;
            var name = obj.name;
            DG.Tweening.DOVirtual.DelayedCall(delay, () =>
            {
                if (obj != null) PushObj(name, obj);
            });
        }
    }
}
