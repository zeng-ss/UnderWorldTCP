using UnityEngine;
using UnityEngine.SceneManagement;

// 离线调试引导：直接运行 GameScene 时跳过「登录 → 创建房间 → 大厅 → 进入游戏」整条联网链路，
// 补齐 AppContext 初始化 + 伪造 Session 数据，然后按 SceneMgr.OnSceneLoaded("GameScene") 的同样逻辑加载战斗资源。
// 仅在编辑器下生效（#if UNITY_EDITOR），不影响真机打包与正常联网流程。
public static class OfflineDebugLauncher
{
    // 离线调试用的假角色 id / 昵称
    private const int DebugRoleId = 1;
    private const string DebugNickname = "DebugPlayer";

    // 离线调试用的基础属性（正常流程由 GameManager 的 _basePlayerValueData 序列化配置提供）
    private const float DebugMaxHealth = 2000f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
#if UNITY_EDITOR
        // 只有当「当前直接就是 GameScene」且「AppContext 尚未初始化」时才走离线引导。
        // 正常流程：StartScene → 热更 → EnterScene(GameManager 建 AppContext) → ... → GameScene，
        // 切到 GameScene 时 AppContext 已存在，不会重复初始化。
        if (SceneManager.GetActiveScene().name != "GameScene") return;
        if (AppContext.IsAlive) return;

        Debug.Log("[OfflineDebugLauncher] 检测到直接运行 GameScene，进入离线调试模式");

        AppContext.Create();
        BootstrapOffline();

        // 伪造服务端角色数据，供 PlayerCtrl.Start 里的 InitializeFromServer / StartPositionSync 使用
        SessionService session = AppContext.Session;
        session.RoleId = DebugRoleId;
        session.PlayerName = DebugNickname;
        session.MainRoleInfo = new MainRoleInfo
        {
            BaseInfo = new RoleBaseInfo { RoleId = DebugRoleId, Nickname = DebugNickname }
        };

        // 与 SceneMgr.OnSceneLoaded("GameScene") 完全一致的战斗资源加载
        AppContext.RemotePlayer.ResetForNewScene();
        AppContext.Res.LoadAndInstantiateAsync("Character");
        AppContext.Res.LoadAndInstantiateAsync("NPC");
        AppContext.Res.LoadAndInstantiateAsync("GameController");
        AppContext.Events.EventTrigger(GameEvent.GameStart);

        Debug.Log("[OfflineDebugLauncher] 离线战斗场景就绪");
#endif
    }

    // 等价于 GameManager.BootstrapServices，但配置一律传 null（各 Service.Init 均容忍 null）
    private static void BootstrapOffline()
    {
        AppContext.Material.Init(null);
        AppContext.Depot.Init(null);
        AppContext.Task.Init(null);

        PlayerValueData baseValue = new PlayerValueData
        {
            ID = DebugRoleId,
            MaxHealthValue = DebugMaxHealth
        };
        AppContext.PlayerData.Init(baseValue);
        AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);

        AppContext.Ui.Init();
        AppContext.Sound.Init();
        AppContext.Proto.Init();
        AppContext.RemotePlayer.Init();
    }
}
