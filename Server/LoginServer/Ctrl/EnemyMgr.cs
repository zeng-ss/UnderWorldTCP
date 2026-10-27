using System;
using System.Collections.Generic;
using Google.Protobuf;
using cfg;

/// <summary>
/// 敌人状态机。数值必须与客户端 HotUpdate.Event.EnemyStateType 一一对齐，
/// 因为服务端是把这个 int 直接下发、客户端据此决定表现。
/// </summary>
public enum EnemyAiState
{
    Idle = 0,
    Attack = 1,
    Hurt = 2,
    Dead = 3,
    Move = 4, // 房主上报的「走位/冲刺」，服务端只转发不判断
}

/// <summary>
/// 敌人实例（服务端只存一份「逻辑副本」）
/// 位置 / 朝向 / 动作都由房主客户端上报，服务端不模拟、不推进。
/// </summary>
public class EnemyInstance
{
    public int InstanceId;
    public int ConfigId;

    public float MaxHp;
    public float CurrHp;

    // 逻辑位置 / 朝向（由房主客户端上报，服务端只做跳变校验）
    public float PosX;
    public float PosY;
    public float PosZ;
    public float RotationY;

    // 逻辑状态：动作由房主客户端上报，这里只是转发用的一份拷贝
    public EnemyAiState State = EnemyAiState.Idle;
    public string Anim = "Idle";

    // 死亡后的清场计时
    public float DeadTimer;

    // 是否已经收到过宿主上报（第一次不做跳变校验）
    public bool HasHostSync;

    // 服务端校验攻击距离用的参数（来自 Luban 敌人表）
    public float AttackRange = 2.5f;

    // 死亡后多久从服务端移除
    public const float RemoveDelay = 6f;

    // 服务端结算用的攻击力。由 Luban 敌人表下发
    public float AttackDamage = 10f;

    // 最后一击的角色：只有这个人上报击杀才算数，避免同屏其他人蹭掉落
    public int LastAttackerRoleId;

    // 掉落只能领一次
    public bool DropClaimed;
}

/// <summary>
/// 敌人实例管理器 —— Owner（房主）模式。
/// </summary>
public class EnemyMgr : Singleton<EnemyMgr>
{
    private readonly object _lock = new object();
    private readonly Dictionary<int, EnemyInstance> _enemies = new Dictionary<int, EnemyInstance>();
    private readonly Random _rand = new Random();
    private int _nextInstanceId = 1;

    #region 生成 / 移除

    /// <summary>
    /// 生成敌人并广播给场景内所有人，返回服务端分配的实例ID。
    /// </summary>
    public int SpawnEnemy(int configId, float maxHp, float posX, float posY, float posZ)
    {
        // 数值全部以 Luban 敌人表为准；表里没有这个 id 时才退回调用方传进来的血量
        enemyData config = LubanMgr.Instance.GetEnemyDataById(configId);
        float realMaxHp = config?.MaxHp ?? maxHp;

        int id;
        lock (_lock)
        {
            id = _nextInstanceId++;
            var enemy = new EnemyInstance
            {
                InstanceId = id,
                ConfigId = configId,
                MaxHp = realMaxHp,
                CurrHp = realMaxHp,
                PosX = posX,
                PosY = posY,
                PosZ = posZ,
                RotationY = 0f,
                State = EnemyAiState.Idle,
                Anim = "Idle",
            };

            if (config != null)
            {
                // 只取「服务端校验攻击」需要的两项；移速 / 追击距离 / 攻击间隔都是房主 AI 的事
                enemy.AttackDamage = config.Attack;
                enemy.AttackRange = config.AttackRange;
            }

            _enemies[id] = enemy;
        }

        SpawnEnemyRet ret = new SpawnEnemyRet
        {
            CmdCode = CmdCode.Succeed,
            EnemyInstanceId = id,
            EnemyConfigId = configId,
            MaxHp = realMaxHp,
            CurrHp = realMaxHp,
            PosX = posX,
            PosY = posY,
            PosZ = posZ,
            Model = config != null ? config.Model : string.Empty,
            Name = config != null ? config.Name : string.Empty,
        };

        BroadcastAll(NetDefine.CMD_SpawnEnemyCode, ret.ToByteString());

        string configName = config != null ? config.Name : "未配置";
        LogMsg.Info(
            $"[EnemyMgr]敌人生成: instanceId={id} configId={configId} name={configName} pos=({posX:F1},{posY:F1},{posZ:F1}) maxHp={realMaxHp}",
            ConsoleColor.Magenta);
        return id;
    }

