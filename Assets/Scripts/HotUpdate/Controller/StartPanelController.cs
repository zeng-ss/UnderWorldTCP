using HotUpdate.Core;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Controller
{
    public class StartPanelController
    {
        private const string LobbySceneName = "LobbyScene";

        public void StartGame()
        {
            int roleId = AppContext.Session.RoleId;
            Debug.Log($"从数据库加载角色数据 roleId={roleId}");
            AppContext.Proto.RequestStartGame(roleId, OnStartGameResult);
        }

        public void OpenFriendPanel() => AppContext.Ui.OpenPanel<FriendPanel>();
        public void OpenDepotPanel() => AppContext.Ui.OpenPanel<DepotPanel>();
        public void OpenAvatarPanel() => AppContext.Ui.OpenPanel<AvatarPanel>();
        private void ShowTip(string message) => AppContext.Ui.ShowTip(message);

        /// <summary>
        /// 退出登录：服务端把角色从在线列表移除并把场景重置回主界面，
        /// 全程盖一层转场遮罩，登录面板加载完才揭开。
        /// </summary>
        public void BackToLogin()
        {
            int roleId = AppContext.Session.RoleId;
            Debug.Log($"退出登录 roleId = {roleId}");

            AppContext.Ui.Transition(0.6f, TransitionPanel.ScrollBgType.Normal, () =>
            {
                AppContext.Proto.RequestBackToLogin(roleId, ret =>
                {
                    if (ret.CmdCode != CmdCode.Succeed)
                    {
                        ShowTip("退出登录失败");
                        AppContext.Ui.TransitionDone();
                        return;
                    }

                    AppContext.Session.Clear();
                    AppContext.Friend.Clear();
                    AppContext.Ui.OpenPanel<LoginPanel>(_ =>
                    {
                        AppContext.Ui.ClosePanel<StartPanel>();
                        AppContext.Ui.TransitionDone();
                    });
                });
            });
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

            AppContext.Proto.RequestChangeScene(LobbySceneName, sceneRet =>
            {
                if (sceneRet.CmdCode != CmdCode.Succeed)
                {
                    Debug.LogError("跳转场景失败：" + sceneRet.CmdCode);
                    return;
                }

                AppContext.Scene.LoadScene(LobbySceneName, 0.5f, TransitionPanel.ScrollBgType.Normal, OnCoveredHandle);
            });
        }

        private void OnCoveredHandle()
        {
            AppContext.Ui.ClosePanel<StartPanel>();
            AppContext.Ui.OpenPanel<RoomPanel>();
        }
    }
}