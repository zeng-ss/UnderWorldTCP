using System;

public class Game_LoginCtrl : IContainer
{
    public void OnInit()
    {
    }

    #region 游戏服务器作为服务端

    /// <summary>
    /// 游戏服务器作为服务端接收到网关服务器发来的数据
    /// </summary>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
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
            default:
                LogMsg.Info("[Game_LoginCtrl]有没有注册的请求码", ConsoleColor.Red);
                break;
        }
    }

    // 跳转场景请求
    private void OnChangeSceneHandle(ServerBase serverBase, BasePackage basePackage)
    {
        ChangeSceneReq req = ChangeSceneReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Game]收到跳转场景请求:" + req);
    }

    // 保存角色数据请求
    private void OnSaveRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        SaveRoleReq req = SaveRoleReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Game]收到保存角色请求:" + req);
    }

    // 开始游戏请求
    private void OnStartGameHandle(ServerBase serverBase, BasePackage basePackage)
    {
        StartGameReq req = StartGameReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Game]收到开始游戏请求:" + req);
    }

    // 请求登录游戏服务器
    private void OnLoginGameServerHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LoginGameServerReq req = LoginGameServerReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Game]收到登录游戏服务器请求:" + req);
    }

    // 请求创建角色
    private void OnCreateRoleHandle(ServerBase serverBase, BasePackage basePackage)
    {
        CreateRoleReq req = CreateRoleReq.Parser.ParseFrom(basePackage.Data);
        serverBase._Client.SendData(basePackage);
        LogMsg.Info("[Game]收到创建角色请求:" + req);
    }

    #endregion

    #region 游戏服务器作为客户端

    /// <summary>
    /// 游戏服务器作为客户端 接收到中心服务器发来的数据
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
        Session session = SessionMgr.Instance.GetSession(basePackage.GateSessionId);
        switch (basePackage.ProtoCode)
        {
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
        }
    }

    // 跳转场景返回
    private void OnChangeSceneResultHandle(Session session, BasePackage basePackage)
    {
        ChangeSceneRet ret = ChangeSceneRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Game]跳转场景结果:" + ret);
        session.SendData(basePackage);
    }

    // 保存角色数据返回
    private void OnSaveRoleResultHandle(Session session, BasePackage basePackage)
    {
        SaveRoleRet ret = SaveRoleRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Game]保存角色结果:" + ret);
        session.SendData(basePackage);
    }

    // 处理开始游戏返回数据
    private void OnStartGameResultHandle(Session session, BasePackage basePackage)
    {
        StartGameRet ret = StartGameRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Game]开始游戏结果:" + ret);
        session.SendData(basePackage);
    }

    // 创建角色返回数据
    private void OnCreateRoleResultHandle(Session session, BasePackage basePackage)
    {
        CreateRoleRet ret = CreateRoleRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Game]创建角色结果:" + ret);
        session.SendData(basePackage);
    }

    // 登录游戏服务器返回数据
    private void OnLoginGameServerResultHandle(Session session, BasePackage basePackage)
    {
        loginGameServerRet ret = loginGameServerRet.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("[Game]登录游戏服务器结果:" + ret);
        session.SendData(basePackage);
    }

    #endregion
}