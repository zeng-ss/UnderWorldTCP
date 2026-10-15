using System;
using HotUpdate.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    // 房间内玩家列表项：头像 + 昵称 + 准备 / 房主状态。
    // 房主视角下，鼠标移到别人那一行会盖上「踢出该玩家」蒙层，点这一行即踢人；
    // 自己那一行和房主那一行不给踢（服务端也会再校验一次）。
    public class RoomPlayerItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image headImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private GameObject kickTip;
        [SerializeField] private Button rowButton;

        private static readonly Color ColMaster = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color ColReady = new Color(0.3f, 0.9f, 0.4f, 1f);
        private static readonly Color ColNotReady = new Color(0.62f, 0.64f, 0.7f, 1f);

        private bool _canKick;
        private Action _onKick;

        private void Awake()
        {
            // 列表项统一带悬停放大（模板上没挂就补一个）
            if (!TryGetComponent<HoverScale>(out _)) gameObject.AddComponent<HoverScale>();
        }

        public void SetData(RoomPlayerData data, bool isSelf, bool canKick, Action onKick = null)
        {
            if (data == null) return;

            nameText.text = isSelf ? data.Nickname + "（我）" : data.Nickname;
            if (data.IsMaster)
            {
                stateText.text = "房主";
                stateText.color = ColMaster;
            }
            else
            {
                stateText.text = data.IsReady ? "已准备" : "未准备";
                stateText.color = data.IsReady ? ColReady : ColNotReady;
            }

            // 行会被 LoopList 复用，每次都要重新挂监听
            _canKick = canKick;
            _onKick = onKick;
            SetKickTipVisible(false);
            rowButton.onClick.RemoveAllListeners();
            if (canKick) rowButton.onClick.AddListener(HandleKick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_canKick) SetKickTipVisible(true);
        }

        public void OnPointerExit(PointerEventData eventData) => SetKickTipVisible(false);

        private void HandleKick() => _onKick?.Invoke();

        // 隐藏时连同整个对象关掉；蒙层上的 Image 是 raycastTarget=false，不会挡住整行点击
        private void SetKickTipVisible(bool visible)
        {
            if (kickTip is not null && kickTip.activeSelf != visible) kickTip.SetActive(visible);
        }
    }
}