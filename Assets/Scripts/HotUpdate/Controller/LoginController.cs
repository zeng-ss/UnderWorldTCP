using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 登录流程 Controller。
/// </summary>
public class LoginController
{
    private const int DefaultJobId = 1;

    private readonly List<GameServer> _servers = new();
    private GameServer _selectedServer;
    private string _pendingAccount;

    #region View → Controller

    /// <summary>面板每次显示时刷新一次服务器列表</summary>
    public void OnPanelShown()
    {
        RefreshServerList();
    }

    public void SelectServer(int index)
    {
        _selectedServer = index >= 0 && index < _servers.Count ? _servers[index] : null;
        BroadcastServerList();
    }

    public void Login(string account, string password)
    {
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            ShowTip("账号或密码不能为空");
            return;
        }

        if (_selectedServer == null)
        {
            ShowTip("请选择服务器");
            return;
        }

        _pendingAccount = account;
        ProtoHandler.Instance.RequestLogin(account, password, OnLoginResult);
    }

    public void RequestRegister()
    {
        UIManager.Instance.OpenPanel<RegisterPanel>();
        UIManager.Instance.ClosePanel<LoginPanel>();
    }

    #endregion

    #region 服务器列表

    private void RefreshServerList()
    {
        ProtoHandler.Instance.RequestServerList(OnServerListResult);
    }

    private void OnServerListResult(GetServerListRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            ShowTip("获取服务器列表失败");
            return;
        }

        _servers.Clear();
        foreach (var server in ret.GameServers) _servers.Add(server);

        if (_servers.Count > 0) SelectServer(0);
        else BroadcastServerList();
    }

    /// <summary>拼好界面需要的一切，一次性广播出去；面板在不在都无所谓</summary>
    private void BroadcastServerList()
    {
        var names = new List<string>(_servers.Count);
        foreach (var server in _servers) names.Add(server.ServerName);

        int index = _selectedServer != null ? _servers.IndexOf(_selectedServer) : 0;
        bool busy = _selectedServer != null && _selectedServer.RunState == 1;

        AppContext.Events.EventTrigger(GameEvent.ServerListChanged,
            new ServerListArgs(names, index, GetRunStateText(_selectedServer?.RunState ?? 0),
                busy ? Color.red : Color.green));
    }

    private string GetRunStateText(int runState)
    {
        return runState switch { 1 => "爆满", 2 => "拥挤", 3 => "正常", _ => "未知" };
    }

    #endregion

    #region 登录与建角色

    private void OnLoginResult(loginRet ret)
    {
        switch (ret.CmdCode)
        {
            case CmdCode.Succeed:
                AppContext.Session.PlayerName = _pendingAccount;
                AppContext.Session.AccountId = ret.AccountId;
                AppContext.Session.ServerId = _selectedServer.ServerId;
                ProtoHandler.Instance.RequestLoginGameServer(ret.AccountId, _selectedServer.ServerId,
                    OnLoginGameServerResult);
                break;
            case CmdCode.AcctNotExist:
                ShowTip("账号不存在");
                break;
            case CmdCode.PasswordError:
                ShowTip("密码错误");
                break;
            case CmdCode.AcctDisable:
                ShowTip("账号已被禁用");
                break;
            default:
                ShowTip("登录失败，服务器错误");
                break;
        }
    }

    private void OnLoginGameServerResult(loginGameServerRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            ShowTip("登录游戏服务器失败");
            return;
        }

        bool hasRole = ret.CreateRoleInfo != null && ret.CreateRoleInfo.RoleId > 0;
        if (hasRole) AppContext.Session.RoleId = ret.CreateRoleInfo.RoleId;
        else CreateRole();

        UIManager.Instance.ClosePanel<LoginPanel>();
        ShowTip("登录成功");
        UIManager.Instance.OpenPanel<StartPanel>();
    }

    private void CreateRole()
    {
        string nickname = AppContext.Session.PlayerName;
        Debug.Log($"创建新角色: {nickname} jobId={DefaultJobId}");
        ProtoHandler.Instance.RequestCreateRole(
            AppContext.Session.AccountId,
            AppContext.Session.ServerId,
            nickname,
            DefaultJobId,
            OnCreateRoleResult);
    }

    private void OnCreateRoleResult(CreateRoleRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            ShowTip(ret.CmdCode == CmdCode.NicknameExist ? "昵称已存在" : "创建角色失败");
            return;
        }

        AppContext.Session.RoleId = ret.RoleId;
        Debug.Log($"角色创建成功 roleId={ret.RoleId}");
    }

    #endregion

    private void ShowTip(string message) => UIManager.Instance.ShowTip(message);
}