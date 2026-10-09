using HotUpdate.Core;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 战斗场景内的输入 Controller。
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private PlayerDataPanel _playerDataPanel;
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

            // 预加载任务面板，避免首次按 Tab 时才加载造成卡顿
            AppContext.Ui.OpenPanel<TaskPanel>(_ => AppContext.Ui.ClosePanel<TaskPanel>());
        }

        private void OnDestroy()
        {
            AppContext.Events.RemoveEventListener(GameEvent.CursorShow, OnCursorShow);
            AppContext.Events.RemoveEventListener(GameEvent.CursorHide, OnCursorHide);

            InputManager.Instance.UnregisterKeyDown(KeyCode.K, ToggleForceUnlock);
            InputManager.Instance.UnregisterKeyDown(KeyCode.C, ToggleChatPanel);
            InputManager.Instance.UnregisterKeyDown(KeyCode.V, ToggleDepotPanel);
            InputManager.Instance.UnregisterKeyDown(KeyCode.B, TogglePlayerDataPanel);
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
            AppContext.Ui.TogglePanel<ChatPanel>();
        }

        private void ToggleDepotPanel()
        {
            AppContext.Ui.TogglePanel<DepotPanel>();
        }

        private void TogglePlayerDataPanel()
        {
            _playerDataPanel ??= AppContext.Ui.GetPanel<PlayerDataPanel>();
            if (_playerDataPanel.isAnimating) return;
            if (!_playerDataPanel.gameObject.activeInHierarchy) AppContext.Ui.OpenPanel<PlayerDataPanel>();
            else AppContext.Ui.ClosePanel<PlayerDataPanel>();
        }

        #endregion
    }
}