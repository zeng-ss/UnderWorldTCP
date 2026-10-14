using HotUpdate.Controller;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 开始界面（主菜单）。四个入口：进入游戏 / 私聊 / 背包 / 退出游戏。
    ///
    /// 按钮不做序列化字段绑定，而是按节点名在运行时找 —— 预制体用
    /// Tools/UI/开始面板 重新生成后节点名不变，绑定时少一个按钮只告警不报错。
    /// </summary>
    [PanelPath("StartPanel")]
    public class StartPanel : BasePanel
    {
        private TMP_Text _playerNameTxt;
        private StartPanelController _controller;

        protected override void Awake()
        {
            base.Awake();
            _controller = new StartPanelController();
            BindHierarchy();
        }

        private void OnEnable()
        {
            if (_playerNameTxt != null)
                _playerNameTxt.text = "欢迎你：" + AppContext.Session.PlayerName;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveAllListeners();
        }

        #region 绑定

        // 按生成脚本的节点命名绑定；节点缺失时告警但不抛异常（面板来自旧版预制体也能跑）
        private void BindHierarchy()
        {
            _playerNameTxt = FindDeep(transform, "playerName")?.GetComponent<TMP_Text>();

            BindClick("HostBtn", _controller.StartGame);
            BindClick("FriendBtn", _controller.OpenFriendPanel);
            BindClick("DepotBtn", _controller.OpenDepotPanel);
            // 旧版预制体里退出按钮叫 exitBtn，两个名字都认
            BindClick("ExitBtn", _controller.ExitGame, "exitBtn");

            Debug.Log("[StartPanel] 已绑定：进入游戏 / 私聊 / 背包 / 退出游戏");
        }

        private void BindClick(string nodeName, UnityAction action, string fallbackNodeName = null)
        {
            Transform node = FindDeep(transform, nodeName);
            if (node == null && !string.IsNullOrEmpty(fallbackNodeName))
                node = FindDeep(transform, fallbackNodeName);

            var btn = node != null ? node.GetComponent<Button>() : null;
            if (btn == null)
            {
                Debug.LogWarning($"[StartPanel] 预制体里没找到 {nodeName}（或它身上没有 Button），" +
                                 "该入口不可用；请用 Tools/UI/开始面板/3 重新生成预制体。");
                return;
            }

            btn.onClick.AddListener(action);
        }

        private void RemoveAllListeners()
        {
            foreach (string nodeName in new[] { "HostBtn", "FriendBtn", "DepotBtn", "ExitBtn", "exitBtn" })
            {
                var btn = FindDeep(transform, nodeName)?.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
        }

        private static Transform FindDeep(Transform root, string nodeName)
        {
            if (root == null) return null;
            if (root.name == nodeName) return root;

            int count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform found = FindDeep(root.GetChild(i), nodeName);
                if (found != null) return found;
            }

            return null;
        }

        #endregion
    }
}
