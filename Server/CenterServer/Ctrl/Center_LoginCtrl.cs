using Google.Protobuf;


/// <summary>
/// 中心服务器处理登录模块的相关逻辑  处理登录数据相关处理  与数据库进行交互 负责数据处理的是我们model
/// </summary>
public class Center_LoginCtrl : IContainer
{
    private LoginModle _loginModle;

    public Center_LoginCtrl(LoginModle loginModle)
    {
        _loginModle = loginModle;
    }

    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
    }

    public void OnInit()
    {
    }

    /// <summary>
    /// 作为服务端接受客户端发过来的数据
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
            /*case NetDefine.CMD_SpawnEnemyCode:
                OnSpawnEnemyHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerAttackCode:
                OnPlayerAttackHandle(serverBase, basePackage);
                break;*/
            case NetDefine.CMD_GetRewardCode:
                OnGetRewardHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_TaskProgressCode:
                OnTaskProgressHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_TaskProgressReqCode:
                OnTaskProgressReqHandle(serverBase, basePackage);
                break;
        }
    }

    private void OnGetRewardHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetRewardReq req = GetRewardReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到获取奖励请求:" + req);
        GetRewardRet ret = _loginModle.GetReward(req);
        LogMsg.Info("[Center]获取奖励处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 任务进度上报（状态迁移 / 进度变化）
    private void OnTaskProgressHandle(ServerBase serverBase, BasePackage basePackage)
    {
        TaskProgressNtf ntf = TaskProgressNtf.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到任务进度上报: roleId=" + ntf.RoleId + " count=" + ntf.ProgressList.Count);
        TaskProgressRet ret = _loginModle.SaveTaskProgress(ntf);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 任务进度拉取（登录 / 进入游戏时恢复状态）
    private void OnTaskProgressReqHandle(ServerBase serverBase, BasePackage basePackage)
    {
        TaskProgressReq req = TaskProgressReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到任务进度拉取: roleId=" + req.RoleId);
        TaskProgressListRet ret = _loginModle.LoadTaskProgress(req);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 开始游戏请求
    private void OnStartGameHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到开始游戏请求:" + req);
        StartGameRet ret = _loginModle.StartGame(req);
        LogMsg.Info("[Center]开始游戏处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 创建角色请求
    private void OnCreateRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到创建角色请求:" + req);
        CreateRoleRet ret = _loginModle.CreateRole(req);
        LogMsg.Info("[Center]创建角色处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 登录游戏服务器请求
    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到登录游戏服务器请求:" + req);
        loginGameServerRet ret = _loginModle.LoginGameServer(req);
        LogMsg.Info("[Center]登录游戏服务器处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 获取服务器列表信息
    private void OnGetServerListHandle(ServerBase serverBase, BasePackage basePackage)
    {
        GetServerListReq req = GetServerListReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到获取服务器列表请求:" + req);
        GetServerListRet ret = _loginModle.GetServerList(req);
        LogMsg.Info("[Center]获取服务器列表处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 处理登录服务器发过来的登录请求
    private void OnLoginHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginReq req = LoginReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到登录请求:" + req);

        loginRet ret = _loginModle.Login(req);
        LogMsg.Info("[Center]登录处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 处理注册事件
    private void OnRegistHandle(ServerBase serverBase, BasePackage basePackage)
    {
        RegistReq req = RegistReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到注册请求:" + req);

        RegistRet ret = _loginModle.RegistAccount(req);
        LogMsg.Info("[Center]注册处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 跳转场景
    private void OnChangeSceneHandle(ServerBase serverBase, BasePackage basePackage)
    {
        ChangeSceneReq req = ChangeSceneReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到跳转场景请求:" + req);
        ChangeSceneRet ret = _loginModle.ChangeScene(req);
        LogMsg.Info("[Center]跳转场景处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 保存角色数据
    private void OnSaveRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SaveRoleReq req = SaveRoleReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到保存角色请求:" + req);
        SaveRoleRet ret = _loginModle.SaveRole(req);
        LogMsg.Info("[Center]保存角色处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    /*// 玩家攻击
    private void OnPlayerAttackHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerAttackReq req = PlayerAttackReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到玩家攻击请求:" + req);
        PlayerAttackRet ret = EnemyMgr.Instance.ProcessAttack(req);
        LogMsg.Info("[Center]攻击处理完成:" + ret);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }

    // 生成敌人
    private void OnSpawnEnemyHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SpawnEnemyReq req = SpawnEnemyReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Center]收到生成敌人请求:" + req);
        int instanceId = EnemyMgr.Instance.SpawnEnemy(req.RoleId, req.EnemyConfigId, req.MaxHp, req.PosX, req.PosY, req.PosZ);
        SpawnEnemyRet ret = new SpawnEnemyRet { EnemyInstanceId = instanceId };
        LogMsg.Info("[Center]生成敌人完成: instanceId=" + instanceId);
        serverBase.SendData(basePackage, basePackage.ProtoCode, ret.ToByteString());
    }*/
}