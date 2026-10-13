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
    /// 找出距离指定点最近的玩家，供敌人 AI 索敌使用。没有任何玩家时返回 false。
    /// </summary>
    public bool TryGetNearestPlayer(float x, float y, float z, out float px, out float py, out float pz)
    {
        px = py = pz = 0f;
        bool found = false;
        float minSq = float.MaxValue;

        lock (_lock)
        {
            foreach (KeyValuePair<int, PlayerSceneInfo> kv in _players)
            {
                PlayerSceneInfo info = kv.Value;
                // 还没上报过位置的玩家（坐标为 0）不参与索敌，避免敌人全挤向原点
                if (info.PosX == 0f && info.PosY == 0f && info.PosZ == 0f) continue;

                float dx = info.PosX - x;
                float dy = info.PosY - y;
                float dz = info.PosZ - z;
                float sq = dx * dx + dy * dy + dz * dz;
                if (sq >= minSq) continue;

                minSq = sq;
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
}
