using HotUpdate.Core;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 开始界面 Controller：加载角色数据、切换场景、退出游戏。
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

        /// <summary>打开好友 / 私聊面板（EnterScene 里没有 GameController，由开始面板直接开）</summary>
        public void OpenFriendPanel()
        {
            AppContext.Ui.OpenPanel<FriendPanel>();
        }

        /// <summary>打开背包面板</summary>
        public void OpenDepotPanel()
        {
            AppContext.Ui.OpenPanel<DepotPanel>();
        }

        /// <summary>打开头像选择面板（保存后服务端落库，Session.Avatar 同步更新）</summary>
        public void OpenAvatarPanel()
        {
            AppContext.Ui.OpenPanel<AvatarPanel>();
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
            AppContext.Session.Avatar = info.BaseInfo.Avatar;
            Debug.Log($"角色数据加载成功: {info.BaseInfo.Nickname}");

            // 背包以服务端 role_bag_item 为准：驱动盘（含等级 / 词条 / 穿戴）与材料数量一次落地
            AppContext.Depot.ApplyServerBag(info.DriverDiskMap.Values);
            AppContext.Material.ApplyServerBag(info.MaterialMap.Values);

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