    /// <summary>移除指定敌人（不广播，由调用方决定是否通知客户端）。</summary>
    public void RemoveEnemy(int instanceId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _enemies.Remove(instanceId);
        }

        if (removed) LogMsg.Info($"[EnemyMgr]敌人移除: instanceId={instanceId}");
    }

    /// <summary>清空所有敌人（切换场景 / 关服时调用）。</summary>
    public void ClearAllEnemies()
    {
        int count;
        lock (_lock)
        {
            count = _enemies.Count;
            _enemies.Clear();
        }

        LogMsg.Info($"[EnemyMgr]清空敌人: count={count}");
    }

    #endregion

    #region 攻击结算

    /// <summary>
    /// 处理玩家攻击：校验敌人存在 → 结算伤害与暴击 → 落血量 → 判定死亡。
    /// 伤害数值目前仍由客户端给出，但血量的唯一可写处在这里。
    /// </summary>
    public PlayerAttackRet ProcessAttack(PlayerAttackReq req)
    {
        PlayerAttackRet ret = new PlayerAttackRet
        {
            EnemyInstanceId = req.EnemyInstanceId,
            AttackerRoleId = req.RoleId,
        };

        EnemyInstance enemy;
        bool justDied = false;

        lock (_lock)
        {
            if (!_enemies.TryGetValue(req.EnemyInstanceId, out enemy))
            {
                ret.CmdCode = CmdCode.EnemyNotExist;
                return ret;
            }

            if (enemy.CurrHp <= 0)
            {
                ret.CmdCode = CmdCode.Succeed;
                ret.DamageDealt = 0;
                ret.EnemyCurrHp = 0;
                ret.EnemyMaxHp = enemy.MaxHp;
                ret.IsDead = true;
                return ret;
            }

            float damage = req.Damage;
            bool isBaoji = _rand.Next(1, 101) <= req.BaojiPercent;
            if (isBaoji) damage += _rand.Next(200, 1501);

            enemy.CurrHp -= damage;
            if (enemy.CurrHp <= 0)
            {
                enemy.CurrHp = 0;
                justDied = true;
                enemy.State = EnemyAiState.Dead;
                enemy.Anim = "Dead";
                enemy.DeadTimer = 0f;
                enemy.LastAttackerRoleId = req.RoleId;
            }

            ret.CmdCode = CmdCode.Succeed;
            ret.DamageDealt = damage;
            ret.EnemyCurrHp = enemy.CurrHp;
            ret.EnemyMaxHp = enemy.MaxHp;
            ret.IsDead = justDied;
            ret.IsBaoji = isBaoji;
        }

        // 死亡立刻广播一次动作，让所有客户端同步倒下（不用等下一个 tick）
        if (justDied)
        {
            BroadcastAll(NetDefine.CMD_SyneEnemyAniCode, BuildAnimPackage(enemy).Data);
            BattleMgr.Instance.OnEnemyDead(enemy.InstanceId); // 计入这一局的波次进度
        }

        LogMsg.Info(
            $"[EnemyMgr]攻击处理: enemyId={req.EnemyInstanceId} damage={ret.DamageDealt} hp={ret.EnemyCurrHp}/{ret.EnemyMaxHp} isDead={ret.IsDead} isBaoji={ret.IsBaoji}");
        return ret;
    }

    #endregion

    #region 击杀掉落

    /// <summary>
    /// 结算一次击杀掉落：校验「敌人确实死了 + 是这个人打死的 + 还没领过」，
    /// 再按 Luban 敌人表的概率逐个 roll。返回 false 表示这次上报不合法（重复上报 / 冒领）。
    /// </summary>
    public bool ClaimKill(int roleId, int instanceId, List<DropItem> drops, out string enemyName)
    {
        drops.Clear();
        enemyName = string.Empty;

        lock (_lock)
        {
            if (!_enemies.TryGetValue(instanceId, out EnemyInstance enemy)) return false;
            if (enemy.CurrHp > 0 || enemy.DropClaimed) return false;
            if (enemy.LastAttackerRoleId != roleId) return false;

            // 先占位再 roll：同一个击杀只会发一次，重复上报直接挡掉
            enemy.DropClaimed = true;

            enemyData config = LubanMgr.Instance.GetEnemyDataById(enemy.ConfigId);
            if (config == null) return true; // 没配置就只当击杀有效，不给掉落

            enemyName = config.Name;

            // 数量表是「材料id → 数量」，概率表是「材料id → 百分比」；
            // 数量表里有、概率表里没配的，按必掉处理
            foreach (var kv in config.DropNums)
            {
                int itemId = kv.Key;
                int num = kv.Value;
                if (itemId <= 0 || num <= 0) continue;

                int chance = config.DropChances.TryGetValue(itemId, out int percent) ? percent : 100;
                if (chance <= 0 || _rand.Next(0, 100) >= chance) continue;

                drops.Add(new DropItem { ItemId = itemId, Count = num });
            }
        }

        LogMsg.Info($"[EnemyMgr]击杀掉落: instanceId={instanceId} roleId={roleId} enemy={enemyName} 掉落种类={drops.Count}");
        return true;
    }

    #endregion

    #region 宿主模式

    // 两次同步之间允许的最大位移：10Hz 上报，3 米 ≈ 30m/s，超了就是异常包
    private const float MaxSyncStep = 3f;

    /// <summary>
    /// 房主客户端上报的敌人状态：做一次「两次位置跳变过大」的简单校验，然后转发给其他人。
    /// </summary>
    public void ApplyHostSync(EnemyAiSyncReq req)
    {
        EnemyInstance enemy;
        bool reject = false;
        bool animChanged = false;

        lock (_lock)
        {
            if (!_enemies.TryGetValue(req.EnemyInstanceId, out enemy)) return;
            if (enemy.CurrHp <= 0f) return; // 死了不接受位移，防止尸体被推着走

            float dx = req.PosX - enemy.PosX;
            float dy = req.PosY - enemy.PosY;
            float dz = req.PosZ - enemy.PosZ;
            float moved = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            // 第一次上报直接接受；之后跳变过大就丢弃这一帧（不踢人，也不处罚）
            if (enemy.HasHostSync && moved > MaxSyncStep)
            {
                reject = true;
            }
            else
            {
                enemy.PosX = req.PosX;
                enemy.PosY = req.PosY;
                enemy.PosZ = req.PosZ;
                enemy.RotationY = req.RotationY;
                enemy.HasHostSync = true;

                // 动作只在变化时转发：否则远端每 0.1 秒重播一次同一个动画
                if (!string.IsNullOrEmpty(req.Anim) && enemy.Anim != req.Anim)
                {
                    enemy.Anim = req.Anim;
                    animChanged = true;
                }

                enemy.State = (EnemyAiState)req.State;
            }
        }

        if (reject)
        {
            LogMsg.Info($"[EnemyMgr]丢弃异常同步: enemyId={req.EnemyInstanceId} 位移过大", ConsoleColor.Red);
            return;
        }

        BroadcastAll(NetDefine.CMD_EnemyPositionSyncCode, BuildPositionPackage(enemy).Data);
        if (animChanged) BroadcastAll(NetDefine.CMD_SyneEnemyAniCode, BuildAnimPackage(enemy).Data);
    }

    /// <summary>
    /// 房主上报「武器碰撞盒打中了谁」：命中判定已经在客户端做完，服务端只结算伤害并广播。
    /// </summary>
    public void HandleHostAttack(EnemyAttackReq req)
    {
        float damage;

        lock (_lock)
        {
            if (!_enemies.TryGetValue(req.EnemyInstanceId, out EnemyInstance enemy)) return;
            if (enemy.CurrHp <= 0f) return;

            damage = enemy.AttackDamage * (0.9f + (float)_rand.NextDouble() * 0.2f);
        }

        if (!PlayerSceneMgr.Instance.TryApplyHurt(req.TargetRoleId, damage,
                out float dealtDamage, out float currHp, out float maxHp, out bool isDead)) return;

        PlayerHurtNtf ntf = new PlayerHurtNtf
        {
            RoleId = req.TargetRoleId,
            EnemyInstanceId = req.EnemyInstanceId,
            Damage = dealtDamage,
            CurrHp = currHp,
            MaxHp = maxHp,
            IsDead = isDead,
        };
        BroadcastAll(NetDefine.CMD_PlayerHurtCode, ntf.ToByteString());

        LogMsg.Info($"[EnemyMgr]敌人命中玩家(宿主碰撞盒): enemyId={req.EnemyInstanceId} roleId={req.TargetRoleId} " +
                    $"damage={dealtDamage:F2} hp={currHp:F2}/{maxHp:F2} isDead={isDead}", ConsoleColor.Yellow);
    }

    #endregion

    #region 死亡清理

    /// <summary>
    /// 服务端唯一的「每帧」逻辑：把死亡超过 RemoveDelay 的敌人从实例表里摘掉。
    /// 敌人 AI / 移动 / 出招判定全部在房主客户端（EnemyHostAI），服务端不再推进任何一项。
    /// </summary>
    public void CleanupDead(float dt)
    {
        if (dt <= 0f) return;
        if (dt > 0.25f) dt = 0.25f; // 线程被挂起过久时钳制一下

        lock (_lock)
        {
            List<int> toRemove = new List<int>();
            foreach (EnemyInstance enemy in _enemies.Values)
            {
                if (enemy.State != EnemyAiState.Dead) continue;

                enemy.DeadTimer += dt;
                if (enemy.DeadTimer >= EnemyInstance.RemoveDelay) toRemove.Add(enemy.InstanceId);
            }

            for (int i = 0; i < toRemove.Count; i++) _enemies.Remove(toRemove[i]);
        }
    }

    #endregion

    #region 广播

    private static BasePackage BuildPositionPackage(EnemyInstance enemy)
    {
        EnemyPositionSyncRet ret = new EnemyPositionSyncRet
        {
            EnemyInstanceId = enemy.InstanceId,
            PosX = enemy.PosX,
            PosY = enemy.PosY,
            PosZ = enemy.PosZ,
            RotationY = enemy.RotationY,
        };
        return new BasePackage { ProtoCode = NetDefine.CMD_EnemyPositionSyncCode, Data = ret.ToByteString() };
    }

    private static BasePackage BuildAnimPackage(EnemyInstance enemy)
    {
        EnemySyncAniRet ret = new EnemySyncAniRet
        {
            EnemyInstanceId = enemy.InstanceId,
            AnimationName = enemy.Anim,
            State = (int)enemy.State,
        };
        return new BasePackage { ProtoCode = NetDefine.CMD_SyneEnemyAniCode, Data = ret.ToByteString() };
    }

    private static void BroadcastAll(int protoCode, ByteString data)
    {
        List<int> sessionIds = PlayerSceneMgr.Instance.GetSessionIdsInScene();
        if (sessionIds.Count == 0) return;

        SendToSessions(sessionIds,
            new List<BasePackage> { new BasePackage { ProtoCode = protoCode, Data = data } });
    }

    private static void SendToSessions(List<int> sessionIds, List<BasePackage> packets)
    {
        if (packets.Count == 0) return;
        foreach (int sessionId in sessionIds)
        {
            Session session = SessionMgr.Instance.GetSession(sessionId);
            if (session == null) continue;
            // 同一份内容复用同一个包对象（SendData 只读它）
            foreach (BasePackage pkg in packets) session.SendData(pkg);
        }
    }

    #endregion
}