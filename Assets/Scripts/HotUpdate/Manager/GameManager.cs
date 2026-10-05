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

    public override void Awake()
    {
        base.Awake();
        AppContext.Create();
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
        UIManager.Instance.OpenPanel<ExitPanel>();
        AppContext.Events.EventTrigger(GameEvent.CursorShow);
    }

    private void OnDestroy()
    {
        AppContext.Events.RemoveEventListener(GameEvent.EquippedChanged, OnEquippedChanged);
        InputManager.Instance.UnregisterKeyDown(KeyCode.Escape, OnEscapePressed);
    }

    private void OnApplicationQuit()
    {
        AppContext.Chat.Clear();
        ProtoHandler.Instance.RequestLeaveRoom(AppContext.Session.RoleId);
    }
}