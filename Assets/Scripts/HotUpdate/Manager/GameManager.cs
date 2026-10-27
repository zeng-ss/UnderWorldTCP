using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Manager
{
    /// <summary>
    /// 游戏组合根
    /// </summary>
    public class GameManager : UnitySingleTonMono<GameManager>
    {
        private AppContext _appContext;
        private PlayerValueData _basePlayerValueData;

        public override void Awake()
        {
            base.Awake();

            // 多余的那一份（场景里手摆的 GameManager 在重新进入场景时又实例化了一份）：
            // base.Awake 已经把它销毁了，必须立刻 return，否则会再建一整套运行时（多 Canvas / 多池子根节点）
            if (IsDuplicateInstance) return;

            _appContext = AppContext.Create();
            _appContext.InitAll(_basePlayerValueData);
            RegisterGlobalHotkeys();
        }

        private void Update()
        {
            AppContext.Proto.Tick();
        }

        /// <summary>
        /// 注册在任何场景下都生效的全局快捷键。
        /// </summary>
        private void RegisterGlobalHotkeys()
        {
            InputManager.Instance.RegisterKeyDown(KeyCode.Escape, OnEscapePressed);
        }

        private void OnEscapePressed()
        {
            AppContext.Ui.OpenPanel<ExitPanel>();
            AppContext.Events.EventTrigger(GameEvent.CursorShow);
        }

        private void OnDestroy()
        {
            // 重复实例没初始化过，没什么可拆的（这里以前会空引用）
            if (IsDuplicateInstance) return;

            InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
            _appContext?.Dispose();
            _appContext = null;
        }

        private void OnApplicationQuit()
        {
            AppContext.Chat.Clear();
            AppContext.Proto.RequestLeaveRoom(AppContext.Session.RoleId);
        }
    }
}