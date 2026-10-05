using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 面板管理器：只负责 Canvas 搭建、面板层级 / 显隐 / 生命周期，以及打开面板时的输入锁。
///
/// 预制体的加载与释放已拆到 <see cref="UIAssetLoader"/>，
/// 面板资源路径由 <see cref="PanelPathAttribute"/> 声明（见 PanelPathResolver）。
/// </summary>
public class UIManager : UnitySingleTonMono<UIManager>
{
    // 已实例化的面板（按资源路径索引，保留实例以便复用）
    private readonly Dictionary<string, BasePanel> UIPanelDict = new Dictionary<string, BasePanel>();

    // Canvas相关
    [HideInInspector] public RectTransform canvas;
    private Canvas canvasComponent;
    private CanvasScaler canvasScaler;

    // 面板容器
    private GameObject currentPanel;

    private bool isCanvasInitialized;

    #region 打开面板

    public void OpenPanel<T>(Action<T> onLoadComplete = null) where T : BasePanel
    {
        if (!isCanvasInitialized || currentPanel == null)
        {
            Debug.LogError("Canvas未初始化完成，无法打开面板！");
            onLoadComplete?.Invoke(null);
            return;
        }

        string path = PanelPathResolver.Resolve<T>();

        // 面板已存在：恢复原始 Transform 后直接显示
        if (UIPanelDict.TryGetValue(path, out var existPanel))
        {
            T cached = existPanel as T;
            AppContext.UILoader.RestoreTransform(path, cached.transform, currentPanel.transform);
            cached.Show();
            PushInputLock(path, cached);
            onLoadComplete?.Invoke(cached);
            return;
        }

        AppContext.UILoader.LoadAsync(path, prefab =>
        {
            if (prefab == null)
            {
                onLoadComplete?.Invoke(null);
                return;
            }

            GameObject panelObj = Instantiate(prefab, currentPanel.transform, false);
            panelObj.name = path;

            BasePanel panel = panelObj.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogError($"面板 {path} 上找不到 {typeof(T).Name} 组件");
                Destroy(panelObj);
                onLoadComplete?.Invoke(null);
                return;
            }

            UIPanelDict.Add(path, panel);
            panel.Show();
            PushInputLock(path, panel);
            onLoadComplete?.Invoke(panel as T);
        });
    }

    /// <summary>
    /// 开 / 关一个面板。由 InputManager 触发，这里不再自己读 Input.GetKeyDown。
    /// </summary>
    public void TogglePanel<T>() where T : BasePanel
    {
        T panel = GetPanel<T>();
        if (panel == null || !panel.gameObject.activeInHierarchy)
        {
            OpenPanel<T>();
            AppContext.Events.EventTrigger(GameEvent.CursorShow);
        }
        else
        {
            ClosePanel<T>();
            AppContext.Events.EventTrigger(GameEvent.CursorHide);
        }
    }

    #endregion

    #region 关闭 / 销毁

    public void ClosePanel<T>() where T : BasePanel
    {
        string path = PanelPathResolver.Resolve<T>();
        if (UIPanelDict.TryGetValue(path, out var panel)) DoClosePanel(path, panel);
    }

    /// <summary>关闭指定面板实例，供 BasePanel 的关闭按钮回调使用</summary>
    public void ClosePanel(BasePanel panel)
    {
        if (panel == null) return;
        foreach (var kv in UIPanelDict)
        {
            if (kv.Value != panel) continue;
            DoClosePanel(kv.Key, panel);
            return;
        }

        panel.Hide();
    }

    private void DoClosePanel(string path, BasePanel panel)
    {
        panel.Hide();
        panel.transform.SetParent(canvas.transform, false);
        PopInputLock(path);
    }

    public void DestroyPanel<T>() where T : BasePanel
    {
        string path = PanelPathResolver.Resolve<T>();
        if (!UIPanelDict.TryGetValue(path, out var panel)) return;

        Destroy(panel.gameObject);
        UIPanelDict.Remove(path);
        PopInputLock(path);
        AppContext.UILoader.Release(path);
    }

    public void ClearAllPanel()
    {
        foreach (var kv in UIPanelDict) kv.Value.Hide();
    }

    /// <summary>
    /// 销毁所有已加载面板并释放其资源句柄。
    /// 用于彻底清理（卸载资源包 / 退出游戏）；日常开关面板请用 ClosePanel，它会缓存实例。
    /// </summary>
    public void DestroyAllPanels()
    {
        foreach (var kv in UIPanelDict)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }

        UIPanelDict.Clear();

        // 面板全没了，输入锁也该全部释放，避免残留锁死玩法操作
        panelInputLocks.Clear();
        InputManager.Instance.PopAllInputLocks();

        AppContext.UILoader.ReleaseAll();
    }

    #endregion

    public T GetPanel<T>() where T : BasePanel
    {
        string path = PanelPathResolver.Resolve<T>();
        return UIPanelDict.TryGetValue(path, out var panel) ? panel as T : null;
    }

    #region 输入锁

    /// <summary>面板路径 → 它压入的输入锁 token</summary>
    private readonly Dictionary<string, object> panelInputLocks = new Dictionary<string, object>();

    private void PushInputLock(string path, BasePanel panel)
    {
        if (panel == null || !panel.BlocksGameplayInput) return;
        if (panelInputLocks.ContainsKey(path)) return;
        panelInputLocks[path] = InputManager.Instance.PushInputLock();
    }

    private void PopInputLock(string path)
    {
        if (!panelInputLocks.TryGetValue(path, out var token)) return;
        InputManager.Instance.PopInputLock(token);
        panelInputLocks.Remove(path);
    }

    #endregion

    #region Canvas 初始化

    public override void Awake()
    {
        base.Awake();
        InitCanvas();
        InitEventSystem();
        isCanvasInitialized = true;
    }

    /// <summary>
    /// 弹出全局提示条。任何地方想弹 TipPanel 直接调 UIManager.Instance.ShowTip(...)，
    /// 不需要经过事件系统绕一圈。
    /// </summary>
    /// <param name="text">提示内容</param>
    /// <param name="showTime">大于 0 时覆盖面板默认显示时长</param>
    public void ShowTip(string text, float showTime = 0f)
    {
        OpenPanel<TipPanel>(panel =>
        {
            if (showTime > 0f) panel.showTime = showTime;
            panel.ShowTip(text);
        });
    }

    private void InitCanvas()
    {
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
    }

    private void InitEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

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

    #endregion

    private void OnDestroy()
    {
        DestroyAllPanels();
    }
}
