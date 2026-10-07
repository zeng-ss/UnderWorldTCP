using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/// <summary>
/// 协议处理层：统一管理请求发送、响应处理、事件广播。
/// 普通 MonoBehaviour，不再自己当单例 —— 由 AppContext 统一创建与持有，访问走 AppContext.Proto。
/// </summary>
public class ProtoHandler
{
    #region 位置同步状态 => 玩家

    private Transform _syncTarget;
    private Transform _syncRotationTarget;
    private float _syncTimer;
    private const float SyncInterval = 0.001f;
    private string _syncNickname;
    private int _syncRoleId;

    // 敌人
    private Transform _syncETarget;
    private Transform _syncERotationTarget;

    #endregion

    #region 事件

    public event Action<PositionSyncNtf> OnPositionSyncReceived;
    public event Action<PlayerEnterSceneNtf> OnPlayerEnterScene;
    public event Action<PlayerLeaveSceneNtf> OnPlayerLeaveScene;
    public event Action<RoomInfoNtf> OnRoomInfoChanged;
    public event Action<RoomStartGameNtf> OnRoomStartGame;
    public event Action<PlayerAttackRet> OnPlayerAttackBroadcast;
    public event Action<SyncAniRet> OnSyncAniReceived;
    public event Action<PlayerVfxNtf> OnPlayerVfxReceived;
    public event Action<EnemyPositionSyncRet> OnEnemyPositionSyncReceived;
    public event Action<EnemySyncAniRet> OnEnemySyncAniReceived;

    #endregion

    #region 回调存储

    private Action<RegistRet> _registCallback;
    private Action<loginRet> _loginCallback;
    private Action<GetServerListRet> _serverListCallback;
    private Action<loginGameServerRet> _loginGameServerCallback;
    private Action<CreateRoleRet> _createRoleCallback;
    private Action<StartGameRet> _startGameCallback;
    private Action<SaveRoleRet> _saveRoleCallback;
    private Action<ChangeSceneRet> _changeSceneCallback;
    private Action<SpawnEnemyRet> _spawnEnemyCallback;
    private readonly Dictionary<int, Action<PlayerAttackRet>> _playerAttackCallbacks = new();
    private int _nextAttackSeqId;
    private Action<GetRewardRet> _getRewardCallback;
    private Action<CreateRoomRet> _createRoomCallback;
    private Action<JoinRoomRet> _joinRoomCallback;
    private Action<SyncAniRet> _syncAniCallback;
    private Action<EnemySyncAniRet> _syncEAniCallback;

    #endregion

    #region 初始化

    // 场景/预制体里可能存在多个实例，只保留最早 Awake 的一个
    private static ProtoHandler _live;

    public void Init()
    {
        InitHandlers();
    }

