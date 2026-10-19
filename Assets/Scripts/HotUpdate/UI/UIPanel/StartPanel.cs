using HotUpdate.Controller;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.RedDot;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("StartPanel")]
    public class StartPanel : BasePanel
    {
        [FormerlySerializedAs("_playerNameTxt")] [SerializeField] private TMP_Text playerNameTxt;
        [FormerlySerializedAs("_avatarImage")] [SerializeField] private Image avatarImage;
        [FormerlySerializedAs("_avatarBtn")] [SerializeField] private Button avatarBtn;
        [FormerlySerializedAs("_hostBtn")] [SerializeField] private Button hostBtn;
        [FormerlySerializedAs("_friendBtn")] [SerializeField] private Button friendBtn;
        [FormerlySerializedAs("_depotBtn")] [SerializeField] private Button depotBtn;
        [FormerlySerializedAs("_exitBtn")] [SerializeField] private Button exitBtn;
        [SerializeField] private GameObject friendRedDot;

        private StartPanelController _controller;

        protected override void Awake()
        {
            base.Awake();
            _controller = new StartPanelController();

            avatarBtn.onClick.AddListener(_controller.OpenAvatarPanel);
            hostBtn.onClick.AddListener(_controller.StartGame);
            friendBtn.onClick.AddListener(_controller.OpenFriendPanel);
            depotBtn.onClick.AddListener(_controller.OpenDepotPanel);
            exitBtn.onClick.AddListener(_controller.ExitGame);

            // AddWatcher 会立刻用当前值回调一次，之后数值变化自动跟
            AppContext.RedDot.GetOrCreate(RedDotPath.Friend).AddWatcher(SetFriendRedDot);
        }

        private void OnEnable()
        {
            playerNameTxt.text = "欢迎你：" + AppContext.Session.PlayerName;
            RefreshAvatar();

            // 离线期间收到的私聊 / 申请只落库、推送早就丢了，上线拉一次列表才能把红点点亮
            AppContext.Friend.RequestFriendList();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AppContext.RedDot.GetOrCreate(RedDotPath.Friend).RemoveWatcher(SetFriendRedDot);
        }

        /// <summary>按 Session.Avatar 重画头像（换完头像由 AvatarPanel 回调）</summary>
        public void RefreshAvatar()
        {
            avatarImage.sprite = AvatarConfig.Get(AppContext.Session.Avatar);
        }

        private void SetFriendRedDot(int count) => friendRedDot.SetActive(count > 0);
    }
}