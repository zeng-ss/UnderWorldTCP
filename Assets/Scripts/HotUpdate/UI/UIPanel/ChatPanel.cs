using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PanelPath("ChatPanel")]
public class ChatPanel : BasePanel
{
    private GameObject _content;

    // UI组件
    private TMP_InputField _chatInput;
    private ScrollRect _chatScrollView;

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
        AppContext.Events.AddEventListener(GameEvent.ChatMessageReceived, OnChatMessageReceived);

        // 补上打开面板前就已经收到的消息
        foreach (var message in AppContext.Chat.Messages) AddChatItem(message, false);
    }

    private void OnDisable()
    {
        InputManager.Instance.UnregisterKeyDown(KeyCode.Return, OnEnterPressed);
        InputManager.Instance.UnregisterKeyDown(KeyCode.KeypadEnter, OnEnterPressed);
        AppContext.Events.RemoveEventListener(GameEvent.ChatMessageReceived, OnChatMessageReceived);
    }

    private void OnChatMessageReceived(EventArgs args)
    {
        var message = ((ChatMessageArgs)args).Message;
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

    private void AddChatItem(MessageData messageData, bool isLocal)
    {
        AppContext.Res.LoadAndInstantiateAsync(
            isLocal ? "ChatRootRight" : "ChatRootLeft", _content.transform,
            chatItem =>
            {
                chatItem.GetComponent<ChatItem>().UpdateDate(messageData);
                // 直接 DOTween 滚动
                DOTween.To(
                    () => _chatScrollView.verticalNormalizedPosition,
                    x => _chatScrollView.verticalNormalizedPosition = x,
                    0f, // 目标值（底部）
                    0.4f // 动画时间
                ).SetEase(Ease.OutBack);
            });
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