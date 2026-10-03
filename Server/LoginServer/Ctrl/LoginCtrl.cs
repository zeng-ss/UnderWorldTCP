using System;
using Google.Protobuf;

public class LoginCtrl : IContainer
{
    #region 登录服务器作为客户端时

    /// <summary>
    /// 登录服务器作为客户端时，收到中心服务器发来的处理结果
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session session =
            SessionMgr.Instance.GetSession(basePackage.UnitySessionId);
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RegistCode:
                OnRegistResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_LoginCode:
                OnLoginResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_GetServerListCode:
                OnGetServerListResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_CreateRoleCode:
                OnCreateRoleResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_StartGameCode:
                OnStartGameResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_SaveRoleCode:
                OnSaveRoleResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_ChangeSceneCode:
                OnChangeSceneResultHandle(session, basePackage);
                break;
            /*case NetDefine.CMD_SpawnEnemyCode:
                OnSpawnEnemyResultHandle(session, basePackage);
                break;*/
            case NetDefine.CMD_PlayerAttackCode:
                OnPlayerAttackResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_GetRewardCode:
                OnGetRewardResultHandle(session, basePackage);
                break;
            default:
                LogMsg.Info("[LoginCtrl]中心服务器发过来的结果的请求码没有注册");
                break;
        }
    }

    private void OnGetRewardResultHandle(Session session, BasePackage basePackage)
    {
        GetRewardRet ret = GetRewardRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]获取奖励结果:" + ret);
        session.SendData(basePackage);
    }

    // 跳转场景返回
    private void OnChangeSceneResultHandle(Session session, BasePackage basePackage)
    {
        ChangeSceneRet ret = ChangeSceneRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]跳转场景结果:" + ret);
        session.SendData(basePackage);
    }

    // 保存角色返回
    private void OnSaveRoleResultHandle(Session session, BasePackage basePackage)
    {
        SaveRoleRet ret = SaveRoleRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]保存角色结果:" + ret);
        session.SendData(basePackage);
    }

    // 开始游戏返回
    private void OnStartGameResultHandle(Session session, BasePackage basePackage)
    {
        StartGameRet ret = StartGameRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]开始游戏结果:" + ret);
        session.SendData(basePackage);
    }

    // 登录游戏服务器返回数据
    private void OnLoginGameServerResultHandle(Session session, BasePackage basePackage)
    {
        loginGameServerRet ret = loginGameServerRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]登录游戏服务器结果:" + ret);
        session.SendData(basePackage);
    }

    // 创建角色返回数据
    private void OnCreateRoleResultHandle(Session session, BasePackage basePackage)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]创建角色结果:" + ret);
        session.SendData(basePackage);
    }

    // 获取服务端发过来的服务器列表信息
    private void OnGetServerListResultHandle(Session session, BasePackage basePackage)
    {
        GetServerListRet ret = GetServerListRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]获取服务器列表结果:" + ret);
        session.SendData(basePackage);
    }

    // 处理中心服务器登录请求返回的结果
    private void OnLoginResultHandle(Session session, BasePackage basePackage)
    {
        loginRet ret = loginRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]登录结果:" + ret);
        session.SendData(basePackage);
    }

    // 接收到服务端注册信息，处理注册结果的方法
    private void OnRegistResultHandle(ServerBase session, BasePackage basePackage)
    {
        RegistRet ret = RegistRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]注册结果:" + ret);
        session.SendData(basePackage);
    }

    // 玩家攻击-本地权威处理并回包+广播给同场景其他人
    private void OnPlayerAttackHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerAttackReq req = PlayerAttackReq.Parser.ParseFrom(basePackage.Data);
        if (!(serverBase is Session session)) return;

        PlayerAttackRet ret = EnemyMgr.Instance.ProcessAttack(req);
        LogMsg.Info($"[Login]攻击处理: roleId={req.RoleId} enemyId={req.EnemyInstanceId} damage={ret.DamageDealt} isDead={ret.IsDead}");

        // 回包给攻击者
        session.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());

        // 广播攻击结果给同场景其他玩家
        var others = PlayerSceneMgr.Instance.GetOtherPlayersInScene(req.RoleId);
        if (others.Count <= 0) return;
        BasePackage broadcastPkg = new BasePackage
        {
            ProtoCode = basePackage.ProtoCode,
            Data = ret.ToByteString()
        };
        foreach (var other in others)
        {
            SessionMgr.Instance.GetSession(other.SessionId)?.SendData(broadcastPkg);
        }
    }

    // 玩家攻击返回
    private void OnPlayerAttackResultHandle(Session session, BasePackage basePackage)
    {
        PlayerAttackRet ret = PlayerAttackRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }
        LogMsg.Info("[Login]玩家攻击结果:" + ret);
        session.SendData(basePackage);
    }

    public void OnInit()
    {
        SessionMgr.Instance.OnSessionDisconnected += OnSessionDisconnected;
    }

    private void OnSessionDisconnected(int sessionId)
    {
        // 优先用 RoomMgr 的 _sessionRole 清理房间（不依赖 PlayerSceneMgr）
        RoomMgr.Instance.LeaveRoomBySession(sessionId);

        // 再清理场景信息
        int roleId = PlayerSceneMgr.Instance.GetRoleIdBySession(sessionId);
        if (roleId > 0)
        {
            PlayerSceneMgr.Instance.OnPlayerLeave(roleId);
            PlayerLeaveSceneNtf ntf = new PlayerLeaveSceneNtf { RoleId = roleId };
            BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_PlayerLeaveSceneCode, Data = ntf.ToByteString() };
            var sessions = SessionMgr.Instance.GetAllSessions();
            foreach (var s in sessions)
            {
                if (s.SessionId != sessionId) s.SendData(pkg);
            }
        }
        LogMsg.Info($"[Login]玩家断线清理: sessionId={sessionId} roleId={roleId}");
    }

    #endregion

    #region 登录服务器作为服务端的时候

    /// <summary>
    /// 登录服务器作为服务端的时候，收到客户端发来的数据
    /// </summary>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_RegistCode:
                OnRegistHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_LoginCode:
                OnLoginHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_GetServerListCode:
                OnGetServerListHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_LoginGameServerCode:
                OnLoginGameServerHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_CreateRoleCode:
                OnCreateRoleHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_StartGameCode:
                OnStartGameHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SaveRoleCode:
                OnSaveRoleHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_ChangeSceneCode:
                OnChangeSceneHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SpawnEnemyCode:
                OnSpawnEnemyHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerAttackCode:
                OnPlayerAttackHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_GetRewardCode:
                OnGetRewardHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PositionSyncCode:
                OnPositionSyncHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_CreateRoomCode:
                OnCreateRoomHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_JoinRoomCode:
                OnJoinRoomHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_LeaveRoomCode:
                OnLeaveRoomHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_RoomStartGameCode:
                OnRoomStartGameHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerReadyCode:
                OnPlayerReadyHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SyneAniCode:
                OnSyncAniHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerVfxCode:
                OnPlayerVfxHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_EnemyPositionSyncCode:
                OnEnemyPositionSyncHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SyneEnemyAniCode:
                OnSyneEnemyAniHandle(serverBase, basePackage);
                break;
            default:
                LogMsg.Info("[LoginCtrl]有没有注册的请求码", ConsoleColor.Red);
                break;
        }
    }

    private void OnEnemyPositionSyncHandle(ServerBase serverBase, BasePackage basePackage)
    {
        EnemyPositionSyncReq req = EnemyPositionSyncReq.Parser.ParseFrom(basePackage.Data);
        EnemyPositionSyncRet ret = new EnemyPositionSyncRet
        {
            RoleId = req.RoleId,
            PosX = req.PosX,
            PosY = req.PosY,
            PosZ = req.PosZ,
            RotationY = req.RotationY 
        };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_EnemyPositionSyncCode, Data = ret.ToByteString() };
        foreach (var player in PlayerSceneMgr.Instance.GetOtherPlayersInScene(req.RoleId))
        {
            SessionMgr.Instance.GetSession(player.SessionId).SendData(pkg);
        }
    }

    private void OnSyneEnemyAniHandle(ServerBase serverBase, BasePackage basePackage)
    {
        EnemySyncAniRet req = EnemySyncAniRet.Parser.ParseFrom(basePackage.Data);
        EnemySyncAniRet ret = new EnemySyncAniRet
        {
            RoleId = req.RoleId,
            AnimationName = req.AnimationName,
        };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_SyneEnemyAniCode, Data = ret.ToByteString() };
        foreach (var player in PlayerSceneMgr.Instance.GetOtherPlayersInScene(req.RoleId))
        {
            SessionMgr.Instance.GetSession(player.SessionId).SendData(pkg);
        }
    }

    private void OnSyncAniHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SyncAniReq req = SyncAniReq.Parser.ParseFrom(basePackage.Data);
        PlayerSceneMgr.Instance.SyncAni(req.RoleId, req.AnimationName, req.SkillConfigIndex);
        LogMsg.Info("[Login]收到动画同步请求:" + req);
    }

    private void OnPlayerVfxHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerVfxNtf ntf = PlayerVfxNtf.Parser.ParseFrom(basePackage.Data);
        PlayerSceneMgr.Instance.BroadcastPlayerVfx(ntf);
        LogMsg.Info($"[Login]VFX同步: roleId={ntf.RoleId} config={ntf.SkillConfigIndex} atk={ntf.AttackIndex} vfx={ntf.VfxIndex}");
    }

    // 敌人生成-在服务端创建并广播给所有客户端
    private void OnSpawnEnemyHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SpawnEnemyReq req = SpawnEnemyReq.Parser.ParseFrom(basePackage.Data);

        float maxHp = req.MaxHp > 0 ? req.MaxHp : 10000;
        float posX = req.PosX; // 允许0值，用客户端实际位置
        float posY = req.PosY;
        float posZ = req.PosZ;
        EnemyMgr.Instance.SpawnEnemy(req.RoleId, req.EnemyConfigId, maxHp, posX, posY, posZ);
        LogMsg.Info("[Login]收到敌人生成请求:" + req);
    }

    private void OnGetRewardHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetRewardReq req = GetRewardReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到获得奖励请求:" + req);
    }

    // 位置同步处理（LoginServer直接广播，不经过CenterServer）
    private void OnPositionSyncHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PositionSyncReq req = PositionSyncReq.Parser.ParseFrom(basePackage.Data);
        //if (!(serverBase is Session selfSession))
        Session selfSession = serverBase as Session;

        int roleId = req.RoleId;
        bool isFirstSync = PlayerSceneMgr.Instance.GetRoleIdBySession(selfSession.SessionId) == 0;

        // 注册/更新玩家场景信息与最新位置
        PlayerSceneMgr.Instance.OnPlayerEnter(roleId, selfSession.SessionId, req.NikeName);
        PlayerSceneMgr.Instance.UpdatePosition(roleId, req.PosX, req.PosY, req.PosZ, req.RotationY);

        // 构建位置同步广播包
        PlayerSceneMgr.Instance.GetOtherPlayersInScene(roleId).ForEach(other =>
        {
            Session otherSession = SessionMgr.Instance.GetSession(other.SessionId);
            if (otherSession == null) return;
            PositionSyncNtf ntf = new PositionSyncNtf
            {
                RoleId = roleId,
                NikeName = req.NikeName,
                PosX = req.PosX,
                PosY = req.PosY,
                PosZ = req.PosZ,
                RotationY = req.RotationY
            };
            // 创建新包发送给其他玩家
            BasePackage broadcastPkg = new BasePackage { ProtoCode = NetDefine.CMD_PositionSyncCode, Data = ntf.ToByteString() };
            otherSession.SendData(broadcastPkg);
        });

        // 首次进入场景：通知其他玩家"有新玩家进入"，并告知新玩家已有的其他玩家
        if (isFirstSync)
        {
            // 1. 通知其他玩家有新玩家
            PlayerEnterSceneNtf enterNtf = new PlayerEnterSceneNtf
            {
                RoleId = roleId,
                Nickname = req.NikeName,
                PosX = req.PosX,
                PosY = req.PosY,
                PosZ = req.PosZ
            };
            BasePackage enterPkg = new BasePackage { ProtoCode = NetDefine.CMD_PlayerEnterSceneCode, Data = enterNtf.ToByteString() };
            PlayerSceneMgr.Instance.GetOtherPlayersInScene(roleId).ForEach(other =>
            {
                Session otherSession = SessionMgr.Instance.GetSession(other.SessionId);
                otherSession?.SendData(enterPkg);
            });

            // 2. 告知新玩家已有的其他玩家
            var others = PlayerSceneMgr.Instance.GetOtherPlayersInScene(roleId);
            foreach (var other in others)
            {
                PlayerEnterSceneNtf existNtf = new PlayerEnterSceneNtf
                {
                    RoleId = other.RoleId,
                    Nickname = other.Nickname,
                    PosX = other.PosX,
                    PosY = other.PosY,
                    PosZ = other.PosZ,
                };
                BasePackage existPkg = new BasePackage { ProtoCode = NetDefine.CMD_PlayerEnterSceneCode, Data = existNtf.ToByteString() };
                selfSession.SendData(existPkg);
            }

            LogMsg.Info($"[Login]玩家首次进入场景: roleId={roleId} Nickname={req.NikeName} 同屏人数={others.Count}");
        }
    }

    #region 房间

    // 创建房间
    private void OnCreateRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoomReq req = CreateRoomReq.Parser.ParseFrom(basePackage.Data);
        if (serverBase is Session session)
        {
            CreateRoomRet ret = RoomMgr.Instance.CreateRoom(req.RoleId, session.SessionId, req.Nickname, req.RoomName);
            session.SendData(basePackage, NetDefine.CMD_CreateRoomCode, ret.ToByteString());
            LogMsg.Info($"[Login]创建房间结果: roomId={ret.RoomId}");
        }
    }

    // 加入房间
    private void OnJoinRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        JoinRoomReq req = JoinRoomReq.Parser.ParseFrom(basePackage.Data);
        if (serverBase is Session session)
        {
            JoinRoomRet ret = RoomMgr.Instance.JoinRoom(req.RoleId, session.SessionId, req.Nickname, req.RoomId);
            session.SendData(basePackage, NetDefine.CMD_JoinRoomCode, ret.ToByteString());
            LogMsg.Info($"[Login]加入房间结果: roleId={req.RoleId} roomId={req.RoomId} cmd={ret.CmdCode}");
        }
    }

    // 离开房间
    private void OnLeaveRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LeaveRoomReq req = LeaveRoomReq.Parser.ParseFrom(basePackage.Data);
        RoomMgr.Instance.LeaveRoom(req.RoleId);
        LogMsg.Info($"[Login]离开房间: roleId={req.RoleId}");
    }

    // 玩家切换准备状态：更新后由 RoomMgr 广播 RoomInfoNtf 给房间所有人
    private void OnPlayerReadyHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerReadyReq req = PlayerReadyReq.Parser.ParseFrom(basePackage.Data);
        RoomMgr.Instance.SetPlayerReady(req.RoleId, req.IsReady);
        LogMsg.Info($"[Login]玩家准备状态: roleId={req.RoleId} isReady={req.IsReady}");
    }

    // 房主开始游戏
    private void OnRoomStartGameHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RoomStartGameReq req = RoomStartGameReq.Parser.ParseFrom(basePackage.Data);
        CmdCode code = RoomMgr.Instance.StartGame(req.RoleId);
        if (code != CmdCode.Succeed)
        {
            serverBase.SendError(basePackage, code);
            return;
        }
        LogMsg.Info($"[Login]房主开始游戏: roleId={req.RoleId}");
    }

    #endregion

    // 跳转场景请求
    private void OnChangeSceneHandle(ServerBase serverBase, BasePackage basePackage)
    {
        ChangeSceneReq req = ChangeSceneReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到跳转场景请求:" + req);
    }

    // 保存角色请求
    private void OnSaveRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SaveRoleReq req = SaveRoleReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到保存角色请求:" + req);
    }

    #region 登陆注册

    // 开始游戏请求
    private void OnStartGameHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到开始游戏请求:" + req);
    }

    // 请求登录服务器
    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到登录游戏服务器请求:" + req);
    }

    // 请求创建角色
    private void OnCreateRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到创建角色请求:" + req);
    }

    // 获取服务器列表信息
    private void OnGetServerListHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到获取服务器列表请求:" + req);
    }

    // 处理登录请求
    private void OnLoginHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginReq req = LoginReq.Parser.ParseFrom(basePackage.Data);

        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到登录请求:" + req);
    }

    // 处理注册事件
    private void OnRegistHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RegistReq req = RegistReq.Parser.ParseFrom(basePackage.Data);
        if (!DataUtils.IsValidUserName(req.UserName))
        {
            serverBase.SendError(basePackage, CmdCode.UserNameIlegal);
            return;
        }

        if (req.Password.Length < 4 || req.Password.Length > 16)
        {
            serverBase.SendError(basePackage, CmdCode.PasswordIlegal);
            return;
        }

        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到注册请求:" + req);
    }

    #endregion

    #endregion
}