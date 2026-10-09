using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.Serialization;

namespace HotUpdate.Manager
{
    /// <summary>
    /// 游戏组合根
    /// </summary>
    public class GameManager : UnitySingleTonMono<GameManager>
    {
        private AppContext _appContext;

        [FormerlySerializedAs("_depotConfig")] [SerializeField]
        private DepotConfig depotConfig;

        [FormerlySerializedAs("_materialData")] [SerializeField]
        private MaterialDataSo materialData;

        [FormerlySerializedAs("_taskConfigSo")] [SerializeField]
        private TaskDataConfigSo taskConfigSo;

        private PlayerValueData _basePlayerValueData;

        public override void Awake()
        {
            base.Awake();
            _appContext = AppContext.Create();
            _appContext.InitAll(materialData, depotConfig, taskConfigSo, _basePlayerValueData);
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
            InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
            _appContext.Dispose();
        }

        private void OnApplicationQuit()
        {
            AppContext.Chat.Clear();
            AppContext.Proto.RequestLeaveRoom(AppContext.Session.RoleId);
        }
    }
}