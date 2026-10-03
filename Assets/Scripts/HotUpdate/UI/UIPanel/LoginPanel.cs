using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : BasePanel
{
    public Text StateText;
    public Dropdown dropdown;
    private List<Dropdown.OptionData> optionDatas = new();
    private GameServer currentServerData;
    private Dictionary<string, GameServer> serverDataDict = new();
    public Button loginBtn;
    public Button registerBtn;
    public Text account;
    public Text password;

    private void OnEnable()
    {
        ProtoHandler.Instance.RequestServerList(OnServerListResult);
        dropdown.onValueChanged.AddListener(value =>
        {
            if (optionDatas.Count > value && serverDataDict.TryGetValue(optionDatas[value].text, out var server))
            {
                currentServerData = server;
                StateText.text = GetRunStateText(server.RunState);
                StateText.color = server.RunState == 1 ? Color.red : Color.green;
            }
        });
    }

    private void OnServerListResult(GetServerListRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("获取服务器列表失败"); });
            return;
        }

        optionDatas.Clear();
        serverDataDict.Clear();

        foreach (var server in ret.GameServers)
        {
            Dropdown.OptionData optionData = new Dropdown.OptionData(server.ServerName);
            optionDatas.Add(optionData);
            serverDataDict[server.ServerName] = server;
        }

        dropdown.options = optionDatas;
        if (optionDatas.Count > 0)
        {
            currentServerData = serverDataDict[optionDatas[0].text];
            StateText.text = GetRunStateText(currentServerData.RunState);
            StateText.color = currentServerData.RunState == 1 ? Color.red : Color.green;
        }
    }

    private string GetRunStateText(int runState)
    {
        return runState switch { 1 => "爆满", 2 => "拥挤", 3 => "正常", _ => "未知" };
    }

    private void Start()
    {
        loginBtn.onClick.AddListener(LoginCheck);
        registerBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenPanel<RegisterPanel>();
            UIManager.Instance.ClosePanel<LoginPanel>();
        });
    }

    private void LoginCheck()
    {
        string inputAccount = account.text.Trim();
        string inputPwd = password.text.Trim();

        if (string.IsNullOrEmpty(inputAccount) || string.IsNullOrEmpty(inputPwd))
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("账号或密码不能为空"); });
            return;
        }

        if (currentServerData == null)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("请选择服务器"); });
            return;
        }

        ProtoHandler.Instance.RequestLogin(inputAccount, inputPwd, OnLoginResult);
    }

    private void OnLoginResult(loginRet ret)
    {
        switch (ret.CmdCode)
        {
            case CmdCode.Succeed:
                GameManager.Instance.curPlayerName = account.text.Trim();
                GameManager.Instance.accountId = ret.AccountId;
                GameManager.Instance.serverId = currentServerData.ServerId;
                // 登录成功后，查询该账号在此服务器是否有已创建的角色
                ProtoHandler.Instance.RequestLoginGameServer(ret.AccountId, currentServerData.ServerId, OnLoginGameServerResult);
                break;
            case CmdCode.AcctNotExist:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("账号不存在"); });
                break;
            case CmdCode.PasswordError:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("密码错误"); });
                break;
            case CmdCode.AcctDisable:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("账号已被禁用"); });
                break;
            default:
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("登录失败，服务器错误"); });
                break;
        }
    }

    private void OnLoginGameServerResult(loginGameServerRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("登录游戏服务器失败"); });
            return;
        }

        // 保存角色信息（如果已创建过角色）
        GameManager.Instance.roleId = ret.CreateRoleInfo?.RoleId ?? 0;
        if (ret.CreateRoleInfo?.RoleId > 0)
        {
            GameManager.Instance.roleId = ret.CreateRoleInfo.RoleId;
        }
        else
        {
            OnCreateRole();
        }

        UIManager.Instance.ClosePanel<LoginPanel>();
        UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("登录成功"); });
        UIManager.Instance.OpenPanel<StartPanel>();
    }
    
    /// <summary>
    /// 创建新角色（首次进入该服务器时）
    /// </summary>
    private void OnCreateRole()
    {
        string defaultNickname = GameManager.Instance.curPlayerName;
        int defaultJob = 1; // 默认职业

        Debug.Log($"创建新角色: {defaultNickname} jobId={defaultJob}");
        ProtoHandler.Instance.RequestCreateRole(
            GameManager.Instance.accountId,
            GameManager.Instance.serverId,
            defaultNickname,
            defaultJob,
            OnCreateRoleResult
        );
    }

    private void OnCreateRoleResult(CreateRoleRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            if (ret.CmdCode == CmdCode.NicknameExist)
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("昵称已存在"); });
            else
                UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("创建角色失败"); });
            return;
        }

        // 创建成功，拿到 roleId，再调用 StartGame 加载完整数据
        GameManager.Instance.roleId = ret.RoleId;
        Debug.Log($"角色创建成功 roleId={ret.RoleId}, 开始加载角色数据");
    }

    private void OnDisable()
    {
        dropdown.onValueChanged.RemoveAllListeners();
    }
}
