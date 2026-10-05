using UnityEngine;

/// <summary>
/// 战斗场景内的输入 Controller。
///
/// 所有按键统一注册到 InputManager，这里不再有 Update 每帧轮询 Input。
/// 光标锁定也改成响应 CursorShow / CursorHide 事件，而不是每帧去写 Cursor.lockState。
///
/// 原先这里还直接去操作 ImprovePanel / PlayerDataPanel 的数据，
/// 那些已交由 DepotController 与 PlayerDataService 处理。
/// </summary>
public class GameController : MonoBehaviour
{
    private bool _wantCursorLocked;
    private bool _forceUnlock;

    private void Start()
    {
        AppContext.Events.AddEventListener(GameEvent.CursorShow, OnCursorShow);
        AppContext.Events.AddEventListener(GameEvent.CursorHide, OnCursorHide);

        InputManager.Instance.RegisterKeyDown(KeyCode.K, ToggleForceUnlock);
        InputManager.Instance.RegisterKeyDown(KeyCode.C, ToggleChatPanel);
        InputManager.Instance.RegisterKeyDown(KeyCode.V, ToggleDepotPanel);
        InputManager.Instance.RegisterKeyDown(KeyCode.B, TogglePlayerDataPanel);
        InputManager.Instance.RegisterKeyDown(KeyCode.Alpha5, DebugFillImprove);

        // 预加载任务面板，避免首次按 Tab 时才加载造成卡顿
        AppContext.TaskUI.Preload();
    }

    private void OnDestroy()
    {
        AppContext.Events.RemoveEventListener(GameEvent.CursorShow, OnCursorShow);
        AppContext.Events.RemoveEventListener(GameEvent.CursorHide, OnCursorHide);

        InputManager.Instance.UnregisterKeyDown(KeyCode.K, ToggleForceUnlock);
        InputManager.Instance.UnregisterKeyDown(KeyCode.C, ToggleChatPanel);
        InputManager.Instance.UnregisterKeyDown(KeyCode.V, ToggleDepotPanel);
        InputManager.Instance.UnregisterKeyDown(KeyCode.B, TogglePlayerDataPanel);
        InputManager.Instance.UnregisterKeyDown(KeyCode.Alpha5, DebugFillImprove);
    }

    #region 光标

    private void OnCursorShow(EventArgs args)
    {
        _wantCursorLocked = false;
        ApplyCursorState();
    }

    private void OnCursorHide(EventArgs args)
    {
        _wantCursorLocked = true;
        ApplyCursorState();
    }

    private void ToggleForceUnlock()
    {
        _forceUnlock = !_forceUnlock;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = !_forceUnlock && _wantCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    #endregion

    #region 面板开关

    private void ToggleChatPanel()
    {
        UIManager.Instance.TogglePanel<ChatPanel>();
    }

    private void ToggleDepotPanel()
    {
        UIManager.Instance.TogglePanel<DepotPanel>();
    }

    private void TogglePlayerDataPanel()
    {
        var panel = UIManager.Instance.GetPanel<PlayerDataPanel>();
        if (panel != null && panel.isAnimating) return;

        if (panel == null || !panel.gameObject.activeInHierarchy) UIManager.Instance.OpenPanel<PlayerDataPanel>();
        else UIManager.Instance.ClosePanel<PlayerDataPanel>();
    }

    /// <summary>调试快捷键：给当前强化中的驱动盘加经验</summary>
    private void DebugFillImprove()
    {
        AppContext.DepotUI.DebugFill(100f);
    }

    #endregion
}
