using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>
/// 玩家场景管理器（LoginServer 内存中管理）
/// 追踪每个玩家所在的场景，用于位置同步广播
/// </summary>
public class PlayerSceneMgr : Singleton<PlayerSceneMgr>
{
    /// <summary>
    /// roleId → PlayerSceneInfo
    /// </summary>
    private Dictionary<int, PlayerSceneInfo> _players = new Dictionary<int, PlayerSceneInfo>();

    /// <summary>
    /// sessionId → roleId
    /// </summary>
    private Dictionary<int, int> _sessionToRole = new Dictionary<int, int>();

    /// <summary>
    /// 玩家进入场景（已存在则只更新会话信息，不重置位置）
    /// </summary>
    public void OnPlayerEnter(int roleId, int sessionId, string nickname)
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
        LogMsg.Info($"[PlayerSceneMgr]玩家进入场景: roleId={roleId} nickname={nickname}");
    }

    /// <summary>
    /// 更新玩家位置（供位置同步时记录，广播进入场景通知时使用）
    /// </summary>
    public void UpdatePosition(int roleId, float posX, float posY, float posZ, float rotationY)
    {
        if (_players.TryGetValue(roleId, out PlayerSceneInfo info))
        {
            info.PosX = posX;
            info.PosY = posY;
            info.PosZ = posZ;
            info.RotationY = rotationY;
        }
    }

    /// <summary>
    /// 玩家离开场景，返回离开前的信息用于广播
    /// </summary>
    public PlayerSceneInfo OnPlayerLeave(int roleId)
    {
        if (_players.TryGetValue(roleId, out PlayerSceneInfo info))
        {
            _sessionToRole.Remove(info.SessionId);
            _players.Remove(roleId);
            LogMsg.Info($"[PlayerSceneMgr]玩家离开场景: roleId={roleId}");
        }
        return info;
    }

    /// <summary>
    /// 通过sessionId查找roleId
    /// </summary>
    public int GetRoleIdBySession(int sessionId)
    {
        _sessionToRole.TryGetValue(sessionId, out int roleId); return roleId;
    }

    /// <summary>
    /// 获取同一场景的其他玩家（用于广播）
    /// </summary>
    public List<PlayerSceneInfo> GetOtherPlayersInScene(int roleId)
    {
        if (!_players.TryGetValue(roleId, out _)) return new List<PlayerSceneInfo>();

        return (from kv in _players where kv.Key != roleId select kv.Value).ToList();
    }

    /// <summary>
    /// 获取同一场景的所有玩家sessionId（含自己），用于广播
    /// </summary>
    public List<int> GetSessionIdsInScene()
    {
        List<int> ids = new List<int>();
        foreach (var kv in _players)
        {
            ids.Add(kv.Value.SessionId);
        }
        return ids;
    }

    public void SyncAni(int roleId, string animationName, int skillConfigIndex = 0)
    {
        SyncAniRet ret = new SyncAniRet { AnimationName = animationName, RoleId = roleId, SkillConfigIndex = skillConfigIndex };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_SyneAniCode, Data = ret.ToByteString() };
        foreach (var kv in _players)
        {
            if (kv.Key == roleId) continue;
            SessionMgr.Instance.GetSession(kv.Value.SessionId).SendData(pkg);
        }
    }

    public Dictionary<int, PlayerSceneInfo> GetAllPlayers()
    {
        return _players;
    }

    public void BroadcastPlayerVfx(PlayerVfxNtf ntf)
    {
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_PlayerVfxCode, Data = ntf.ToByteString() };
        foreach (var kv in _players)
        {
            if (kv.Key == ntf.RoleId) continue;
            SessionMgr.Instance.GetSession(kv.Value.SessionId).SendData(pkg);
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
