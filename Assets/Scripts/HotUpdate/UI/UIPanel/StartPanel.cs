using HotUpdate.Controller;
using HotUpdate.Data;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 开始界面（主菜单）：头像 / 进入游戏 / 私聊 / 背包 / 退出游戏。
    /// 引用全部在预制体里连（Tools/UI/开始面板/5 可一键补绑），改节点名要在 Inspector 重连。
    /// </summary>
    [PanelPath("StartPanel")]
    public class StartPanel : BasePanel
    {
        [SerializeField] private TMP_Text _playerNameTxt;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Button _avatarBtn;
        [SerializeField] private Button _hostBtn;
        [SerializeField] private Button _friendBtn;
        [SerializeField] private Button _depotBtn;
        [SerializeField] private Button _exitBtn;

        private StartPanelController _controller;

        protected override void Awake()
        {
            base.Awake();
            _controller = new StartPanelController();

            _avatarBtn.onClick.AddListener(_controller.OpenAvatarPanel);
            _hostBtn.onClick.AddListener(_controller.StartGame);
            _friendBtn.onClick.AddListener(_controller.OpenFriendPanel);
            _depotBtn.onClick.AddListener(_controller.OpenDepotPanel);
            _exitBtn.onClick.AddListener(_controller.ExitGame);
        }

        private void OnEnable()
        {
            _playerNameTxt.text = "欢迎你：" + AppContext.Session.PlayerName;
            RefreshAvatar();
        }

        /// <summary>按 Session.Avatar 重画头像（换完头像由 AvatarPanel 回调）</summary>
        public void RefreshAvatar()
        {
            _avatarImage.sprite = AvatarConfig.Get(AppContext.Session.Avatar);
        }
    }
}
