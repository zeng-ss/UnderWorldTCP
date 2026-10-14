using System.Collections;
using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.Service;
using HotUpdate.UI.UIItem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("FriendPanel")]
    public class FriendPanel : BasePanel
    {
        // 好友列表项：固定高度，需与编辑器脚本 FriendItemHeight 一致
        private const float FriendItemHeight = 64f;
        private const string FriendItemAddress = "FriendItem";

        // 消息气泡
        private const string BubbleSelf = "ChatRootRight"; // 自己发的：靠右
        private const string BubblePeer = "ChatRootLeft"; // 对方发的：靠左

        // 好友列表（申请在前、好友在后，共用一个 LoopList）
        private LoopList _friendList;
        private readonly List<FriendData> _entries = new();
        private GameObject _friendItemPrefab;
        private TMP_InputField _addFriendInput;

        // 聊天区
        private ScrollRect _msgScroll;
        private RectTransform _msgContent;
        private TMP_InputField _msgInput;
        private TMP_Text _chatTitle;
        private int _chatRoleId = -1;
        private Coroutine _scrollRoutine;

        // 打开时独占操作、锁住玩法输入
        public override bool BlocksGameplayInput => true;

        // 好友服务挂在 AppContext 上（生命周期随客户端），面板只取引用、不持有
        private FriendService _friendService;

        protected override void Awake()
        {
            base.Awake();
            _friendService = new FriendService();
            _friendService.Init();
            BindHierarchy();
        }

        public override void Show()
        {
            base.Show();
            if (!AppContext.IsAlive) return;

            LoadFriendItemPrefab();
            RefreshFriendList();
            UpdateChatTitle();
            RefreshMessages();

            // 每次打开都跟服务端对齐一次：好友列表 + 待处理申请一起拉
            _friendService?.RequestFriendList(RefreshFriendList);
        }

        #region 由 FriendService 调用

        /// <summary>收到一条私聊消息时刷新当前聊天窗口</summary>
        public void OnMessageReceived(FriendMsgData msg)
        {
            // 面板没开就先不刷（消息已进 FriendService 缓存，Show() 时会重新拉出来）
            if (msg == null || !gameObject.activeInHierarchy) return;
            if (_chatRoleId <= 0) return;

            int peerId = msg.SenderRoleId == AppContext.Session.RoleId ? msg.ReceiverRoleId : msg.SenderRoleId;
            if (peerId == _chatRoleId) RefreshMessages();
        }

        /// <summary>收到新的好友申请推送：面板开着就立刻重拉，把申请顶到列表最前面</summary>
        public void OnFriendApplyReceived()
        {
            if (!gameObject.activeInHierarchy) return;
            _friendService?.RequestFriendList(RefreshFriendList);
        }

        #endregion

        #region 绑定预制体层级

        // 按生成脚本的节点命名绑定；节点缺失时告警但不抛异常
        private void BindHierarchy()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                Debug.LogError("[FriendPanel] 预制体缺少 Panel 子节点，无法绑定。请用 Tools/UI/好友面板 重新生成。");
                return;
            }

            BindFriendList(FindDeep(panel, "FriendScroll"));

            // 消息区：只要一个 ScrollRect，气泡直接挂到它的 Content 下（自适应高度）
            _msgScroll = FindDeep(panel, "MsgScroll")?.GetComponent<ScrollRect>();
            _msgContent = _msgScroll != null ? _msgScroll.content as RectTransform : null;
            if (_msgScroll == null || _msgContent == null)
                Debug.LogError("[FriendPanel] 未找到 MsgScroll / Content，聊天消息无法显示。");

            _chatTitle = FindDeep(panel, "ChatTitle")?.GetComponent<TMP_Text>();
            _msgInput = FindDeep(panel, "MsgInput")?.GetComponent<TMP_InputField>();
            _addFriendInput = FindDeep(panel, "AddFriendInput")?.GetComponent<TMP_InputField>();

            BindClick(panel, "CloseBtn", OnCloseClicked);
            BindClick(panel, "AddFriendBtn", OnAddFriendClicked);
            BindClick(panel, "SendBtn", OnSendClicked);
        }

        private void BindFriendList(Transform host)
        {
            if (host == null) return;

            var scroll = host.GetComponent<ScrollRect>();
            if (scroll == null || scroll.content == null)
            {
                Debug.LogError("[FriendPanel] FriendScroll 缺少 ScrollRect / Content，好友列表无法初始化。");
                return;
            }

            _friendList = host.GetComponent<LoopList>();
            if (_friendList == null) _friendList = host.gameObject.AddComponent<LoopList>();

            _friendList.Init(scroll, scroll.content);
            _friendList.SetItemHeight(FriendItemHeight);
            _friendList.SetItemFactory(CreateFriendItem, FillFriendItem);
        }

        private void BindClick(Transform root, string nodeName, UnityEngine.Events.UnityAction action)
        {
            var btn = FindDeep(root, nodeName)?.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
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

        #region 好友列表（申请 + 好友）

        private void LoadFriendItemPrefab()
        {
            if (_friendItemPrefab != null) return;
            AppContext.Res.LoadAssetAsync<GameObject>(FriendItemAddress, prefab =>
            {
                _friendItemPrefab = prefab;
                RefreshFriendList();
            });
        }

        // 模板还没加载完时返回 null，LoopList 会跳过并等下一次刷新
        private GameObject CreateFriendItem()
        {
            return _friendItemPrefab != null ? Instantiate(_friendItemPrefab) : null;
        }

        // 组装列表：待处理申请排在最前面，其后是已同意的好友
        private void RefreshFriendList()
        {
            _entries.Clear();

            if (_friendService != null)
            {
                _entries.AddRange(_friendService.Applies);
                _entries.AddRange(_friendService.Friends);
            }

            _friendList?.SetCount(_entries.Count);
        }

        private void FillFriendItem(int index, GameObject item)
        {
            if (index < 0 || index >= _entries.Count) return;
            FriendData entry = _entries[index];

            // 展示交给 FriendItem（预制体自带），面板只管点击与选中态
            var view = item.GetComponent<FriendItem>();
            if (view == null)
            {
                Debug.LogWarning("[FriendPanel] FriendItem 预制体缺少 FriendItem 组件，" +
                                 "请用 Tools/UI/好友面板 重新生成并注册。");
                return;
            }

            view.SetData(entry, _friendService, RefreshFriendList);
            view.SetSelected(!entry.IsRequest && entry.RoleId == _chatRoleId);

            // 整行按钮：好友项点击开聊天；申请项整行不可点（用行内的同意 / 拒绝）
            var rowBtn = item.GetComponent<Button>();
            if (rowBtn != null)
            {
                rowBtn.onClick.RemoveAllListeners();
                rowBtn.interactable = !entry.IsRequest;
                if (!entry.IsRequest)
                {
                    int roleId = entry.RoleId;
                    rowBtn.onClick.AddListener(() => OpenChatWith(roleId));
                }
            }
        }

        #endregion

        #region 聊天区

        private void OpenChatWith(int friendRoleId)
        {
            _chatRoleId = friendRoleId;
            UpdateChatTitle();

            // 好友项会被 LoopList 复用，切换聊天对象后要重画一遍才能刷新选中高亮
            _friendList?.RefreshItems();

            // 拉最近 100 条历史后再渲染
            _friendService?.OpenChat(friendRoleId, RefreshMessages);
        }

        private void UpdateChatTitle()
        {
            if (_chatTitle == null) return;

            if (_chatRoleId <= 0)
            {
                _chatTitle.text = "选择好友开始聊天";
                return;
            }

            FriendData friend = _entries.Find(f => f.RoleId == _chatRoleId);
            _chatTitle.text = friend != null ? friend.Nickname : ("好友 " + _chatRoleId);
        }

        // 整窗重建：清掉旧气泡，再按缓存逐条实例化 ChatRootLeft / ChatRootRight
        private void RefreshMessages()
        {
            if (_msgContent == null) return;

            for (int i = _msgContent.childCount - 1; i >= 0; i--) Destroy(_msgContent.GetChild(i).gameObject);

            if (!AppContext.IsAlive || _chatRoleId <= 0) return;

            List<FriendMsgData> messages = _friendService?.GetChatCache(_chatRoleId);
            if (messages == null || messages.Count == 0) return;

            FriendData friend = _entries.Find(f => f.RoleId == _chatRoleId);
            string peerName = friend != null ? friend.Nickname : ("好友 " + _chatRoleId);

            foreach (FriendMsgData msg in messages)
            {
                FriendMsgData captured = msg;
                string who = captured.IsSelf ? "我" : peerName;

                AppContext.Res.LoadAndInstantiateAsync(
                    captured.IsSelf ? BubbleSelf : BubblePeer, _msgContent,
                    item =>
                    {
                        if (item == null) return;
                        item.GetComponent<ChatItem>()?.SetContent(who, captured.SendTime, captured.Content);
                        ScrollToBottomSoon();
                    });
            }
        }

        // ContentSizeFitter 要下一帧才算好高度，等一帧再贴底；异步实例化再补一次
        private void ScrollToBottomSoon()
        {
            if (!isActiveAndEnabled) return;

            if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
            _scrollRoutine = StartCoroutine(ScrollToBottomRoutine());
        }

        private IEnumerator ScrollToBottomRoutine()
        {
            yield return null;
            ScrollToBottom();
            yield return null;
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (_msgScroll == null || _msgContent == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_msgContent);
            _msgScroll.verticalNormalizedPosition = 0f;
        }

        private void OnSendClicked()
        {
            if (_chatRoleId <= 0)
            {
                AppContext.Ui.ShowTip("请先选择好友");
                return;
            }

            string content = _msgInput != null ? _msgInput.text : "";
            if (string.IsNullOrEmpty(content)) return;
            _msgInput.text = "";
            _friendService?.SendMessage(_chatRoleId, content);
        }

        private void OnAddFriendClicked()
        {
            string input = _addFriendInput != null ? _addFriendInput.text : "";
            if (string.IsNullOrEmpty(input)) return;
            if (!int.TryParse(input, out int targetRoleId) || targetRoleId <= 0)
            {
                AppContext.Ui.ShowTip("请输入正确的角色ID");
                return;
            }

            _addFriendInput.text = "";
            _friendService?.RequestAddFriend(targetRoleId);
        }

        #endregion
    }
}
