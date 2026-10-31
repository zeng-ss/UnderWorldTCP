using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Service;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HotUpdate.Launcher
{
    // 离线调试引导：直接运行 GameScene 时跳过「登录 → 创建房间 → 大厅 → 进入游戏」整条联网链路，
    // 让战斗场景开箱即可试玩（调手感 / 调相机 / 调技能）。
    //
    // 与真实流程的差别只有「没有服务端」这一点：
    //   · AppContext.IsOffline = true —— 网络层空转，不会去连服务器；
    //   · 玩家属性用这里的调试数值，不走服务端下发；
    //   · 敌人本地生成、伤害本地按同一公式结算（见 PlayerCombat.BuildOfflineAttackResult）。
    public static class OfflineDebugLauncher
    {
        // 离线调试用的假角色 id / 昵称
        private const int DebugRoleId = 1;
        private const string DebugNickname = "DebugPlayer";

        // 离线调试用的基础属性（正常流程由 GameManager 的 _basePlayerValueData 序列化配置提供）
        private const float DebugMaxHealth = 2000f;
        private const float DebugAttack = 200f;
        private const float DebugDefense = 10f;
        private const float DebugBaoji = 10f;
        private const float DebugExAttack = 500f;

        // 离线木桩：数量 / 血量 / 围绕玩家的距离
        private const int DummyCount = 1;
        private const float DummyHealth = 50000f;
        private const float DummyRadius = 6f;
        private const int DummyInstanceIdBase = 9000;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
#if UNITY_EDITOR
            // 只有当「当前直接就是 GameScene」且「AppContext 尚未初始化」时才走离线引导。
            // 正常流程：StartScene → 热更 → EnterScene(GameManager 建 AppContext) → ... → GameScene，
            // 切到 GameScene 时 AppContext 已存在，不会重复初始化。
            if (SceneManager.GetActiveScene().name != "GameScene") return;
            if (AppContext.IsAlive) return;

            Debug.Log("[OfflineDebugLauncher] 检测到直接运行 GameScene，进入离线调试模式");

            // 必须在任何网络对象被创建之前置位：NetClientMgr 会按它决定要不要连服务器
            AppContext.IsOffline = true;

            // 正常流程是 GameManager 建 AppContext 并 InitAll。直接跑 GameScene 时没有它，
            // 这里必须走完整初始化 —— Canvas / EventSystem / 音效池 / 配置表都在 InitAll 里。
            AppContext context = AppContext.Create();
            context.InitAll(BuildDebugPlayerValueData());

            ApplyDebugSession();

            // 与 SceneMgr.OnSceneLoaded("GameScene") 保持一致
            AppContext.RemotePlayer.ResetForNewScene();
            AppContext.Res.LoadAndInstantiateAsync("GameController");
            AppContext.Res.LoadAndInstantiateAsync("NPC");

            // 玩家落地后再在它周围摆几只木桩（敌人要站在地面上，所以先等玩家出来再算位置）
            AppContext.Res.LoadAndInstantiateAsync("Character", null, player =>
            {
                if (player == null) return;
                SpawnDebugEnemies(player.transform.position);
            });

            // 战斗 HUD：头像 / 血条 + 底部面板快捷栏
            AppContext.Ui.OpenPanel<PlayerProPanel>();

            LockCursor();

            Debug.Log("[OfflineDebugLauncher] 离线战斗场景就绪：" +
                      "WASD 移动 / 鼠标转视角 / 滚轮缩放 / 左键普攻 / 右键重击 / E 拼刀 / Shift 闪避 / Tab 任务");
#endif
        }

        // 伪造服务端角色数据，供 PlayerCtrl 里的 InitializeFromServer / StartPositionSync 使用
        private static void ApplyDebugSession()
        {
            SessionService session = AppContext.Session;
            session.RoleId = DebugRoleId;
            session.PlayerName = DebugNickname;
            session.MainRoleInfo = new MainRoleInfo
            {
                BaseInfo = new RoleBaseInfo { RoleId = DebugRoleId, Nickname = DebugNickname }
            };
        }

        private static PlayerValueData BuildDebugPlayerValueData()
        {
            return new PlayerValueData
            {
                ID = (ulong)DebugRoleId,
                MaxHealthValue = DebugMaxHealth,
                AttackValue = DebugAttack,
                DefenseValue = DebugDefense,
                BaoJiValue = DebugBaoji,
                ExAttackValue = DebugExAttack,
            };
        }

        // 在玩家周围摆几只「木桩」：它们不会移动也不会打人（行为由服务端驱动），
        // 但可以被打死，用来验证攻击手感、伤害飘字、震屏与相机。
        private static void SpawnDebugEnemies(Vector3 center)
        {
            for (int i = 0; i < DummyCount; i++)
            {
                float angle = i * Mathf.PI * 2f / DummyCount;
                Vector3 pos = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * DummyRadius;

                // 落到地面：从高处往下打一条射线，命中点就是站立位置
                if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 40f,
                        ~0, QueryTriggerInteraction.Ignore))
                    pos = hit.point;

                AppContext.Enemy.SpawnLocal(DummyInstanceIdBase + i, DummyHealth, pos);
            }
        }

        // 离线场景没有走「开面板 → 关面板」的光标流程，这里直接锁上，
        // 否则 PlayerCameraCtrl 判定光标未锁定会不响应鼠标（转不了视角）
        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}