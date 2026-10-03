using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatPanel : BasePanel
{
    private GameController gameController;
    private GameObject content;
    
    // UI组件
    [HideInInspector] public TMP_InputField chatInput;
    private ScrollRect chatScrollView;

    public override void Awake()
    {
        content = GameObject.Find("DialogueItemContent").gameObject;
        chatInput = GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();
        chatScrollView = transform.Find("Scroll View").GetComponent<ScrollRect>();
    }
    public void Start()
    {
        chatInput.onEndEdit.AddListener(OnEndEditMessage);
        gameController = FindObjectOfType<GameController>();
    }
    
    private void Update()
    {
        // 按回车激活输入框
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInput.isFocused) return;
            chatInput.ActivateInputField();
            chatInput.Select();
        }
    }

    private void OnEndEditMessage(string inputMes)
    {
        if (string.IsNullOrEmpty(inputMes)) return;
        chatInput.text = "";
        DateTime now = DateTime.Now;
        GameManager.Instance.SendMes(inputMes, now.ToString("HH:mm:ss"));
    }

    public void AddChatItem(MessageData messageData, bool isLocal)
    {
        ResMgr.Instance.LoadAndInstantiateAsync(isLocal ? "Assets/Res/UI/UIItem/ChatRootRight" : "Assets/Res/UI/UIItem/ChatRootLeft", content.transform,chatItem =>
        {
            chatItem.GetComponent<ChatItem>().UpdateDate(messageData); 
            // 直接 DOTween 滚动
            DOTween.To(
                () => chatScrollView.verticalNormalizedPosition,
                x => chatScrollView.verticalNormalizedPosition = x,
                0f, // 目标值（底部）
                0.4f // 动画时间
            ).SetEase(Ease.OutBack);
        });
    }

    public void ClearChatItems() { for (var i = 0; i < content.transform.childCount; i++) { Destroy(content.transform.GetChild(i).gameObject); } }

    private void OnDestroy()
    {
        chatInput.onEndEdit.RemoveListener(OnEndEditMessage);
    }
}
