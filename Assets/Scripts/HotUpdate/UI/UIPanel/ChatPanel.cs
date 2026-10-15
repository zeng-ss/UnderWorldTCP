using System;
using DG.Tweening;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.RedDot;
using HotUpdate.UI.UIItem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("ChatPanel")]
    public class ChatPanel : BasePanel
    {
        // 消息气泡模板
        private const string BubbleSelf = "ChatRootRight"; // 自己发的：靠右
        private const string BubblePeer = "ChatRootLeft";  // 对方发的：靠左

        private GameObject _content;

        // UI组件
        private TMP_InputField _chatInput;
        private ScrollRect _chatScrollView;

        // 气泡模板只加载一次并缓存住。
        // 不能每条消息都走一次异步实例化：左右两个气泡是各自的加载 handle，完成时机不同，
        // 回调顺序不等于消息顺序 —— 第一次打开就会出现历史消息错位（第二次命中缓存才正常）。
        private GameObject _bubbleSelf, _bubblePeer;
        private bool _bubbleLoadStarted;

        protected override void Awake()
        {
            base.Awake();
            _content = GameObject.Find("DialogueItemContent").gameObject;
            _chatInput = GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();
            _chatScrollView = transform.Find("Scroll View").GetComponent<ScrollRect>();
        }

        private void Start()
        {
            _chatInput.onEndEdit.AddListener(OnEndEditMessage);
        }

        private void OnEnable()
        {
            InputManager.Instance.RegisterKeyDown(KeyCode.Return, OnEnterPressed);
            InputManager.Instance.RegisterKeyDown(KeyCode.KeypadEnter, OnEnterPressed);

            // 打开面板 = 消息都看到了，清掉红点
            if (AppContext.IsAlive) AppContext.RedDot.Clear(RedDotPath.Chat);

            // 重开面板要把上一轮渲染的气泡清掉，否则历史会被叠加渲染好几遍
            ClearChatItems();
            EnsureBubblePrefabs();
            AppendHistory();
        }

        private void OnDisable()
        {
            InputManager.Instance.UnregisterKeyDown(KeyCode.Return, OnEnterPressed);
            InputManager.Instance.UnregisterKeyDown(KeyCode.KeypadEnter, OnEnterPressed);
        }

        /// <summary>收到一条聊天消息（由 ChatService 直接调用，不再走全局事件）</summary>
        public void OnMessageReceived(MessageData message)
        {
            bool isSelf = message != null && message.SenderClientId == 0;
            AddChatItem(message, isSelf);
        }

        /// <summary>回车激活输入框。注册到 InputManager，不再每帧轮询。</summary>
        private void OnEnterPressed()
        {
            if (_chatInput == null || _chatInput.isFocused) return;
            _chatInput.ActivateInputField();
            _chatInput.Select();
        }

        private void OnEndEditMessage(string inputMes)
        {
            if (string.IsNullOrEmpty(inputMes)) return;
            _chatInput.text = "";
            DateTime now = DateTime.Now;
            AppContext.Chat.SendLocal(inputMes, now.ToString("HH:mm:ss"));
        }

        // 气泡模板加载（两个都到齐后自动补渲染一次）
        private void EnsureBubblePrefabs()
        {
            if (_bubbleSelf != null && _bubblePeer != null) return;
            if (_bubbleLoadStarted) return;
            _bubbleLoadStarted = true;

            AppContext.Res.LoadAssetAsync<GameObject>(BubbleSelf, prefab =>
            {
                _bubbleSelf = prefab;
                AppendHistory();
            });

            AppContext.Res.LoadAssetAsync<GameObject>(BubblePeer, prefab =>
            {
                _bubblePeer = prefab;
                AppendHistory();
            });
        }

        // 模板没到齐就先空着，到齐的回调会再来调一次
        private void AppendHistory()
        {
            if (_bubbleSelf == null || _bubblePeer == null) return;

            foreach (var message in AppContext.Chat.Messages)
            {
                AddChatItem(message, message.SenderClientId == 0);
            }
        }

        private void AddChatItem(MessageData messageData, bool isLocal)
        {
            // 模板还在加载：这条先跳过，历史会在加载完成的回调里补齐
            GameObject prefab = isLocal ? _bubbleSelf : _bubblePeer;
            if (prefab == null || messageData == null) return;

            GameObject chatItem = Instantiate(prefab, _content.transform);
            chatItem.GetComponent<ChatItem>().UpdateDate(messageData);

            // 直接 DOTween 滚动
            DOTween.To(
                () => _chatScrollView.verticalNormalizedPosition,
                x => _chatScrollView.verticalNormalizedPosition = x,
                0f, // 目标值（底部）
                0.4f // 动画时间
            ).SetEase(Ease.OutBack);
        }

        public void ClearChatItems()
        {
            for (var i = 0; i < _content.transform.childCount; i++)
            {
                Destroy(_content.transform.GetChild(i).gameObject);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _chatInput.onEndEdit.RemoveListener(OnEndEditMessage);
        }
    }
}
