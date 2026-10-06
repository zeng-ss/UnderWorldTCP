using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// UI 资源加载器。
///
/// 从 UIManager 里拆出来，只负责：面板预制体的异步加载、handle 缓存与释放、
/// 以及预制体原始 RectTransform 的快照 / 还原。
/// 面板的层级、显隐、生命周期仍归 UIManager 管。
/// </summary>
public class UIAssetLoader
{
    private struct RectTransformData
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 SizeDelta;
        public Vector2 Pivot;
        public Vector3 AnchoredPosition3D;
        public Vector3 LocalScale;
    }

    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadingHandles = new();
    private readonly Dictionary<string, RectTransformData> _originalTransforms = new();

    /// <summary>资源系统是否就绪。Addressables 由 AOT 启动流程（Load.cs）保证先初始化，
    /// 能跑到这里的代码（热更场景里的 UI）时资源系统必然已就绪</summary>
    public bool IsReady => true;

    #region 加载

    /// <summary>
    /// 异步加载面板预制体。同一路径并发请求会复用同一个 handle，
    /// 不会像旧实现那样直接告警并返回 null。
    /// </summary>
    public void LoadAsync(string path, Action<GameObject> onComplete)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"UIAssetLoader: 无法加载面板 {path}（路径为空）");
            onComplete?.Invoke(null);
            return;
        }

        if (_handles.TryGetValue(path, out var loaded))
        {
            onComplete?.Invoke(loaded.Result);
            return;
        }

        if (_loadingHandles.TryGetValue(path, out var pending))
        {
            pending.Completed += handle => OnLoadFinished(path, handle, onComplete);
            return;
        }

        var handle = Addressables.LoadAssetAsync<GameObject>(path);
        _loadingHandles[path] = handle;
        handle.Completed += h => OnLoadFinished(path, h, onComplete);
    }

    private void OnLoadFinished(string path, AsyncOperationHandle<GameObject> handle, Action<GameObject> onComplete)
    {
        _loadingHandles.Remove(path);
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"UIAssetLoader: 加载面板失败 {path} - {handle.OperationException}");
            onComplete?.Invoke(null);
            return;
        }

        _handles[path] = handle;
        GameObject prefab = handle.Result;
        SnapshotTransform(path, prefab);
        onComplete?.Invoke(prefab);
    }

    #endregion

    #region Transform 快照 / 还原

    private void SnapshotTransform(string path, GameObject prefab)
    {
        if (prefab == null) return;
        var prefabRect = prefab.GetComponent<RectTransform>();
        if (prefabRect == null) return;

        _originalTransforms[path] = new RectTransformData
        {
            AnchorMin = prefabRect.anchorMin,
            AnchorMax = prefabRect.anchorMax,
            SizeDelta = prefabRect.sizeDelta,
            Pivot = prefabRect.pivot,
            AnchoredPosition3D = prefabRect.anchoredPosition3D,
            LocalScale = prefabRect.localScale
        };
    }

    /// <summary>把面板恢复到预制体原始的宽高 / 锚点 / 缩放</summary>
    public void RestoreTransform(string path, Transform panelTransform, Transform parent)
    {
        if (panelTransform == null || parent == null) return;

        var panelRect = panelTransform.GetComponent<RectTransform>();
        if (panelRect != null && _originalTransforms.TryGetValue(path, out var original))
        {
            panelRect.SetParent(parent, false);
            panelRect.anchorMin = original.AnchorMin;
            panelRect.anchorMax = original.AnchorMax;
            panelRect.sizeDelta = original.SizeDelta;
            panelRect.pivot = original.Pivot;
            panelRect.anchoredPosition3D = original.AnchoredPosition3D;
            panelRect.localScale = original.LocalScale;
            panelRect.localRotation = Quaternion.identity;
            return;
        }

        panelTransform.SetParent(parent);
        panelTransform.localPosition = Vector3.zero;
        panelTransform.localRotation = Quaternion.identity;
        panelTransform.localScale = Vector3.one;
    }

    #endregion

    #region 释放

    public void Release(string path)
    {
        if (_handles.TryGetValue(path, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(path);
        }

        _originalTransforms.Remove(path);
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values) Addressables.Release(handle);
        _handles.Clear();

        foreach (var handle in _loadingHandles.Values) Addressables.Release(handle);
        _loadingHandles.Clear();

        _originalTransforms.Clear();
    }

    #endregion
}
