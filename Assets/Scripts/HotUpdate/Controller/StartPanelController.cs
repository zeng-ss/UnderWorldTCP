using HotUpdate.Core;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 开始界面 Controller：加载角色数据、切换场景、退出游戏。
    ///
    /// 普通类，由 StartPanel 自己 new 并持有，生命周期跟着面板走。
    /// 它不持有面板引用 —— 面板只调用它的方法，结果一律写回 Service，
    /// 需要弹提示就直接调 UIManager.ShowTip，不反向调用 View。
    /// </summary>
    public class StartPanelController
    {
        private const string LobbySceneName = "LobbyScene";

        public void StartGame()
        {
            int roleId = AppContext.Session.RoleId;
            Debug.Log($"从数据库加载角色数据 roleId={roleId}");
            AppContext.Proto.RequestStartGame(roleId, OnStartGameResult);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        private void OnStartGameResult(StartGameRet ret)
        {
            if (ret.CmdCode != CmdCode.Succeed)
            {
                ShowTip("获取角色数据失败");
                return;
            }

            var info = ret.MainRoleInfo;
            AppContext.Session.MainRoleInfo = info;
            AppContext.Session.RoleId = info.BaseInfo.RoleId;
            AppContext.Session.PlayerName = info.BaseInfo.Nickname;
            Debug.Log($"角色数据加载成功: {info.BaseInfo.Nickname}");

            AppContext.Proto.RequestChangeScene(info.BaseInfo.RoleId, LobbySceneName, sceneRet =>
            {
                if (sceneRet.CmdCode != CmdCode.Succeed)
                {
                    Debug.LogError("跳转场景失败：" + sceneRet.CmdCode);
                    return;
                }

                AppContext.Ui.ClosePanel<StartPanel>();
                AppContext.Scene.LoadScene(LobbySceneName);
            });
        }

        private void ShowTip(string message)
        {
            AppContext.Ui.ShowTip(message);
        }
    }
}