    private void InitHandlers()
    {
        AppContext.Events.AddNetHandler(NetDefine.CMD_RegistCode, OnRegistResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_LoginCode, OnLoginResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_GetServerListCode, OnGetServerListResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_LoginGameServerCode, OnLoginGameServerResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_CreateRoleCode, OnCreateRoleResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_StartGameCode, OnStartGameResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_SaveRoleCode, OnSaveRoleResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_ChangeSceneCode, OnChangeSceneResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_SpawnEnemyCode, OnSpawnEnemyResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerAttackCode, OnPlayerAttackResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_GetRewardCode, OnGetRewardResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_CreateRoomCode, OnCreateRoomResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_JoinRoomCode, OnJoinRoomResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_RoomInfoCode, OnRoomInfoNtf);
        AppContext.Events.AddNetHandler(NetDefine.CMD_RoomStartGameCode, OnRoomStartGameNtf);
        AppContext.Events.AddNetHandler(NetDefine.CMD_ErrCode, OnErrorResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_PositionSyncCode, OnPositionSync);
        AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerEnterSceneCode, OnPlayerEnterSceneEvent);
        AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerLeaveSceneCode, OnPlayerLeaveSceneEvent);
        AppContext.Events.AddNetHandler(NetDefine.CMD_SyneAniCode, OnSyncAniResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_PlayerVfxCode, OnPlayerVfxResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_EnemyPositionSyncCode, OnEnemyPosSyncResult);
        AppContext.Events.AddNetHandler(NetDefine.CMD_SyneEnemyAniCode, OnEnemyAniSyncResult);
    }

    private void Update()
    {
        _syncTimer += Time.deltaTime;
        if (!(_syncTimer >= SyncInterval)) return;
        _syncTimer = 0;

        if (_syncETarget)
        {
            NetClientMgr.Instance.Send(NetDefine.CMD_PositionSyncCode, new PositionSyncReq
            {
                RoleId = _syncRoleId,
                PosX = _syncTarget.position.x,
                PosY = _syncTarget.position.y,
                PosZ = _syncTarget.position.z,
                RotationY = _syncRotationTarget.eulerAngles.y,
                NikeName = _syncNickname
            }.ToByteString());
        }

        /*
        if (_syncETarget)
        {
            NetClientMgr.Instance.Send(NetDefine.CMD_EnemyPositionSyncCode, new EnemyPositionSyncReq
            {
                RoleId = _syncRoleId,
                PosX = _syncETarget.position.x,
                PosY = _syncETarget.position.y,
                PosZ = _syncETarget.position.z,
                RotationY = _syncERotationTarget.eulerAngles.y
            }.ToByteString());
        }
        */
    }

    #endregion

    #region 位置同步控制

    /// <summary>
    /// 开始定时向服务端发送位置信息（联机时在 StartGame 成功后调用）
    /// rotationTarget 用于同步模型旋转（playerModel.transform），不传则用 positionTarget 的旋转
    /// </summary>
    public void StartPositionSync(Transform positionTarget, int roleId, Transform rotationTarget = null)
    {
        MainRoleInfo info = AppContext.Session.MainRoleInfo;
        string nickname = info != null ? info.BaseInfo.Nickname : AppContext.Session.PlayerName;
        if (string.IsNullOrEmpty(nickname)) nickname = "Player" + roleId;

        _syncTarget = positionTarget;
        _syncRotationTarget = rotationTarget;
        _syncRoleId = roleId;
        _syncNickname = nickname;
        _syncTimer = 0;
    }

    public void StartEPositionSync(Transform positionTarget, int roleId, Transform rotationTarget = null)
    {
        _syncETarget = positionTarget;
        _syncERotationTarget = rotationTarget;
        _syncRoleId = roleId;
    }

    public void StopPositionSync()
    {
        _syncTarget = null;
        _syncRotationTarget = null;
    }

    public void StopEPositionSync()
    {
        _syncETarget = null;
        _syncERotationTarget = null;
    }

    #endregion

    #region 请求方法

    public void RequestSyncAni(int roleId, string aniName, int skillConfigIndex, Action<SyncAniRet> callback)
    {
        _syncAniCallback = callback;
        SyncAniReq req = new SyncAniReq
            { RoleId = roleId, AnimationName = aniName, SkillConfigIndex = skillConfigIndex };
        NetClientMgr.Instance.Send(NetDefine.CMD_SyneAniCode, req.ToByteString());
    }

    public void RequestSyncEnemyAni(int roleId, string aniName, Action<SyncAniRet> callback)
    {
        _syncAniCallback = callback;
        SyncAniReq req = new SyncAniReq { RoleId = roleId, AnimationName = aniName };
        NetClientMgr.Instance.Send(NetDefine.CMD_SyneAniCode, req.ToByteString());
    }

    public void RequestSyncVfx(PlayerVfxNtf ntf)
    {
        NetClientMgr.Instance.Send(NetDefine.CMD_PlayerVfxCode, ntf.ToByteString());
    }

    public void RequestRegist(string userName, string phoneNum, string password, Action<RegistRet> callback)
    {
        _registCallback = callback;
        RegistReq req = new RegistReq { UserName = userName, Password = password };
        NetClientMgr.Instance.Send(NetDefine.CMD_RegistCode, req.ToByteString());
    }

    public void RequestLogin(string userName, string password, Action<loginRet> callback)
    {
        _loginCallback = callback;
        LoginReq req = new LoginReq { UserName = userName, Password = password };
        NetClientMgr.Instance.Send(NetDefine.CMD_LoginCode, req.ToByteString());
    }

    public void RequestServerList(Action<GetServerListRet> callback)
    {
        _serverListCallback = callback;
        GetServerListReq req = new GetServerListReq { ServerId = 0 };
        NetClientMgr.Instance.Send(NetDefine.CMD_GetServerListCode, req.ToByteString());
    }

    public void RequestLoginGameServer(int accountId, int serverId, Action<loginGameServerRet> callback)
    {
        _loginGameServerCallback = callback;
        LoginGameServerReq req = new LoginGameServerReq { AccountId = accountId, GameServerId = serverId };
        NetClientMgr.Instance.Send(NetDefine.CMD_LoginGameServerCode, req.ToByteString());
    }

    public void RequestCreateRole(int accountId, int serverId, string nickname, int jobId,
        Action<CreateRoleRet> callback)
    {
        _createRoleCallback = callback;
        CreateRoleReq req = new CreateRoleReq
        {
            AccountId = accountId,
            GameServerId = serverId,
            Nickname = nickname,
            JobId = jobId
        };
        NetClientMgr.Instance.Send(NetDefine.CMD_CreateRoleCode, req.ToByteString());
    }

    public void RequestStartGame(int roleId, Action<StartGameRet> callback)
    {
        _startGameCallback = callback;
        StartGameReq req = new StartGameReq { RoleId = roleId };
        NetClientMgr.Instance.Send(NetDefine.CMD_StartGameCode, req.ToByteString());
    }

    public void RequestSaveRole(SaveRoleReq req, Action<SaveRoleRet> callback)
    {
        _saveRoleCallback = callback;
        NetClientMgr.Instance.Send(NetDefine.CMD_SaveRoleCode, req.ToByteString());
    }

    public void RequestChangeScene(int roleId, string sceneName, Action<ChangeSceneRet> callback)
    {
        _changeSceneCallback = callback;
        ChangeSceneReq req = new ChangeSceneReq { RoleId = roleId, SceneName = sceneName };
        NetClientMgr.Instance.Send(NetDefine.CMD_ChangeSceneCode, req.ToByteString());
    }

    public void RequestSpawnEnemy(int roleId, int enemyConfigId, float maxHp, Vector3 pos,
        Action<SpawnEnemyRet> callback)
    {
        _spawnEnemyCallback = callback;
        SpawnEnemyReq req = new SpawnEnemyReq
        {
            RoleId = roleId,
            EnemyConfigId = enemyConfigId,
            MaxHp = maxHp,
            PosX = pos.x,
            PosY = pos.y,
            PosZ = pos.z
        };
        NetClientMgr.Instance.Send(NetDefine.CMD_SpawnEnemyCode, req.ToByteString());
    }

    public void RequestPlayerAttack(int roleId, int enemyInstanceId, float damage, float baojiPercent, bool isExAttack,
        Action<PlayerAttackRet> callback)
    {
        int seqId = ++_nextAttackSeqId;
        _playerAttackCallbacks[seqId] = callback;
        PlayerAttackReq req = new PlayerAttackReq
        {
            RoleId = roleId,
            EnemyInstanceId = enemyInstanceId,
            Damage = damage,
            BaojiPercent = baojiPercent,
            IsExAttack = isExAttack,
            SequenceId = seqId
        };
        NetClientMgr.Instance.Send(NetDefine.CMD_PlayerAttackCode, req.ToByteString());
    }

    public void RequestGetReward(int rewardType, Action<GetRewardRet> callback)
    {
        _getRewardCallback = callback;
        GetRewardReq req = new GetRewardReq { RoleId = AppContext.Session.RoleId, RewardType = rewardType };
        NetClientMgr.Instance.Send(NetDefine.CMD_GetRewardCode, req.ToByteString());
    }

    /// <summary>
    /// 保存任务进度到服务端（任务状态机迁移时调用）。
    /// 注意：TaskProgressReq / TaskProgressRet 协议类尚未生成，本方法当前为桩实现，
    /// 等 proto 定义 TaskProgressReq 并重新编译后，再补上真实的网络发送。
    /// </summary>
    public void RequestSaveTaskProgress(int taskId, int state, int currentCount, Action<GetRewardRet> callback)
    {
        // TODO(任务持久化)：等 proto 生成 TaskProgressReq 后，替换为真实发送。
        // 当前仅记录日志，保证客户端状态机逻辑可独立编译运行。
        Debug.Log($"[Task] 保存任务进度 taskId={taskId} state={state} count={currentCount}（桩，未联网）");
    }

    public void RequestCreateRoom(int roleId, string roomName, string nickname,
        Action<CreateRoomRet> callback)
    {
        _createRoomCallback = callback;
        CreateRoomReq req = new CreateRoomReq
        {
            RoleId = roleId,
            RoomName = roomName,
            Nickname = nickname
        };
        NetClientMgr.Instance.Send(NetDefine.CMD_CreateRoomCode, req.ToByteString());
    }

    public void RequestJoinRoom(int roleId, int roomId, string nickname,
        Action<JoinRoomRet> callback)
    {
        _joinRoomCallback = callback;
        JoinRoomReq req = new JoinRoomReq
        {
            RoleId = roleId,
            RoomId = roomId,
            Nickname = nickname
        };
        NetClientMgr.Instance.Send(NetDefine.CMD_JoinRoomCode, req.ToByteString());
    }

    public void RequestLeaveRoom(int roleId)
    {
        LeaveRoomReq req = new LeaveRoomReq { RoleId = roleId };
        NetClientMgr.Instance.Send(NetDefine.CMD_LeaveRoomCode, req.ToByteString());
    }

    public void RequestRoomStartGame(int roleId)
    {
        RoomStartGameReq req = new RoomStartGameReq { RoleId = roleId };
        NetClientMgr.Instance.Send(NetDefine.CMD_RoomStartGameCode, req.ToByteString());
    }

    public void RequestPlayerReady(int roleId, bool isReady)
    {
        PlayerReadyReq req = new PlayerReadyReq { RoleId = roleId, IsReady = isReady };
        NetClientMgr.Instance.Send(NetDefine.CMD_PlayerReadyCode, req.ToByteString());
    }

    #endregion

    #region 响应处理

    private void OnRegistResult(ByteString data)
    {
        RegistRet ret = RegistRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 注册结果 CmdCode={ret.CmdCode}");
        _registCallback?.Invoke(ret);
        _registCallback = null;
    }

    private void OnLoginResult(ByteString data)
    {
        loginRet ret = loginRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 登录结果 CmdCode={ret.CmdCode}");
        _loginCallback?.Invoke(ret);
        _loginCallback = null;
    }

    private void OnGetServerListResult(ByteString data)
    {
        GetServerListRet ret = GetServerListRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 服务器列表 数量={ret.GameServers.Count}");
        _serverListCallback?.Invoke(ret);
        _serverListCallback = null;
    }

    private void OnLoginGameServerResult(ByteString data)
    {
        loginGameServerRet ret = loginGameServerRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 登录游戏服务器结果 CmdCode={ret.CmdCode}");
        _loginGameServerCallback?.Invoke(ret);
        _loginGameServerCallback = null;
    }

    private void OnCreateRoleResult(ByteString data)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 创建角色结果 CmdCode={ret.CmdCode}");
        _createRoleCallback?.Invoke(ret);
        _createRoleCallback = null;
    }

    private void OnStartGameResult(ByteString data)
    {
        StartGameRet ret = StartGameRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 开始游戏结果 CmdCode={ret.CmdCode}");
        _startGameCallback?.Invoke(ret);
        _startGameCallback = null;
    }

    private void OnSaveRoleResult(ByteString data)
    {
        SaveRoleRet ret = SaveRoleRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 保存角色结果 CmdCode={ret.CmdCode}");
        _saveRoleCallback?.Invoke(ret);
        _saveRoleCallback = null;
    }

    private void OnChangeSceneResult(ByteString data)
    {
        ChangeSceneRet ret = ChangeSceneRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 切换场景结果 CmdCode={ret.CmdCode}");
        _changeSceneCallback?.Invoke(ret);
        _changeSceneCallback = null;
    }

    private void OnSpawnEnemyResult(ByteString data)
    {
        SpawnEnemyRet ret = SpawnEnemyRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 敌人生成结果 instanceId={ret.EnemyInstanceId} pos=({ret.PosX},{ret.PosY},{ret.PosZ})");

        if (_spawnEnemyCallback != null)
        {
            // 请求者的回调（GameManager.SpawnEnemy）
            _spawnEnemyCallback.Invoke(ret);
            _spawnEnemyCallback = null;
        }
        else
        {
            // 服务端广播给其他人 → 直接生成敌人
            AppContext.RemotePlayer.SpawnEnemy(ret);
        }
    }

    private void OnPlayerAttackResult(ByteString data)
    {
        PlayerAttackRet ret = PlayerAttackRet.Parser.ParseFrom(data);
        Debug.Log(
            $"ProtoHandler: 攻击结果 attacker={ret.AttackerRoleId} enemy={ret.EnemyInstanceId} damage={ret.DamageDealt} isDead={ret.IsDead}");

        // 判断是不是自己的攻击（回包的 attacker_role_id = 本地 roleId，且请求中有匹配的回调）
        if (ret.AttackerRoleId == AppContext.Session.RoleId)
        {
            // 遍历字典找到匹配的回调（sequence_id 无法回传因为请求中没带）
            // 取字典中最旧的未处理回调
            if (_playerAttackCallbacks.Count > 0)
            {
                var keys = new List<int>(_playerAttackCallbacks.Keys);
                keys.Sort();
                int oldestKey = keys[0];
                var cb = _playerAttackCallbacks[oldestKey];
                _playerAttackCallbacks.Remove(oldestKey);
                cb?.Invoke(ret);
            }
        }
        else if (ret.AttackerRoleId > 0)
        {
            // 别人的攻击广播
            OnPlayerAttackBroadcast?.Invoke(ret);
        }
    }

    private void OnGetRewardResult(ByteString data)
    {
        GetRewardRet ret = GetRewardRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 奖励结果 CmdCode={ret.CmdCode}");
        _getRewardCallback?.Invoke(ret);
        _getRewardCallback = null;
    }

    private void OnCreateRoomResult(ByteString data)
    {
        CreateRoomRet ret = CreateRoomRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 创建房间结果 roomId={ret.RoomId}");
        _createRoomCallback?.Invoke(ret);
        _createRoomCallback = null;
    }

    private void OnJoinRoomResult(ByteString data)
    {
        JoinRoomRet ret = JoinRoomRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 加入房间结果 roomId={ret.RoomId} players={ret.Players.Count}");
        _joinRoomCallback?.Invoke(ret);
        _joinRoomCallback = null;
    }

    private void OnRoomInfoNtf(ByteString data)
    {
        RoomInfoNtf ntf = RoomInfoNtf.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 房间信息变更 roomId={ntf.RoomId} players={ntf.Players.Count}");
        OnRoomInfoChanged?.Invoke(ntf);
    }

    private void OnRoomStartGameNtf(ByteString data)
    {
        RoomStartGameNtf ntf = RoomStartGameNtf.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 房间开始游戏 roomId={ntf.RoomId}");
        OnRoomStartGame?.Invoke(ntf);
    }

    private void OnErrorResult(ByteString data)
    {
        ErrMsg err = ErrMsg.Parser.ParseFrom(data);
        Debug.LogError($"ProtoHandler: 收到错误 CmdCode={err.CmdCode}");
    }

    private void OnSyncAniResult(ByteString data)
    {
        SyncAniRet ret = SyncAniRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 同步动画 ：{ret.AnimationName}");
        if (_syncAniCallback != null)
        {
            _syncAniCallback.Invoke(ret);
            _syncAniCallback = null;
        }
        else
        {
            OnSyncAniReceived?.Invoke(ret);
        }
    }

    private void OnPlayerVfxResult(ByteString data)
    {
        PlayerVfxNtf ntf = PlayerVfxNtf.Parser.ParseFrom(data);
        Debug.Log(
            $"ProtoHandler: VFX同步 roleId={ntf.RoleId} skill={ntf.SkillConfigIndex} atk={ntf.AttackIndex} vfx={ntf.VfxIndex}");
        OnPlayerVfxReceived?.Invoke(ntf);
    }

    private void OnEnemyPosSyncResult(ByteString data)
    {
        EnemyPositionSyncRet ntf = EnemyPositionSyncRet.Parser.ParseFrom(data);
        OnEnemyPositionSyncReceived?.Invoke(ntf);
    }

    private void OnEnemyAniSyncResult(ByteString data)
    {
        EnemySyncAniRet ret = EnemySyncAniRet.Parser.ParseFrom(data);
        Debug.Log($"ProtoHandler: 同步动画 ：{ret.AnimationName}");
        if (_syncEAniCallback != null)
        {
            _syncEAniCallback.Invoke(ret);
            _syncEAniCallback = null;
        }
        else
        {
            OnEnemySyncAniReceived?.Invoke(ret);
        }
    }

    #endregion

    #region 位置同步 / 场景事件

    private void OnPositionSync(ByteString data)
    {
        PositionSyncNtf ntf = PositionSyncNtf.Parser.ParseFrom(data);
        OnPositionSyncReceived?.Invoke(ntf);
    }

    private void OnPlayerEnterSceneEvent(ByteString data)
    {
        PlayerEnterSceneNtf ntf = PlayerEnterSceneNtf.Parser.ParseFrom(data);
        OnPlayerEnterScene?.Invoke(ntf);
    }

    private void OnPlayerLeaveSceneEvent(ByteString data)
    {
        PlayerLeaveSceneNtf ntf = PlayerLeaveSceneNtf.Parser.ParseFrom(data);
        OnPlayerLeaveScene?.Invoke(ntf);
    }

    #endregion

    public void Clear()
    {
        // AppContext 可能已先一步 Dispose（退出流程），此时事件总线随它一起没了
        if (!AppContext.IsAlive) return;

        AppContext.Events.RemoveNetHandler(NetDefine.CMD_RegistCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_LoginCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_GetServerListCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_LoginGameServerCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_CreateRoleCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_StartGameCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_SaveRoleCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_ChangeSceneCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_SpawnEnemyCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerAttackCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_GetRewardCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_CreateRoomCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_JoinRoomCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomInfoCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_RoomStartGameCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_ErrCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_PositionSyncCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerEnterSceneCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerLeaveSceneCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_SyneAniCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_PlayerVfxCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_EnemyPositionSyncCode);
        AppContext.Events.RemoveNetHandler(NetDefine.CMD_SyneEnemyAniCode);
    }
}