using System;
using System.Collections.Generic;
using Google.Protobuf;

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
}

/// <summary>
/// 敌人实例
/// </summary>
public class EnemyInstance
{
    public int InstanceId;
    public int ConfigId;

    public float MaxHp;
    public float CurrHp;

    // 服务端权威变换
    public float PosX;
    public float PosY;
    public float PosZ;
    public float RotationY;

    // 服务端权威状态与动作
    public EnemyAiState State = EnemyAiState.Idle;
    public string Anim = "Idle";

    // 生成后的「待机」时间，过了才开始索敌（保留原来敌人先站一会儿的手感）
    public float AggroTimer;
    public float AggroDelay = 2f;

    // 攻击节奏
    public float AttackTimer;

    // 本次挥招的结算：出招瞬间锁定目标并倒计时，到点才判定命中（对齐动画挥到中段的时刻）
    public int TargetRoleId;
    public float PendingHurtTimer;

    // 广播限频
    public float SyncTimer;

    // 死亡后的清场计时
    public float DeadTimer;

    // AI 参数（后续可挪到 Luban 敌人配置表）
    public float MoveSpeed = 3.5f; // 追击速度（单位/秒）
    public float AttackRange = 2.5f; // 进入该距离即停下攻击
    public float ChaseDistance = 10f; // 超过该距离在客户端表现上算「奔跑」
    public float AttackInterval = 2.2f; // 两次攻击之间的最小间隔
    public float RemoveDelay = 6f; // 死亡后多久从服务端移除

    // 服务端结算用的攻击力。与客户端 enemy.prefab 上 HitData.damageValue 对齐（那边的值只用于表现参数，
    // 伤害数值一律以本字段为准，后续挪到 Luban 敌人配置表）。
    public float AttackDamage = 10f;
}

/// <summary>
/// 敌人实例管理器
/// 本类会在【网络线程】和【tick 线程】上被并发访问，所有公共入口内部加锁。
/// </summary>
public class EnemyMgr : Singleton<EnemyMgr>
{
    // 位置广播频率（秒）。10Hz 对插值跟随已经足够平滑。
    private const float PositionSyncInterval = 0.1f;

    // 从抬手到命中的延迟（秒）：动画挥到中段才结算，避免「刚抬手玩家就掉血」
    private const float AttackImpactDelay = 0.25f;

    // 命中距离容差：出招后玩家跑了这么远仍算被擦到
    private const float HitTolerance = 0.6f;

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
        int id;
        lock (_lock)
        {
            id = _nextInstanceId++;
            var enemy = new EnemyInstance
            {
                InstanceId = id,
                ConfigId = configId,
                MaxHp = maxHp,
                CurrHp = maxHp,
                PosX = posX,
                PosY = posY,
                PosZ = posZ,
                RotationY = 0f,
                State = EnemyAiState.Idle,
                Anim = "Idle",
            };
            _enemies[id] = enemy;
        }

        SpawnEnemyRet ret = new SpawnEnemyRet
        {
            CmdCode = CmdCode.Succeed,
            EnemyInstanceId = id,
            EnemyConfigId = configId,
            MaxHp = maxHp,
            CurrHp = maxHp,
            PosX = posX,
            PosY = posY,
            PosZ = posZ,
        };

        BroadcastAll(NetDefine.CMD_SpawnEnemyCode, ret.ToByteString());
        LogMsg.Info(
            $"[EnemyMgr]敌人生成: instanceId={id} configId={configId} pos=({posX:F1},{posY:F1},{posZ:F1}) maxHp={maxHp}",
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
            }

