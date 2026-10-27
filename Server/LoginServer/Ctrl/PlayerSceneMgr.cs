using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 玩家场景管理器（LoginServer 内存中管理）。
/// 追踪每个玩家所在的场景与最新位置，用于位置广播，同时给敌人 AI 提供索敌坐标。
/// 会被【网络线程】与【tick 线程】并发访问，所有公共入口内部加锁。
/// </summary>
public class PlayerSceneMgr : Singleton<PlayerSceneMgr>
{
    private readonly object _lock = new object();

    // roleId → PlayerSceneInfo
    private readonly Dictionary<int, PlayerSceneInfo> _players = new Dictionary<int, PlayerSceneInfo>();

    // sessionId → roleId
    private readonly Dictionary<int, int> _sessionToRole = new Dictionary<int, int>();

    /// <summary>
    /// 玩家进入场景（已存在则只更新会话信息，不重置位置）
    /// </summary>
    public void OnPlayerEnter(int roleId, int sessionId, string nickname)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(roleId, out PlayerSceneInfo info))
            {
                info.SessionId = sessionId;
                info.Nickname = nickname;
                _sessionToRole[sessionId] = roleId;
                return;
            }

            _players[roleId] = new PlayerSceneInfo
            {
                RoleId = roleId,
                SessionId = sessionId,
                Nickname = nickname,
            };
            _sessionToRole[sessionId] = roleId;
        }

        LogMsg.Info($"[PlayerSceneMgr]玩家进入场景: roleId={roleId} nickname={nickname}");
    }

    /// <summary>
    /// 更新玩家战斗属性（血量上限 / 防御）。属性由客户端按「基础值 + 已装备驱动盘加成」算出后上报，
    /// 服务端需要同一份数值才能结算敌人攻击的伤害。
    /// 角色条目可能还没建立（属性上报早于首次位置同步），此处懒创建；后续 OnPlayerEnter 会把会话补上。
    /// </summary>
    public void SetPlayerStats(int roleId, float maxHp, float defense)
    {
        lock (_lock)
        {
            if (!_players.TryGetValue(roleId, out PlayerSceneInfo info))
            {
                info = new PlayerSceneInfo { RoleId = roleId };
                _players[roleId] = info;
            }

            if (maxHp < 1f) maxHp = 1f;

            if (!info.StatsInited)
            {
                // 首次上报：直接满血
                info.MaxHp = maxHp;
                info.CurrHp = maxHp;
            }
            else if (info.CurrHp > 0f)
            {
                // 已受伤：上限变化（换装 / 升级）时按当前血量比例折算，避免换装回血或暴毙
                float ratio = info.MaxHp > 0f ? info.CurrHp / info.MaxHp : 1f;
                float scaled = (float)Math.Round(ratio * maxHp, 2);
                info.CurrHp = Math.Min(maxHp, Math.Max(1f, scaled));
                info.MaxHp = maxHp;
            }
            else
            {
                // 已死亡：只更新上限，不复活
                info.MaxHp = maxHp;
            }

            info.Defense = defense;
            info.StatsInited = true;
        }
    }

    /// <summary>
    /// 复活：玩家重新进入场景（断线重连 / 重登）时回满血。返回是否发生了复活。
    /// </summary>
    public bool ReviveOnEnter(int roleId)
    {
        lock (_lock)
        {
            if (!_players.TryGetValue(roleId, out PlayerSceneInfo info)) return false;
            if (!info.StatsInited || info.CurrHp > 0f) return false;

            info.CurrHp = info.MaxHp;
            LogMsg.Info($"[PlayerSceneMgr]玩家重新进入场景，血量重置: roleId={roleId} hp={info.MaxHp}");
            return true;
        }
    }

    /// <summary>
    /// 结算一次玩家受击：按防御减伤后扣血。血量唯一可写处，敌人攻击的伤害只能从这里进去。
    /// 玩家不存在 / 属性未上报 / 已死亡时返回 false（调用方据此判定「这一拳挥空」）。
    /// </summary>
    public bool TryApplyHurt(int roleId, float rawDamage, out float dealtDamage, out float currHp,
        out float maxHp, out bool isDead)
    {
        dealtDamage = 0f;
        currHp = 0f;
        maxHp = 0f;
        isDead = false;

        lock (_lock)
        {
            if (!_players.TryGetValue(roleId, out PlayerSceneInfo info)) return false;
            if (!info.StatsInited) return false;
            if (info.CurrHp <= 0f) return false;

            // 减伤公式与客户端原有实现保持一致：伤害 - round(防御 / 10)，最低 0
            float reduce = (float)Math.Round(info.Defense / 10f, 2);
            float damage = rawDamage - reduce;
            if (damage < 0f) damage = 0f;

            info.CurrHp -= damage;
            if (info.CurrHp <= 0f)
            {
                info.CurrHp = 0f;
                isDead = true;
            }

            dealtDamage = damage;
            currHp = info.CurrHp;
            maxHp = info.MaxHp;
            return true;
        }
    }

    /// <summary>取玩家当前位置（敌人结算命中距离用）。玩家不存在时返回 false。</summary>
    public bool TryGetPlayerPos(int roleId, out float px, out float py, out float pz)
    {
        px = py = pz = 0f;

        lock (_lock)
        {
            if (!_players.TryGetValue(roleId, out PlayerSceneInfo info)) return false;

            px = info.PosX;
            py = info.PosY;
            pz = info.PosZ;
            return true;
        }
    }

    /// <summary>
    /// 更新玩家位置（位置同步时记录，敌人 AI 索敌与进入场景通知都读这里）
    /// </summary>
    public void UpdatePosition(int roleId, float posX, float posY, float posZ, float rotationY)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(roleId, out PlayerSceneInfo info))
            {
                info.PosX = posX;
                info.PosY = posY;
                info.PosZ = posZ;
                info.RotationY = rotationY;
            }
        }
    }

    /// <summary>
    /// 玩家离开场景，返回离开前的信息用于广播
    /// </summary>
    public PlayerSceneInfo OnPlayerLeave(int roleId)
    {
        lock (_lock)
        {
            if (_players.TryGetValue(roleId, out PlayerSceneInfo info))
            {
                _sessionToRole.Remove(info.SessionId);
                _players.Remove(roleId);
                LogMsg.Info($"[PlayerSceneMgr]玩家离开场景: roleId={roleId}");
                return info;
            }
        }

        return null;
    }

    /// <summary>
    /// 通过sessionId查找roleId
    /// </summary>
    public int GetRoleIdBySession(int sessionId)
    {
        lock (_lock)
        {
            _sessionToRole.TryGetValue(sessionId, out int roleId);
            return roleId;
        }
    }

    /// <summary>
    /// 获取同一场景的其他玩家（用于广播）。返回快照，调用方可在锁外遍历。
    /// </summary>
    public List<PlayerSceneInfo> GetOtherPlayersInScene(int roleId)
    {
        lock (_lock)
        {
            if (!_players.ContainsKey(roleId)) return new List<PlayerSceneInfo>();

            List<PlayerSceneInfo> result = new List<PlayerSceneInfo>(_players.Count);
            foreach (KeyValuePair<int, PlayerSceneInfo> kv in _players)
            {
                if (kv.Key != roleId) result.Add(kv.Value);
            }

            return result;
        }
    }

    /// <summary>
    /// 获取同一场景的所有玩家sessionId（含自己），用于广播。返回快照。
    /// </summary>
    public List<int> GetSessionIdsInScene()
    {
        lock (_lock)
        {
            List<int> ids = new List<int>(_players.Count);
            foreach (KeyValuePair<int, PlayerSceneInfo> kv in _players)
            {
                ids.Add(kv.Value.SessionId);
            }

            return ids;
        }
    }

    /// <summary>
    /// 找出距离指定点最近的「活着的」玩家，供敌人 AI 索敌使用。没有任何可打目标时返回 false。
    /// 已死亡的玩家不再被索敌（敌人会转而追别人）；还没上报过位置的玩家也不参与，避免敌人全挤向原点。
    /// </summary>
    /// <summary>指定玩家是否还活着（不在场景里 / 血量归零都算死）</summary>
    public bool IsPlayerAlive(int roleId)
    {
        lock (_lock)
        {
            if (!_players.TryGetValue(roleId, out PlayerSceneInfo info)) return false;
            if (!info.StatsInited) return true; // 还没上报属性，先当作活着
            return info.CurrHp > 0f;
        }
    }

    public bool TryGetNearestPlayer(float x, float y, float z, out int roleId, out float px, out float py, out float pz)
    {
        roleId = 0;
        px = py = pz = 0f;
        bool found = false;
        float minSq = float.MaxValue;

        lock (_lock)
        {
            foreach (KeyValuePair<int, PlayerSceneInfo> kv in _players)
            {
                PlayerSceneInfo info = kv.Value;
                // 还没上报过位置的玩家（坐标为 0）不参与索敌
                if (info.PosX == 0f && info.PosY == 0f && info.PosZ == 0f) continue;
                // 属性上报过且血量归零 = 已死亡，不再是索敌目标
                if (info.StatsInited && info.CurrHp <= 0f) continue;

                float dx = info.PosX - x;
                float dy = info.PosY - y;
                float dz = info.PosZ - z;
                float sq = dx * dx + dy * dy + dz * dz;
                if (sq >= minSq) continue;

                minSq = sq;
                roleId = info.RoleId;
                px = info.PosX;
                py = info.PosY;
                pz = info.PosZ;
                found = true;
            }
        }

        return found;
    }

    public void SyncAni(int roleId, string animationName, int skillConfigIndex = 0)
    {
        SyncAniRet ret = new SyncAniRet
            { AnimationName = animationName, RoleId = roleId, SkillConfigIndex = skillConfigIndex };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_SyneAniCode, Data = ret.ToByteString() };

        List<PlayerSceneInfo> others = GetOtherPlayersInScene(roleId);
        foreach (PlayerSceneInfo other in others)
        {
            SessionMgr.Instance.GetSession(other.SessionId)?.SendData(pkg);
        }
    }

    /// <summary>返回玩家快照（roleId → 信息），供外部只读遍历。</summary>
    public Dictionary<int, PlayerSceneInfo> GetAllPlayers()
    {
        lock (_lock)
        {
            return new Dictionary<int, PlayerSceneInfo>(_players);
        }
    }

    public void BroadcastPlayerVfx(PlayerVfxNtf ntf)
    {
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_PlayerVfxCode, Data = ntf.ToByteString() };

        List<PlayerSceneInfo> others = GetOtherPlayersInScene(ntf.RoleId);
        foreach (PlayerSceneInfo other in others)
        {
            SessionMgr.Instance.GetSession(other.SessionId)?.SendData(pkg);
        }
    }
}

public class PlayerSceneInfo
{
    public int RoleId;
    public int SessionId;
    public string Nickname;
    public int MapId;
    public float PosX;
    public float PosY;
    public float PosZ;
    public float RotationY;

    // —— 会话内战斗状态（服务端权威，不落库）——
    // 属性由客户端上报（MaxHp / Defense），血量只在服务端结算，客户端只能被动接收。
    public float MaxHp = 1000f;
    public float CurrHp = 1000f;
    public float Defense;
    public bool StatsInited;

    public bool IsDead => StatsInited && CurrHp <= 0f;
}
