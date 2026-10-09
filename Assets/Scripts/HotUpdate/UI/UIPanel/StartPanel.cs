using System;
using HotUpdate.Controller;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("StartPanel")]
    public class StartPanel : BasePanel
    {
        public TMP_Text playerNameTxt;
        public Button hostButton;
        [Header("退出游戏按钮")] public Button exitBtn;

        /// <summary>本面板的 Controller：由 View 自己 new 并持有，生命周期跟着面板走</summary>
        private StartPanelController _controller;

        protected override void Awake()
        {
            base.Awake();
            _controller = new StartPanelController();
            hostButton.onClick.AddListener(_controller.StartGame);
            exitBtn.onClick.AddListener(_controller.ExitGame);
        }

        private void OnEnable()
        {
            playerNameTxt.text = "欢迎你：" + AppContext.Session.PlayerName;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            hostButton.onClick.RemoveAllListeners();
            exitBtn.onClick.RemoveAllListeners();
        }
    }
}
