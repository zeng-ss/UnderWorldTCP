using UnityEngine;

/// <summary>
/// 游戏组合根
/// </summary>
public class GameManager : UnitySingleTonMono<GameManager>
{
    [Header("静态配置")] [SerializeField] private MaterialDataSO materialData;
    [SerializeField] private DepotConfig depotConfig;
    [Header("当前解锁的任务列表")] [SerializeField] private TaskDataConfigSO taskConfigSo;
    private PlayerValueData _basePlayerValueData;
    private AppContext _appContext;

    public override void Awake()
    {
        base.Awake();
        _appContext = AppContext.Create();
        BootstrapServices();
        RegisterGlobalHotkeys();
        AppContext.Events.AddEventListener(GameEvent.EquippedChanged, OnEquippedChanged);
    }

    /// <summary>按依赖顺序初始化各个 Service</summary>
    private void BootstrapServices()
    {
        AppContext.Material.Init(materialData);
        AppContext.Depot.Init(depotConfig);
        AppContext.Task.Init(taskConfigSo);
        AppContext.PlayerData.Init(_basePlayerValueData ?? new PlayerValueData());
        // 用当前装备先算一次，保证 Current 一开始就有值而不是全 0
        AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
        AppContext.Ui.Init();
        AppContext.Sound.Init();
        AppContext.Proto.Init();
        AppContext.RemotePlayer.Init();
    }

    private void OnEquippedChanged(EventArgs args)
    {
        AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
    }

    /// <summary>
    /// 注册在任何场景下都生效的全局快捷键。
    /// </summary>
    private void RegisterGlobalHotkeys()
    {
        InputManager.Instance.RegisterKeyDown(KeyCode.Escape, OnEscapePressed);
        AppContext.TaskUI.Initialize();
    }

    private void OnEscapePressed()
    {
        AppContext.Ui.OpenPanel<ExitPanel>();
        AppContext.Events.EventTrigger(GameEvent.CursorShow);
    }

    private void OnDestroy()
    {
        AppContext.Events.RemoveEventListener(GameEvent.EquippedChanged, OnEquippedChanged);
        InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
        _appContext.Dispose();
    }

    private void OnApplicationQuit()
    {
        AppContext.Chat.Clear();
        AppContext.Proto.RequestLeaveRoom(AppContext.Session.RoleId);
    }
}