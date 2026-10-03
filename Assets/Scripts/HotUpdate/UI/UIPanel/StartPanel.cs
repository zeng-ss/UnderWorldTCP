using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartPanel : BasePanel
{
    public TMP_Text playerNameTxt;
    public Button hostButton;
    [Header("退出游戏按钮")] public Button exitBtn;

    private void OnEnable()
    {
        playerNameTxt.text = "欢迎你：" + GameManager.Instance.curPlayerName;

        // 判断是否已经创建过角色：LoginGameServer 已经查询过
        // RoleTable 中 AccountId + ServerId 匹配的记录
        hostButton.onClick.AddListener(OnStartGame);

        // 没有角色：自动创建（首次进入该服务器时）
        //playerNameTxt.text = "首次进入，点击下方按钮创建角色";
        //hostButton.onClick.AddListener(OnCreateRole);
        exitBtn.onClick.AddListener(ExitGame);
    }

    /// <summary>
    /// 从数据库加载已保存的角色数据
    /// </summary>
    private void OnStartGame()
    {
        Debug.Log($"从数据库加载角色数据 roleId={GameManager.Instance.roleId}");
        ProtoHandler.Instance.RequestStartGame(GameManager.Instance.roleId, OnStartGameResult);
    }

    private void OnStartGameResult(StartGameRet ret)
    {
        if (ret.CmdCode != CmdCode.Succeed)
        {
            UIManager.Instance.OpenPanel<TipPanel>(panel => { panel.ShowTip("获取角色数据失败"); });
            return;
        }

        // 将从数据库拿到的角色数据保存到 GameManager
        GameManager.Instance.mainRoleInfo = ret.MainRoleInfo;
        GameManager.Instance.roleId = ret.MainRoleInfo.BaseInfo.RoleId;
        GameManager.Instance.curPlayerName = ret.MainRoleInfo.BaseInfo.Nickname;
        Debug.Log($"角色数据加载成功: {ret.MainRoleInfo.BaseInfo.Nickname}");

        ProtoHandler.Instance.RequestChangeScene(ret.MainRoleInfo.BaseInfo.RoleId, "LobbyScene", sceneRet =>
        {
            if (sceneRet.CmdCode != CmdCode.Succeed)
            {
                Debug.LogError("跳转场景失败：" + sceneRet.CmdCode);
                return;
            }

            SceneMgr.Instance.LoadScene("LobbyScene");
            UIManager.Instance.ClosePanel<StartPanel>();
        });
    }

    private void ExitGame()
    {
        Application.Quit();
    }

    private void OnDisable()
    {
        hostButton.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
    }
}