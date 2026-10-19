using System;
using System.Collections.Generic;
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
            case NetDefine.CMD_TaskProgressCode:
                OnTaskProgressResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_TaskProgressReqCode:
                OnTaskProgressReqResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_DriverDiskUpgradeCode:
                OnDriverDiskUpgradeResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_DriverDiskEquipCode:
                OnDriverDiskEquipResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_BagInfoCode:
                OnBagInfoResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_FriendListCode:
                OnFriendListResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_AddFriendCode:
                OnAddFriendResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_HandleFriendCode:
                OnHandleFriendResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_SendFriendMsgCode:
                OnSendFriendMsgResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_FriendHistoryCode:
                OnFriendHistoryResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_SetAvatarCode:
                OnSetAvatarResultHandle(session, basePackage);
                break;
            case NetDefine.CMD_FriendMsgReadCode:
                OnFriendMsgReadResultHandle(session, basePackage);
                break;
            default:
                LogMsg.Info("[LoginCtrl]中心服务器发过来的结果的请求码没有注册");
                break;
        }
    }

    // 驱动盘升级返回（CenterServer 已落库）。失败也原样透传，客户端据此拿 Tip 弹提示，
    // 不走 SendError —— SendError 只发 CMD_ErrCode，回调永远等不到响应会悬挂。
    private void OnDriverDiskUpgradeResultHandle(Session session, BasePackage basePackage)
    {
        DriverDiskUpgradeRet ret = DriverDiskUpgradeRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Login]驱动盘升级结果:" + ret);
        session.SendData(basePackage);
    }

    // 装备 / 卸下返回（同上，失败原样透传）
    private void OnDriverDiskEquipResultHandle(Session session, BasePackage basePackage)
    {
        DriverDiskEquipRet ret = DriverDiskEquipRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Login]驱动盘装备结果:" + ret);
        session.SendData(basePackage);
    }

    // 背包拉取返回
    private void OnBagInfoResultHandle(Session session, BasePackage basePackage)
    {
        BagInfoRet ret = BagInfoRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }

        LogMsg.Info("[Login]背包拉取结果: 驱动盘=" + ret.DriverDiskMap.Count + " 材料=" + ret.MaterialMap.Count);
        session.SendData(basePackage);
    }

    // 好友列表返回：补充每个好友的在线状态后透传
    private void OnFriendListResultHandle(Session session, BasePackage basePackage)
    {
        FriendListRet ret = FriendListRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }

        // 补充在线状态：LoginServer 的 PlayerSceneMgr 维护着「进入过场景」的玩家，
        // 用它来近似在线判断（进入过场景且未离开即视为在线）。
        foreach (FriendInfo info in ret.Friends)
        {
            info.IsOnline = GetSessionIdByRole(info.RoleId) > 0;
        }

        // 待处理申请里的角色同样补一次，保持两个列表的字段口径一致（UI 目前不展示）
        foreach (FriendInfo info in ret.Applies)
        {
            info.IsOnline = GetSessionIdByRole(info.RoleId) > 0;
        }

        LogMsg.Info("[Login]好友列表结果: 好友数=" + ret.Friends.Count + " 待处理申请=" + ret.Applies.Count);
        session.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 添加好友返回：成功则实时通知被申请者（若在线）
    private void OnAddFriendResultHandle(Session session, BasePackage basePackage)
    {
        AddFriendRet ret = AddFriendRet.Parser.ParseFrom(basePackage.Data);
        // 失败也原样透传，客户端拿 Tip 弹提示（不用 SendError，避免回调悬挂）
        session.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());

        if (ret.CmdCode != CmdCode.Succeed) return;

        int targetSessionId = GetSessionIdByRole(ret.ToRoleId);
        if (targetSessionId <= 0) return;

        FriendApplyNtf ntf = new FriendApplyNtf
        {
            FromRoleId = ret.FromRoleId,
            FromNickname = ret.FromNickname
        };
        BasePackage push = new BasePackage
            { ProtoCode = NetDefine.CMD_FriendApplyNtfCode, Data = ntf.ToByteString() };
        SessionMgr.Instance.GetSession(targetSessionId)?.SendData(push);
        LogMsg.Info("[Login]好友申请实时通知: 申请者=" + ret.FromNickname + " -> roleId=" + ret.ToRoleId);
    }

    // 处理好友申请返回：透传
    private void OnHandleFriendResultHandle(Session session, BasePackage basePackage)
    {
        HandleFriendRet ret = HandleFriendRet.Parser.ParseFrom(basePackage.Data);
        session.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
        LogMsg.Info("[Login]处理好友申请结果:" + ret.CmdCode);
    }

    // 发送私聊返回：透传给发送者，并把消息实时推送给接收者（若在线）
    private void OnSendFriendMsgResultHandle(Session session, BasePackage basePackage)
    {
        SendFriendMsgRet ret = SendFriendMsgRet.Parser.ParseFrom(basePackage.Data);
        // 原样透传（含完整消息 id / 时间），客户端据此回填本地
        session.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());

        if (ret.CmdCode != CmdCode.Succeed || ret.Message == null) return;

        int receiverId = ret.Message.ReceiverRoleId;
        int targetSessionId = GetSessionIdByRole(receiverId);
        if (targetSessionId <= 0) return; // 接收者离线，等下次拉历史

        FriendMsgPushNtf ntf = new FriendMsgPushNtf { Message = ret.Message };
        BasePackage push = new BasePackage
            { ProtoCode = NetDefine.CMD_FriendMsgPushCode, Data = ntf.ToByteString() };
        SessionMgr.Instance.GetSession(targetSessionId)?.SendData(push);
        LogMsg.Info("[Login]私聊消息实时推送: " + ret.Message.SenderRoleId + " -> " + receiverId);
    }

    // 聊天记录返回：透传
    private void OnFriendHistoryResultHandle(Session session, BasePackage basePackage)
    {
        FriendHistoryRet ret = FriendHistoryRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }

        LogMsg.Info("[Login]聊天记录结果: 条数=" + ret.Messages.Count);
        session.SendData(basePackage);
    }

    // 设置头像返回：透传（失败也透传，客户端据此拿 CmdCode 提示，避免回调悬挂）
    private void OnSetAvatarResultHandle(Session session, BasePackage basePackage)
    {
        SetAvatarRet ret = SetAvatarRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Login]设置头像结果:" + ret.CmdCode + " avatar=" + ret.Avatar);
        session.SendData(basePackage);
    }

    // 标记私聊已读结果（无 UI，客户端可以忽略）
    private void OnFriendMsgReadResultHandle(Session session, BasePackage basePackage)
    {
        MarkFriendMsgReadRet ret = MarkFriendMsgReadRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Login]标记私聊已读结果:" + ret.CmdCode);
        session.SendData(basePackage);
    }

    // 通过 roleId 找在线 sessionId（进入过场景即视为在线）
    private int GetSessionIdByRole(int roleId)
    {
        // PlayerSceneMgr 维护 roleId → sessionId（进入场景时写入）
        Dictionary<int, PlayerSceneInfo> players = PlayerSceneMgr.Instance.GetAllPlayers();
        if (players.TryGetValue(roleId, out PlayerSceneInfo info)) return info.SessionId;
        return 0;
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

    // 任务进度上报返回（CenterServer 处理完回包，透传给真正客户端）
    private void OnTaskProgressResultHandle(Session session, BasePackage basePackage)
    {
        TaskProgressRet ret = TaskProgressRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }

        LogMsg.Info("[Login]任务进度上报结果:" + ret);
        session.SendData(basePackage);
    }

    // 任务进度拉取返回（CenterServer 处理完回包，透传给真正客户端）
    private void OnTaskProgressReqResultHandle(Session session, BasePackage basePackage)
    {
        TaskProgressListRet ret = TaskProgressListRet.Parser.ParseFrom(basePackage.Data);
        if (ret.CmdCode != CmdCode.Succeed)
        {
            session.SendError(basePackage, ret.CmdCode);
            return;
        }

        LogMsg.Info("[Login]任务拉取结果: 条数=" + ret.TaskList.Count);
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
        LogMsg.Info(
            $"[Login]攻击处理: roleId={req.RoleId} enemyId={req.EnemyInstanceId} damage={ret.DamageDealt} isDead={ret.IsDead}");

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
            BasePackage pkg = new BasePackage
                { ProtoCode = NetDefine.CMD_PlayerLeaveSceneCode, Data = ntf.ToByteString() };
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
            case NetDefine.CMD_TaskProgressCode:
                OnTaskProgressHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_TaskProgressReqCode:
                OnTaskProgressReqHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_DriverDiskUpgradeCode:
                OnDriverDiskUpgradeHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_DriverDiskEquipCode:
                OnDriverDiskEquipHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_BagInfoCode:
                OnBagInfoHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_FriendListCode:
                OnFriendListHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_AddFriendCode:
                OnAddFriendHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_HandleFriendCode:
                OnHandleFriendHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SendFriendMsgCode:
                OnSendFriendMsgHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_FriendHistoryCode:
                OnFriendHistoryHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SetAvatarCode:
                OnSetAvatarHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_FriendMsgReadCode:
                OnFriendMsgReadHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerStatsCode:
                OnPlayerStatsHandle(serverBase, basePackage);
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
            case NetDefine.CMD_RoomListCode:
                OnRoomListHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_RoomChatCode:
                OnRoomChatHandle(serverBase, basePackage);
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
            case NetDefine.CMD_KickPlayerCode:
                OnKickPlayerHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_SyneAniCode:
                OnSyncAniHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerVfxCode:
                OnPlayerVfxHandle(serverBase, basePackage);
                break;
            default:
                LogMsg.Info("[LoginCtrl]有没有注册的请求码", ConsoleColor.Red);
                break;
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
        LogMsg.Info(
            $"[Login]VFX同步: roleId={ntf.RoleId} config={ntf.SkillConfigIndex} atk={ntf.AttackIndex} vfx={ntf.VfxIndex}");
    }

    // 敌人生成：服务端分配实例ID并把出生信息广播给场景内所有人。
    // 生成后敌人的位置与动作全部由 EnemyMgr 的 tick 推进，客户端不再插手。
    private void OnSpawnEnemyHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SpawnEnemyReq req = SpawnEnemyReq.Parser.ParseFrom(basePackage.Data);

        float maxHp = req.MaxHp > 0 ? req.MaxHp : 10000;
        EnemyMgr.Instance.SpawnEnemy(req.EnemyConfigId, maxHp, req.PosX, req.PosY, req.PosZ);
        LogMsg.Info("[Login]收到敌人生成请求:" + req);
    }

    private void OnGetRewardHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetRewardReq req = GetRewardReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到获得奖励请求:" + req);
    }

    // 任务进度上报转发（LoginServer 透传给 CenterServer 处理）
    private void OnTaskProgressHandle(ServerBase serverBase, BasePackage basePackage)
    {
        TaskProgressNtf ntf = TaskProgressNtf.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到任务进度上报转发:" + ntf);
    }

    // 任务进度拉取转发
    private void OnTaskProgressReqHandle(ServerBase serverBase, BasePackage basePackage)
    {
        TaskProgressReq req = TaskProgressReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到任务进度拉取转发:" + req);
    }

    // 驱动盘升级转发（LoginServer 只透传给 CenterServer，校验与落库都在 Center）
    private void OnDriverDiskUpgradeHandle(ServerBase serverBase, BasePackage basePackage)
    {
        DriverDiskUpgradeReq req = DriverDiskUpgradeReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到驱动盘升级转发:" + req);
    }

    // 装备 / 卸下转发
    private void OnDriverDiskEquipHandle(ServerBase serverBase, BasePackage basePackage)
    {
        DriverDiskEquipReq req = DriverDiskEquipReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到驱动盘装备转发:" + req);
    }

    // 背包拉取转发
    private void OnBagInfoHandle(ServerBase serverBase, BasePackage basePackage)
    {
        BagInfoReq req = BagInfoReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到背包拉取转发:" + req);
    }

    // 好友列表拉取转发（透传 CenterServer）
    private void OnFriendListHandle(ServerBase serverBase, BasePackage basePackage)
    {
        FriendListReq req = FriendListReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到好友列表转发: roleId=" + req.RoleId);
    }

    // 添加好友转发
    private void OnAddFriendHandle(ServerBase serverBase, BasePackage basePackage)
    {
        AddFriendReq req = AddFriendReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到添加好友转发: " + req.FromRoleId + " -> " + req.ToRoleId);
    }

    // 处理好友申请转发
    private void OnHandleFriendHandle(ServerBase serverBase, BasePackage basePackage)
    {
        HandleFriendReq req = HandleFriendReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到处理好友申请转发: roleId=" + req.RoleId);
    }

    // 发送私聊消息转发
    private void OnSendFriendMsgHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SendFriendMsgReq req = SendFriendMsgReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到私聊消息转发: " + req.SenderRoleId + " -> " + req.ReceiverRoleId);
    }

    // 聊天记录拉取转发
    private void OnFriendHistoryHandle(ServerBase serverBase, BasePackage basePackage)
    {
        FriendHistoryReq req = FriendHistoryReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到聊天记录转发: roleId=" + req.RoleId);
    }

    // 玩家战斗属性上报（血量上限 / 防御）。属性算在客户端（基础值 + 已装备驱动盘加成），
    // 但伤害结算在服务端，所以服务端要留一份。此处直接消费，不转发给 CenterServer。
    private void OnPlayerStatsHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerStatsNtf ntf = PlayerStatsNtf.Parser.ParseFrom(basePackage.Data);
        PlayerSceneMgr.Instance.SetPlayerStats(ntf.RoleId, ntf.MaxHp, ntf.Defense);
        LogMsg.Info($"[Login]玩家属性上报: roleId={ntf.RoleId} maxHp={ntf.MaxHp} defense={ntf.Defense}");
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
            BasePackage broadcastPkg = new BasePackage
                { ProtoCode = NetDefine.CMD_PositionSyncCode, Data = ntf.ToByteString() };
            otherSession.SendData(broadcastPkg);
        });

        // 首次进入场景：通知其他玩家"有新玩家进入"，并告知新玩家已有的其他玩家
        if (isFirstSync)
        {
            // 重新进入场景 = 复活（客户端没有独立的复活流程，重登 / 重连即回满血）
            PlayerSceneMgr.Instance.ReviveOnEnter(roleId);

            // 1. 通知其他玩家有新玩家
            PlayerEnterSceneNtf enterNtf = new PlayerEnterSceneNtf
            {
                RoleId = roleId,
                Nickname = req.NikeName,
                PosX = req.PosX,
                PosY = req.PosY,
                PosZ = req.PosZ
            };
            BasePackage enterPkg = new BasePackage
                { ProtoCode = NetDefine.CMD_PlayerEnterSceneCode, Data = enterNtf.ToByteString() };
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
                BasePackage existPkg = new BasePackage
                    { ProtoCode = NetDefine.CMD_PlayerEnterSceneCode, Data = existNtf.ToByteString() };
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
            CreateRoomRet ret = RoomMgr.Instance.CreateRoom(req.RoleId, session.SessionId, req.Nickname,
                req.Avatar, req.RoomName);
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
            JoinRoomRet ret = RoomMgr.Instance.JoinRoom(req.RoleId, session.SessionId, req.Nickname,
                req.Avatar, req.RoomId);
            session.SendData(basePackage, NetDefine.CMD_JoinRoomCode, ret.ToByteString());
            LogMsg.Info($"[Login]加入房间结果: roleId={req.RoleId} roomId={req.RoomId} cmd={ret.CmdCode}");
        }
    }

    // 拉取房间列表（房间只存在于 LoginServer 内存，直接本地返回，不经过 CenterServer）
    private void OnRoomListHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RoomListReq req = RoomListReq.Parser.ParseFrom(basePackage.Data);
        if (!(serverBase is Session session)) return;

        RoomListRet ret = new RoomListRet { CmdCode = CmdCode.Succeed };
        ret.Rooms.AddRange(RoomMgr.Instance.GetRoomList());

        session.SendData(basePackage, NetDefine.CMD_RoomListCode, ret.ToByteString());
        LogMsg.Info($"[Login]房间列表结果: roleId={req.RoleId} 房间数={ret.Rooms.Count}");
    }

    // 房间内聊天：服务端补昵称与时间后广播给房间内所有人（含发送者）
    private void OnRoomChatHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RoomChatReq req = RoomChatReq.Parser.ParseFrom(basePackage.Data);
        bool ok = RoomMgr.Instance.BroadcastChat(req.RoleId, req.Content);
        LogMsg.Info($"[Login]房间聊天: roleId={req.RoleId} ok={ok}");
    }

    // 离开房间：先离开再回包，客户端据此关闭房间界面
    private void OnLeaveRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LeaveRoomReq req = LeaveRoomReq.Parser.ParseFrom(basePackage.Data);
        if (!(serverBase is Session session)) return;

        bool ok = RoomMgr.Instance.LeaveRoom(req.RoleId);
        LeaveRoomRet ret = new LeaveRoomRet
        {
            CmdCode = ok ? CmdCode.Succeed : CmdCode.ReqParamError,
            Tip = ok ? "" : "你当前不在任何房间"
        };
        session.SendData(basePackage, NetDefine.CMD_LeaveRoomCode, ret.ToByteString());
        LogMsg.Info($"[Login]离开房间: roleId={req.RoleId} ok={ok}");
    }

    // 房主踢人：RoomMgr 校验后通知被踢者退回列表页，并给房主回包
    private void OnKickPlayerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        KickPlayerReq req = KickPlayerReq.Parser.ParseFrom(basePackage.Data);
        if (!(serverBase is Session session)) return;

        string tip;
        CmdCode code = RoomMgr.Instance.KickPlayer(req.RoleId, req.TargetRoleId, out tip);
        KickPlayerRet ret = new KickPlayerRet { CmdCode = code, Tip = tip };
        session.SendData(basePackage, NetDefine.CMD_KickPlayerCode, ret.ToByteString());
        LogMsg.Info($"[Login]踢人: master={req.RoleId} target={req.TargetRoleId} cmd={code}");
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

    // 设置头像请求（落库在中心服，这里只转发）
    private void OnSetAvatarHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SetAvatarReq req = SetAvatarReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到设置头像请求: roleId=" + req.RoleId + " avatar=" + req.Avatar);
    }

    // 标记私聊已读转发（聊天窗口开着时收到新消息，客户端补一条回执）
    private void OnFriendMsgReadHandle(ServerBase serverBase, BasePackage basePackage)
    {
        MarkFriendMsgReadReq req = MarkFriendMsgReadReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到标记私聊已读转发: roleId=" + req.RoleId + " friend=" + req.FriendRoleId);
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
        /*
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
        */

        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Login]收到注册请求:" + req);
    }

    #endregion

    #endregion
}