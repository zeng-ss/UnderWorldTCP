using System;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>
/// UI 资源加载器。
///
/// 从 UIManager 里拆出来，只负责：面板预制体的异步加载、AssetHandle 缓存与释放、
/// 以及预制体原始 RectTransform 的快照 / 还原。
/// 面板的层级、显隐、生命周期仍归 UIManager 管。
/// </summary>
public class UIAssetLoader
{
    private struct RectTransformData
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector3 anchoredPosition3D;
        public Vector3 localScale;
    }

    private readonly Dictionary<string, AssetHandle> _handles = new();
    private readonly Dictionary<string, AssetHandle> _loadingHandles = new();
    private readonly Dictionary<string, RectTransformData> _originalTransforms = new();

    /// <summary>资源包未就绪时（热更 DLL 重载早期）返回 false</summary>
    public bool IsReady => Global.Instance != null && Global.Instance._YooPackage != null;

    #region 加载

    /// <summary>
    /// 异步加载面板预制体。同一路径并发请求会复用同一个 handle，
    /// 不会像旧实现那样直接告警并返回 null。
    /// </summary>
    public void LoadAsync(string path, Action<GameObject> onComplete)
    {
        if (string.IsNullOrEmpty(path) || !IsReady)
        {
            Debug.LogError($"UIAssetLoader: 无法加载面板 {path}（路径为空或资源包未就绪）");
            onComplete?.Invoke(null);
            return;
        }

        if (_handles.TryGetValue(path, out var loaded))
        {
            onComplete?.Invoke(loaded.AssetObject as GameObject);
            return;
        }

        if (_loadingHandles.TryGetValue(path, out var pending))
        {
            pending.Completed += handle => OnLoadFinished(path, handle, onComplete);
            return;
        }

        AssetHandle handle = Global.Instance._YooPackage.LoadAssetAsync<GameObject>(path);
        _loadingHandles[path] = handle;
        handle.Completed += h => OnLoadFinished(path, h, onComplete);
    }

    private void OnLoadFinished(string path, AssetHandle handle, Action<GameObject> onComplete)
    {
        _loadingHandles.Remove(path);
        if (handle.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"UIAssetLoader: 加载面板失败 {path} - {handle.Error}");
            onComplete?.Invoke(null);
            return;
        }

        _handles[path] = handle;
        GameObject prefab = handle.AssetObject as GameObject;
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
            anchorMin = prefabRect.anchorMin,
            anchorMax = prefabRect.anchorMax,
            sizeDelta = prefabRect.sizeDelta,
            pivot = prefabRect.pivot,
            anchoredPosition3D = prefabRect.anchoredPosition3D,
            localScale = prefabRect.localScale
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
            panelRect.anchorMin = original.anchorMin;
            panelRect.anchorMax = original.anchorMax;
            panelRect.sizeDelta = original.sizeDelta;
            panelRect.pivot = original.pivot;
            panelRect.anchoredPosition3D = original.anchoredPosition3D;
            panelRect.localScale = original.localScale;
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
            handle.Release();
            _handles.Remove(path);
        }

        _originalTransforms.Remove(path);
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values) handle.Release();
        _handles.Clear();

        foreach (var handle in _loadingHandles.Values) handle.Release();
        _loadingHandles.Clear();

        _originalTransforms.Clear();
    }

    #endregion
}