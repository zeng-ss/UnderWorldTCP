using System;
using System.Collections.Generic;
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
            if (obj == null) return; // 已销毁的对象不入池（Unity 的 == 重载能识别被 Destroy 的对象）
            obj.SetActive(false);
            _poolList.Add(obj);
            if (FatherObj != null) obj.transform.parent = FatherObj.transform;
        }

        public GameObject PopObj()
        {
            // 池里的对象可能在外部被销毁（最典型的是场景 Single 切换），
            // 这类「空壳」直接跳过丢弃，否则访问它的 transform 会抛 MissingReferenceException。
            while (_poolList.Count > 0)
            {
                int last = _poolList.Count - 1;
                var obj = _poolList[last]; // 尾取 O(1)
                _poolList.RemoveAt(last); // 尾删 O(1)
                if (obj == null) continue;

                obj.transform.parent = null;
                obj.SetActive(true);
                return obj;
            }

            return null;
        }

        public int Count
        {
            get
            {
                // 顺手清掉空壳，让统计与「池子里还有没有货」的判断都准确
                for (int i = _poolList.Count - 1; i >= 0; i--)
                {
                    if (_poolList[i] == null) _poolList.RemoveAt(i);
                }

                return _poolList.Count;
            }
        }
    }

    /// <summary>
    /// 缓存池管理器。纯 C# 类，不是 MonoBehaviour —— 由 AppContext 统一创建与持有，访问走 AppContext.Pool。
    /// 池子根节点是 DontDestroyOnLoad 的，跨场景存活（见 CreateRoot）。
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
            if (_poolDic.TryGetValue(name, out var poolData))
            {
                GameObject pooled = poolData.PopObj();
                // PopObj 可能返回 null（池内对象都已被外部销毁），这时不能把 null 交给回调，直接走新加载
                if (pooled != null)
                {
                    callback?.Invoke(pooled);
                    return;
                }
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

            if (_grandFatherObj == null) _grandFatherObj = CreateRoot();

            if (_poolDic.TryGetValue(name, out var poolData))
                poolData.PushObj(obj);
            else
                _poolDic.Add(name, new PoolData(obj, _grandFatherObj));
        }

        /// <summary>
        /// 创建池子根节点。必须是 DontDestroyOnLoad 的持久对象：
        /// 场景是 LoadSceneMode.Single 切换的，根节点若留在场景里会随旧场景被销毁，
        /// 而 PoolMgr 挂在 AppContext（常驻）上，字典不会跟着清，于是池里全是空壳。
        /// </summary>
        private static GameObject CreateRoot()
        {
            var root = new GameObject("Pool");
            GameObject.DontDestroyOnLoad(root);
            return root;
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