            ret.CmdCode = CmdCode.Succeed;
            ret.DamageDealt = damage;
            ret.EnemyCurrHp = enemy.CurrHp;
            ret.EnemyMaxHp = enemy.MaxHp;
            ret.IsDead = justDied;
            ret.IsBaoji = isBaoji;
        }

        // 死亡立刻广播一次动作，让所有客户端同步倒下（不用等下一个 tick）
        if (justDied) BroadcastAll(NetDefine.CMD_SyneEnemyAniCode, BuildAnimPackage(enemy).Data);

        LogMsg.Info(
            $"[EnemyMgr]攻击处理: enemyId={req.EnemyInstanceId} damage={ret.DamageDealt} hp={ret.EnemyCurrHp}/{ret.EnemyMaxHp} isDead={ret.IsDead} isBaoji={ret.IsBaoji}");
        return ret;
    }

    #endregion

    #region 逻辑帧

    /// <summary>
    /// 推进所有敌人的状态与位置，并广播需要同步的内容。
    /// 由 LoginServer 的固定频率循环驱动；dt 为距上一帧的秒数。
    /// </summary>
    public void Tick(float dt)
    {
        if (dt <= 0f) return;
        // 线程被挂起过久时钳制一下，避免敌人瞬移穿过玩家
        if (dt > 0.25f) dt = 0.25f;

        List<BasePackage> posPackets = new List<BasePackage>();
        List<BasePackage> animPackets = new List<BasePackage>();
        List<BasePackage> hurtPackets = new List<BasePackage>();
        List<int> toRemove = new List<int>();

        lock (_lock)
        {
            foreach (EnemyInstance enemy in _enemies.Values)
            {
                if (enemy.State == EnemyAiState.Dead)
                {
                    enemy.DeadTimer += dt;
                    if (enemy.DeadTimer >= enemy.RemoveDelay)
                    {
                        toRemove.Add(enemy.InstanceId);
                    }

                    continue;
                }

                // 先结算上一招的命中，再决定这一帧做什么
                ResolvePendingAttack(enemy, dt, hurtPackets);
                UpdateEnemy(enemy, dt, posPackets, animPackets);
            }

            foreach (int id in toRemove) _enemies.Remove(id);
        }

        if (posPackets.Count == 0 && animPackets.Count == 0 && hurtPackets.Count == 0) return;

        List<int> sessionIds = PlayerSceneMgr.Instance.GetSessionIdsInScene();
        if (sessionIds.Count == 0) return;

        SendToSessions(sessionIds, posPackets);
        SendToSessions(sessionIds, animPackets);
        // 受击广播发给场景内所有人（含被打者自己，他要更新血条与状态）
        SendToSessions(sessionIds, hurtPackets);
    }

    /// <summary>
    /// 结算一次挥招：出招时锁定了目标并起了倒计时，到点后校验「目标是否还在攻击距离内」——
    /// 玩家在这 0.25 秒里跑开就算挥空，打中则扣血并广播。
    /// 调用方必须已持有 _lock。
    /// </summary>
    private void ResolvePendingAttack(EnemyInstance enemy, float dt, List<BasePackage> hurtPackets)
    {
        if (enemy.PendingHurtTimer <= 0f) return;

        enemy.PendingHurtTimer -= dt;
        if (enemy.PendingHurtTimer > 0f) return;
        enemy.PendingHurtTimer = 0f;

        // 目标已死亡 / 已离开场景
        if (!PlayerSceneMgr.Instance.TryGetPlayerPos(enemy.TargetRoleId, out float px, out float py, out float pz))
            return;

        float dx = px - enemy.PosX;
        float dy = py - enemy.PosY;
        float dz = pz - enemy.PosZ;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

        // 玩家躲开了：这一招挥空
        if (dist > enemy.AttackRange + HitTolerance) return;

        // ±10% 浮动，避免每下都是同一个数字
        float rawDamage = enemy.AttackDamage * (0.9f + (float)_rand.NextDouble() * 0.2f);

        if (!PlayerSceneMgr.Instance.TryApplyHurt(enemy.TargetRoleId, rawDamage,
                out float dealtDamage, out float currHp, out float maxHp, out bool isDead)) return;

        PlayerHurtNtf ntf = new PlayerHurtNtf
        {
            RoleId = enemy.TargetRoleId,
            EnemyInstanceId = enemy.InstanceId,
            Damage = dealtDamage,
            CurrHp = currHp,
            MaxHp = maxHp,
            IsDead = isDead,
        };
        hurtPackets.Add(new BasePackage { ProtoCode = NetDefine.CMD_PlayerHurtCode, Data = ntf.ToByteString() });

        LogMsg.Info(
            $"[EnemyMgr]敌人命中玩家: enemyId={enemy.InstanceId} roleId={enemy.TargetRoleId} damage={dealtDamage:F2} hp={currHp:F2}/{maxHp:F2} isDead={isDead}",
            ConsoleColor.Yellow);
    }

    /// <summary>
    /// 单只敌人的 AI：找最近玩家 → 追击 / 保持距离 → 站定攻击。
    /// 调用方必须已持有 _lock。
    /// </summary>
    private void UpdateEnemy(EnemyInstance enemy, float dt, List<BasePackage> posPackets,
        List<BasePackage> animPackets)
    {
        // 出生后先发呆一小会儿再索敌
        if (enemy.AggroTimer < enemy.AggroDelay)
        {
            enemy.AggroTimer += dt;
            SetAnim(enemy, "Idle", EnemyAiState.Idle, animPackets);
            return;
        }

        if (!PlayerSceneMgr.Instance.TryGetNearestPlayer(enemy.PosX, enemy.PosY, enemy.PosZ,
                out int targetRoleId, out float targetX, out float targetY, out float targetZ))
        {
            // 场景里没人（或都死了）：原地待机
            SetAnim(enemy, "Idle", EnemyAiState.Idle, animPackets);
            PushPosition(enemy, dt, posPackets);
            return;
        }

        float dx = targetX - enemy.PosX;
        float dy = targetY - enemy.PosY;
        float dz = targetZ - enemy.PosZ;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

        // 水平面上始终面向玩家
        float horizSq = dx * dx + dz * dz;
        if (horizSq > 0.0001f)
        {
            enemy.RotationY = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
        }

        if (dist > enemy.AttackRange)
        {
            // 追击：沿水平方向直线靠近，并且在到达攻击距离时不过冲
            float horiz = (float)Math.Sqrt(horizSq);
            if (horiz > 0.0001f)
            {
                float step = enemy.MoveSpeed * dt;
                float allowed = horiz - enemy.AttackRange;
                if (allowed < 0f) allowed = 0f;
                if (step > allowed) step = allowed;
                enemy.PosX += dx / horiz * step;
                enemy.PosZ += dz / horiz * step;
            }

            // 没有独立的跑步动作，沿用地表最长的那段位移动画
            SetAnim(enemy, "Attack05Run", EnemyAiState.Attack, animPackets);
            PushPosition(enemy, dt, posPackets);
            return;
        }

        // 已进入攻击距离：站定，按间隔出招
        enemy.AttackTimer -= dt;
        if (enemy.AttackTimer <= 0f)
        {
            enemy.AttackTimer = enemy.AttackInterval;
            // 出招瞬间锁定目标并起命中倒计时，0.25 秒后再由 ResolvePendingAttack 结算
            enemy.TargetRoleId = targetRoleId;
            enemy.PendingHurtTimer = AttackImpactDelay;
            SetAnim(enemy, PickAttackAnim(), EnemyAiState.Attack, animPackets);
        }
        else
        {
            SetAnim(enemy, "Idle", EnemyAiState.Attack, animPackets);
        }

        PushPosition(enemy, dt, posPackets);
    }

    private string PickAttackAnim()
    {
        switch (_rand.Next(0, 4))
        {
            case 0: return "Attack01";
            case 1: return "Attack02";
            case 2: return "Attack03";
            default: return "Attack04";
        }
    }

    /// <summary>动作或状态变化时才产生一个同步包。调用方必须已持有 _lock。</summary>
    private static void SetAnim(EnemyInstance enemy, string anim, EnemyAiState state, List<BasePackage> animPackets)
    {
        if (enemy.Anim == anim && enemy.State == state) return;

        enemy.Anim = anim;
        enemy.State = state;
        animPackets.Add(BuildAnimPackage(enemy));
    }

    /// <summary>按固定频率产生位置同步包。调用方必须已持有 _lock。</summary>
    private static void PushPosition(EnemyInstance enemy, float dt, List<BasePackage> posPackets)
    {
        enemy.SyncTimer += dt;
        if (enemy.SyncTimer < PositionSyncInterval) return;
        enemy.SyncTimer = 0f;
        posPackets.Add(BuildPositionPackage(enemy));
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