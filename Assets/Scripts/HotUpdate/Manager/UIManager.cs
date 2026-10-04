using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

public class UIManager : UnitySingleTonMono<UIManager>
{
    // 面板管理字典（保留实例）
    private Dictionary<string, BasePanel> UIPanelDict = new();

    private Dictionary<string, AssetHandle> panelAssetHandles = new();

    // 面板原始Transform配置（保存预制体的宽高/锚点）
    private Dictionary<string, RectTransformData> panelOriginalTransformData = new();

    // 加载中面板追踪
    private Dictionary<string, AssetHandle> panelLoadHandles = new();

    // Canvas相关
    [HideInInspector] public RectTransform canvas;
    private Canvas canvasComponent;
    private CanvasScaler canvasScaler;

    // 面板容器
    private GameObject currentPanel;

    private bool isCanvasInitialized;

    private struct RectTransformData
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector3 anchoredPosition3D;
        public Vector3 localScale;
    }

    public void OpenPanel<T>(Action<T> onLoadComplete = null) where T : BasePanel
    {
        if (!isCanvasInitialized)
        {
            Debug.LogError("Canvas未初始化完成，无法打开面板！");
            onLoadComplete?.Invoke(null);
            return;
        }

        string panelName = GetPanelKey<T>();

        // 面板已存在：恢复原始Transform
        if (UIPanelDict.ContainsKey(panelName))
        {
            T panel = UIPanelDict[panelName] as T;
            if (panelOriginalTransformData.ContainsKey(panelName))
            {
                RestorePanelTransform(panel.transform, panelName);
            }

            panel.Show();
            onLoadComplete?.Invoke(panel);
            return;
        }

        // 面板未加载：异步加载
        if (panelLoadHandles.ContainsKey(panelName))
        {
            Debug.LogWarning($"面板 {panelName} 正在加载中，请勿重复调用");
            onLoadComplete?.Invoke(null);
            return;
        }

        AssetHandle loadHandle = Global.Instance._YooPackage.LoadAssetAsync<GameObject>(panelName);
        panelLoadHandles.Add(panelName, loadHandle);
        StartCoroutine(FinishLoadPanelCoroutine(loadHandle, panelName, onLoadComplete));
    }

    private IEnumerator FinishLoadPanelCoroutine<T>(AssetHandle loadHandle, string panelName,
        Action<T> onLoadComplete = null) where T : BasePanel
    {
        yield return loadHandle;
        panelLoadHandles.Remove(panelName);

        if (loadHandle.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"加载面板失败: {panelName}，错误：{loadHandle.Error}");
            onLoadComplete?.Invoke(null);
            yield break;
        }

        panelAssetHandles[panelName] = loadHandle;

        GameObject prefabObj = loadHandle.AssetObject as GameObject;
        RectTransform prefabRect = prefabObj.GetComponent<RectTransform>();
        if (prefabRect != null)
        {
            panelOriginalTransformData[panelName] = new RectTransformData
            {
                anchorMin = prefabRect.anchorMin,
                anchorMax = prefabRect.anchorMax,
                sizeDelta = prefabRect.sizeDelta,
                pivot = prefabRect.pivot,
                anchoredPosition3D = prefabRect.anchoredPosition3D,
                localScale = prefabRect.localScale
            };
        }

        GameObject panelObj = Instantiate(prefabObj, currentPanel.transform, false);
        panelObj.name = panelName;

        BasePanel panel = panelObj.GetComponent<T>();
        if (panel == null)
        {
            Debug.LogError($"面板 {panelName} 缺少BasePanel组件");
            Destroy(panelObj);
            onLoadComplete?.Invoke(null);
            yield break;
        }

        UIPanelDict.Add(panelName, panel);
        panel.Show();
        onLoadComplete?.Invoke(panel as T);
    }

    private void RestorePanelTransform(Transform panelTransform, string panelName)
    {
        if (!panelOriginalTransformData.ContainsKey(panelName)) return;

        RectTransform panelRect = panelTransform.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            RectTransformData originalData = panelOriginalTransformData[panelName];
            panelRect.SetParent(currentPanel.transform, false);
            panelRect.anchorMin = originalData.anchorMin;
            panelRect.anchorMax = originalData.anchorMax;
            panelRect.sizeDelta = originalData.sizeDelta;
            panelRect.pivot = originalData.pivot;
            panelRect.anchoredPosition3D = originalData.anchoredPosition3D;
            panelRect.localScale = originalData.localScale;
            panelRect.localRotation = Quaternion.identity;
        }
        else
        {
            panelTransform.SetParent(currentPanel.transform);
            panelTransform.localPosition = Vector3.zero;
            panelTransform.localRotation = Quaternion.identity;
            panelTransform.localScale = Vector3.one;
        }
    }

    public void ClosePanel<T>() where T : BasePanel
    {
        string panelName = GetPanelKey<T>();
        if (UIPanelDict.TryGetValue(panelName, out var panel))
        {
            panel.Hide();
            panel.transform.SetParent(canvas.transform, false);
        }
    }

    public void DestroyPanel<T>() where T : BasePanel
    {
        string panelName = GetPanelKey<T>();
        if (UIPanelDict.TryGetValue(panelName, out var panel))
        {
            Destroy(panel.gameObject);
            UIPanelDict.Remove(panelName);

            if (panelAssetHandles.ContainsKey(panelName))
            {
                panelAssetHandles[panelName].Release();
                panelAssetHandles.Remove(panelName);
            }

            if (panelOriginalTransformData.ContainsKey(panelName))
            {
                panelOriginalTransformData.Remove(panelName);
            }
        }
    }

    public T GetPanel<T>() where T : BasePanel
    {
        string panelName = GetPanelKey<T>();
        return UIPanelDict.TryGetValue(panelName, out var panel) ? panel as T : null;
    }

    private string GetPanelKey<T>() where T : BasePanel => "Assets/Res/UI/UIPanel/" + typeof(T).Name;

    public void ClearAllPanel()
    {
        foreach (var kv in UIPanelDict) kv.Value.Hide();
    }

    public void TogglePanel<T>(KeyCode keyCode) where T : BasePanel
    {
        if (Input.GetKeyDown(keyCode))
        {
            T panel = GetPanel<T>();
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                OpenPanel<T>();
                EventMgr.Instance.EventTrigger(GameEvent.CursorShow);
            }
            else
            {
                ClosePanel<T>();
                EventMgr.Instance.EventTrigger(GameEvent.CursorHide);
            }
        }
    }

    public override void Awake()
    {
        base.Awake();
        GameObject canvasObj = Resources.Load<GameObject>("Canvas") ?? new GameObject("Canvas");
        if (canvasObj.name != "Canvas")
        {
            canvasObj.name = "Canvas";
            canvasComponent = canvasObj.AddComponent<Canvas>();
            canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            canvasObj = Instantiate(canvasObj);
            canvasObj.name = "Canvas";
            canvasComponent = canvasObj.GetComponent<Canvas>();
            canvasScaler = canvasObj.GetComponent<CanvasScaler>();
        }

        canvas = canvasObj.transform as RectTransform;
        DontDestroyOnLoad(canvasObj);

        currentPanel = new GameObject("currentShowPanel");
        RectTransform rect = currentPanel.AddComponent<RectTransform>();
        rect.SetParent(canvas, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = Resources.Load<GameObject>("EventSystem") ?? new GameObject("EventSystem");
            if (eventSystemObj.name != "EventSystem")
            {
                eventSystemObj.name = "EventSystem";
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else
            {
                eventSystemObj = Instantiate(eventSystemObj);
            }

            DontDestroyOnLoad(eventSystemObj);
        }

        isCanvasInitialized = true;
    }

    private void OnDestroy()
    {
        foreach (var handle in panelAssetHandles.Values) handle.Release();
        panelAssetHandles.Clear();
        panelLoadHandles.Clear();
        panelOriginalTransformData.Clear();
    }
}