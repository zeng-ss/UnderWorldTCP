using HotUpdate.Core;
using HotUpdate.Event;
using HotUpdate.Manager;
using HotUpdate.UI;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 战斗场景内的输入 Controller：光标锁定 + 面板快捷键。
    ///
    /// 面板快捷键不再逐个硬编码，统一按 <see cref="PanelShortcut.All"/> 注册 ——
    /// 和游戏内 HUD 底部快捷栏共用同一份总表，加面板只改总表一处。
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private bool _wantCursorLocked;
        private bool _forceUnlock;

        private void Start()
        {
            AppContext.Events.AddEventListener(GameEvent.CursorShow, OnCursorShow);
            AppContext.Events.AddEventListener(GameEvent.CursorHide, OnCursorHide);

            foreach (PanelShortcut shortcut in PanelShortcut.All)
            {
                InputManager.Instance.RegisterKeyDown(shortcut.Key, shortcut.Toggle);
            }

            // 调试用：强制解锁光标
            // InputManager.Instance.RegisterKeyDown(KeyCode.K, ToggleForceUnlock);

            // 预加载任务面板的预制体资源，避免首次按 Tab 时才加载造成卡顿。
            AppContext.UILoader.LoadAsync(PanelPathResolver.Resolve<TaskPanel>(), null);

            // 进战斗场景默认锁光标 —— 不锁的话鼠标不会带动视角，得先开一次面板再关掉才能转镜头
            _wantCursorLocked = true;
            ApplyCursorState();
        }

        private void OnDestroy()
        {
            AppContext.Events.RemoveEventListener(GameEvent.CursorShow, OnCursorShow);
            AppContext.Events.RemoveEventListener(GameEvent.CursorHide, OnCursorHide);

            foreach (PanelShortcut shortcut in PanelShortcut.All)
            {
                InputManager.Instance.UnregisterKeyDown(shortcut.Key, shortcut.Toggle);
            }

            InputManager.Instance.UnregisterKeyDown(KeyCode.K, ToggleForceUnlock);
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
            bool locked = !_forceUnlock && _wantCursorLocked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            // 未锁定时必须把光标放出来，否则会出现「鼠标能动但看不见、点不到 UI」
            Cursor.visible = !locked;
        }

        #endregion
    }
}