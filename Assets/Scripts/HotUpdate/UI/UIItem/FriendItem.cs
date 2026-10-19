using System;
using HotUpdate.Data;
using HotUpdate.Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIItem
{
    public class FriendItem : MonoBehaviour
    {
        [SerializeField] private Image headImage;
        [SerializeField] private TMP_Text nameText, statusText;
        [SerializeField] private GameObject selectedMark, redDot;

        // 申请形态专属：按钮容器与两个按钮（好友形态下整体隐藏）
        [SerializeField] private GameObject applyButtons;
        [SerializeField] private Button agreeBtn, rejectBtn;

        private static readonly Color ColOnline = new Color(0.3f, 0.9f, 0.4f, 1f);
        private static readonly Color ColOffline = new Color(0.62f, 0.64f, 0.7f, 1f);
        private FriendService _friendService;

        public void SetData(FriendData data, FriendService service, Action refresh)
        {
            if (data == null) return;
            _friendService = service;
            nameText.text = data.Nickname;
            headImage.sprite = AvatarConfig.Get(data.Avatar);
            bool isRequest = data.IsRequest;
            applyButtons.SetActive(isRequest);
            statusText.gameObject.SetActive(!isRequest);

            // 未读红点只看这个好友自己的未读数 —— 面板是复用的，每次填充都要重设
            redDot.SetActive(!isRequest && data.Unread > 0);

            SetSelected(false);
            if (!isRequest)
            {
                statusText.text = data.IsOnline ? "在线" : "离线";
                statusText.color = data.IsOnline ? ColOnline : ColOffline;
            }
            else
            {
                agreeBtn.onClick.RemoveAllListeners();
                agreeBtn.onClick.AddListener(() => OnHandleRequest(data.RoleId, true, refresh));

                rejectBtn.onClick.RemoveAllListeners();
                rejectBtn.onClick.AddListener(() => OnHandleRequest(data.RoleId, false, refresh));
            }
        }

        // 重拉一次能让界面自动纠正回真实状态。
        private void OnHandleRequest(int fromRoleId, bool accept, Action refresh)
        {
            _friendService?.RequestHandleFriend(fromRoleId, accept, (ok, tip) =>
            {
                AppContext.Ui.ShowTip(tip);
                _friendService?.RequestFriendList(refresh);
            });
        }

        public void SetSelected(bool selected)
        {
            selectedMark.SetActive(selected);
        }
    }
}