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
    // 房间面板：一个面板两种形态。
    //   ListArea —— 房间大厅（房间列表 + 创建 + 加入 + 刷新）
    //   RoomArea —— 房间内（玩家列表 + 聊天 + 准备 / 开始 / 退出）
    [PanelPath("RoomPanel")]
    public class RoomPanel : BasePanel
    {
        private const string RoomItemAddress = "RoomListItem";
        private const string MemberItemAddress = "RoomPlayerItem";

        // 聊天气泡
        private const string BubbleSelf = "ChatRootRight";
        private const string BubblePeer = "ChatRootLeft";

        // 房间服务
        private RoomService _room;

        // 两种形态的容器
        private GameObject _listArea, _roomArea;

        // ---- 大厅 ----
        private LoopList _roomList;
        private readonly List<RoomItemData> _roomEntries = new();
        private GameObject _roomItemPrefab;
        private TMP_InputField _roomNameInput, _roomIdInput;

        // ---- 房间内 ----
        private LoopList _memberList;
        private readonly List<RoomPlayerData> _memberEntries = new();
        private GameObject _memberItemPrefab;
        private TMP_Text _chatTitle;
        private Button _readyBtn, _startBtn;

        // ---- 聊天 ----
        private ScrollRect _msgScroll;
        private RectTransform _msgContent;
        private TMP_InputField _msgInput;
        private Coroutine _scrollRoutine;

        // 打开时独占操作
        public override bool BlocksGameplayInput => true;

        protected override void Awake()
        {
            base.Awake();
            _room = new RoomService();
            BindHierarchy();
        }

        public override void Show()
        {
            base.Show();
            if (!AppContext.IsAlive) return;

            LoadPrefabs();
            // 已在房间里 直接展示房间形态，否则拉一次房间列表
            if (_room.IsInRoom)
                EnterRoomView();
            else
                EnterListView();
        }

        #region 服务端推送入口

        /// <summary>房间成员 / 准备状态发生变化（服务端广播）</summary>
        public void OnRoomInfoChanged(RoomInfoNtf ntf)
        {
            if (!gameObject.activeInHierarchy) return;

            _room.ApplyRoomInfo(ntf);
            EnterRoomView();
        }

        /// <summary>收到房间内一条聊天消息</summary>
        private void OnRoomChatReceived(RoomChatNtf ntf)
        {
            if (!gameObject.activeInHierarchy || ntf?.Message == null) return;

            _room.ApplyRoomChat(ntf.Message);
            RefreshMessages();
        }

        /// <summary>自己被房主踢出：退回房间列表页（创建 / 查找房间那一页）</summary>
        public void OnKicked(string tip)
        {
            _room?.ApplyKicked();
            if (!gameObject.activeInHierarchy) return;

            AppContext.Ui.ShowTip(string.IsNullOrEmpty(tip) ? "你已被移出房间" : tip);
            EnterListView();
        }

        #endregion

        #region 绑定层级

        // 按生成脚本的节点命名绑定；节点缺失时告警但不抛异常
        private void BindHierarchy()
        {
            Transform panel = transform.Find("Panel");
            if (panel == null)
            {
                Debug.LogError("[RoomPanel] 预制体缺少 Panel 子节点，无法绑定。请用 Tools/UI/房间面板 重新生成。");
                return;
            }

            _listArea = FindDeep(panel, "ListArea")?.gameObject;
            _roomArea = FindDeep(panel, "RoomArea")?.gameObject;

            BindRoomList(FindDeep(panel, "RoomScroll"));
            BindMemberList(FindDeep(panel, "PlayerScroll"));

            // 聊天区：气泡直接挂到 ScrollRect 的 Content 下（自适应高度）
            _msgScroll = FindDeep(panel, "MsgScroll")?.GetComponent<ScrollRect>();
            _msgContent = _msgScroll != null ? _msgScroll.content : null;
            if (_msgScroll == null || _msgContent == null)
                Debug.LogError("[RoomPanel] 未找到 MsgScroll / Content，房间聊天无法显示。");

            _chatTitle = FindDeep(panel, "ChatTitle")?.GetComponent<TMP_Text>();
            _msgInput = FindDeep(panel, "MsgInput")?.GetComponent<TMP_InputField>();
            _roomNameInput = FindDeep(panel, "RoomNameInput")?.GetComponent<TMP_InputField>();
            _roomIdInput = FindDeep(panel, "RoomIdInput")?.GetComponent<TMP_InputField>();

            _readyBtn = FindDeep(panel, "ReadyBtn")?.GetComponent<Button>();
            _startBtn = FindDeep(panel, "StartBtn")?.GetComponent<Button>();

            BindClick(panel, "HomeBtn", OnHomeClicked);
            BindClick(panel, "CreateBtn", OnCreateClicked);
            BindClick(panel, "JoinBtn", OnJoinClicked);
            BindClick(panel, "RefreshBtn", OnRefreshClicked);
            BindClick(panel, "SendBtn", OnSendClicked);
            BindClick(panel, "LeaveBtn", OnLeaveClicked);
            if (_readyBtn != null) _readyBtn.onClick.AddListener(OnReadyClicked);
            if (_startBtn != null) _startBtn.onClick.AddListener(OnStartClicked);
        }

        private void BindRoomList(Transform host)
        {
            if (host == null) return;

            var scroll = host.GetComponent<ScrollRect>();
            if (scroll == null || scroll.content == null)
            {
                Debug.LogError("[RoomPanel] RoomScroll 缺少 ScrollRect / Content，房间列表无法初始化。");
                return;
            }

            _roomList = host.GetComponent<LoopList>();
            if (_roomList == null) _roomList = host.gameObject.AddComponent<LoopList>();
            _roomList.Init(scroll, scroll.content);
            _roomList.SetItemFactory(CreateRoomItem, FillRoomItem);
        }

        private void BindMemberList(Transform host)
        {
            if (host == null) return;

            var scroll = host.GetComponent<ScrollRect>();
            if (scroll == null || scroll.content == null)
            {
                Debug.LogError("[RoomPanel] PlayerScroll 缺少 ScrollRect / Content，玩家列表无法初始化。");
                return;
            }

            _memberList = host.GetComponent<LoopList>();
            if (_memberList == null) _memberList = host.gameObject.AddComponent<LoopList>();
            _memberList.Init(scroll, scroll.content);
            _memberList.SetItemFactory(CreateMemberItem, FillMemberItem);
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

        #region 形态切换

        private void EnterListView()
        {
            if (_listArea != null) _listArea.SetActive(true);
            if (_roomArea != null) _roomArea.SetActive(false);

            RefreshRoomList();
            _room.RequestRoomList(RefreshRoomList);
        }

        private void EnterRoomView()
        {
            if (_listArea != null) _listArea.SetActive(false);
            if (_roomArea != null) _roomArea.SetActive(true);

            if (_chatTitle != null) _chatTitle.text = $"房间 {_room.CurrentRoomId}";

            RefreshMembers();
            RefreshMessages();
        }

        #endregion

        #region 大厅（房间列表 / 创建 / 加入）

        private void LoadPrefabs()
        {
            if (_roomItemPrefab == null)
            {
                AppContext.Res.LoadAssetAsync<GameObject>(RoomItemAddress, prefab =>
                {
                    _roomItemPrefab = prefab;
                    RefreshRoomList();
                });
            }

            if (_memberItemPrefab == null)
            {
                AppContext.Res.LoadAssetAsync<GameObject>(MemberItemAddress, prefab =>
                {
                    _memberItemPrefab = prefab;
                    RefreshMembers();
                });
            }
        }

        private GameObject CreateRoomItem() => _roomItemPrefab != null ? Instantiate(_roomItemPrefab) : null;

        private GameObject CreateMemberItem() => _memberItemPrefab != null ? Instantiate(_memberItemPrefab) : null;

        private void RefreshRoomList()
        {
            _roomEntries.Clear();
            if (_room != null) _roomEntries.AddRange(_room.Rooms);
            _roomList?.SetCount(_roomEntries.Count);
        }

        private void FillRoomItem(int index, GameObject item)
        {
            if (index < 0 || index >= _roomEntries.Count) return;
            RoomItemData data = _roomEntries[index];

            var view = item.GetComponent<RoomListItem>();
            if (view == null)
            {
                Debug.LogWarning("[RoomPanel] RoomListItem 预制体缺少 RoomListItem 组件，请用 Tools/UI/房间面板 重新生成并注册。");
                return;
            }

            view.SetData(data);

            // 整行点击 = 加入该房间；已开局的房间不可加入
            var rowBtn = item.GetComponent<Button>();
            if (rowBtn != null)
            {
                rowBtn.onClick.RemoveAllListeners();
                rowBtn.interactable = !data.IsPlaying;
                int roomId = data.RoomId;
                rowBtn.onClick.AddListener(() => JoinRoom(roomId));
            }
        }

        private void OnRefreshClicked() => _room?.RequestRoomList(RefreshRoomList);

        private void OnCreateClicked()
        {
            string roomName = _roomNameInput != null ? _roomNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(roomName))
                roomName = (AppContext.Session.PlayerName ?? "玩家") + "的房间";

            _room?.CreateRoom(roomName, (ok, tip) =>
            {
                if (!ok)
                {
                    AppContext.Ui.ShowTip(tip);
                    return;
                }

                if (_roomNameInput != null) _roomNameInput.text = "";
                EnterRoomView();
            });
        }

        private void OnJoinClicked()
        {
            string input = _roomIdInput != null ? _roomIdInput.text.Trim() : "";
            if (!int.TryParse(input, out int roomId) || roomId <= 0)
            {
                AppContext.Ui.ShowTip("请输入正确的房间号");
                return;
            }

            JoinRoom(roomId);
        }

        private void JoinRoom(int roomId)
        {
            _room?.JoinRoom(roomId, (ok, tip) =>
            {
                if (!ok)
                {
                    AppContext.Ui.ShowTip(tip);
                    return;
                }

                if (_roomIdInput != null) _roomIdInput.text = "";
                EnterRoomView();
            });
        }

        #endregion

        #region 房间内（玩家列表 / 准备 / 开始 / 退出）

        private void RefreshMembers()
        {
            _memberEntries.Clear();
            if (_room != null) _memberEntries.AddRange(_room.Members);
            _memberList?.SetCount(_memberEntries.Count);

            RefreshButtons();
        }

        private void FillMemberItem(int index, GameObject item)
        {
            if (index < 0 || index >= _memberEntries.Count) return;

            var view = item.GetComponent<RoomPlayerItem>();
            if (view == null)
            {
                Debug.LogWarning("[RoomPanel] RoomPlayerItem 预制体缺少 RoomPlayerItem 组件，请用 Tools/UI/房间面板 重新生成并注册。");
                return;
            }

            RoomPlayerData data = _memberEntries[index];
            bool isSelf = data.RoleId == AppContext.Session.RoleId;

            // 只有房主能踢人，且不能踢自己 / 房主自己（服务端还会再校验一次）
            bool canKick = _room != null && _room.IsMaster && !isSelf && !data.IsMaster;
            int roleId = data.RoleId;
            view.SetData(data, isSelf, canKick, canKick ? () => KickPlayer(roleId) : null);
        }

        // 踢人：服务端校验通过后会广播新的 RoomInfoNtf，成员列表随之刷新
        private void KickPlayer(int targetRoleId)
        {
            _room?.RequestKickPlayer(targetRoleId, (ok, tip) =>
            {
                if (!ok)
                {
                    AppContext.Ui.ShowTip(tip);
                    return;
                }

                AppContext.Ui.ShowTip("已将该玩家移出房间");
            });
        }

        // 准备按钮文案跟随自己的准备态；开始按钮只对房主开放，且需所有非房主已准备
        private void RefreshButtons()
        {
            bool isMaster = _room != null && _room.IsMaster;

            if (_readyBtn != null)
            {
                RoomPlayerData self = _memberEntries.Find(m => m.RoleId == AppContext.Session.RoleId);
                bool ready = self != null && self.IsReady;
                _readyBtn.gameObject.SetActive(!isMaster);
                SetButtonLabel(_readyBtn, ready ? "取消准备" : "准备");
            }

            if (_startBtn != null)
            {
                _startBtn.gameObject.SetActive(isMaster);
                _startBtn.interactable = isMaster && AllOthersReady();
            }
        }

        private bool AllOthersReady()
        {
            foreach (RoomPlayerData m in _memberEntries)
            {
                if (!m.IsMaster && !m.IsReady) return false;
            }

            return true;
        }

        private void OnReadyClicked()
        {
            RoomPlayerData self = _memberEntries.Find(m => m.RoleId == AppContext.Session.RoleId);
            bool next = self == null || !self.IsReady;
            _room?.RequestReady(next);
        }

        private void OnStartClicked()
        {
            if (_room == null || !_room.IsMaster) return;
            if (!AllOthersReady())
            {
                AppContext.Ui.ShowTip("还有玩家未准备");
                return;
            }

            _room.RequestStartGame();
        }

        private void OnLeaveClicked()
        {
            _room?.LeaveRoom(() =>
            {
                if (!gameObject.activeInHierarchy) return;
                EnterListView();
            });
        }

        // 返回主界面：先退出房间（避免留下幽灵房间），再回到开始面板所在的场景
        private void OnHomeClicked()
        {
            if (_room is { IsInRoom: true }) _room.LeaveRoom();

            AppContext.Proto.RequestChangeScene("EnterScene", ret =>
            {
                if (ret.CmdCode != CmdCode.Succeed)
                {
                    Debug.LogError($"跳转场景失败：{ret.CmdCode}");
                    return;
                }

                AppContext.Scene.LoadScene("EnterScene", 0.4f, TransitionPanel.ScrollBgType.Normal, OnCoveredHandle);
            });
        }

        private void OnCoveredHandle()
        {
            AppContext.Ui.ClosePanel(this);
            AppContext.Ui.OpenPanel<StartPanel>();
        }

        #endregion

        #region 房间聊天

        // 气泡预制体只加载一次并缓存住。
        private GameObject _bubbleSelfPrefab, _bubblePeerPrefab;
        private bool _bubbleLoadStarted;

        private void EnsureBubblePrefabs()
        {
            if (_bubbleSelfPrefab != null && _bubblePeerPrefab != null) return;
            if (_bubbleLoadStarted) return;
            _bubbleLoadStarted = true;

            if (_bubbleSelfPrefab == null)
            {
                AppContext.Res.LoadAssetAsync<GameObject>(BubbleSelf, prefab =>
                {
                    _bubbleSelfPrefab = prefab;
                    RefreshMessages();
                });
            }

            if (_bubblePeerPrefab == null)
            {
                AppContext.Res.LoadAssetAsync<GameObject>(BubblePeer, prefab =>
                {
                    _bubblePeerPrefab = prefab;
                    RefreshMessages();
                });
            }
        }

        private void RefreshMessages()
        {
            if (_msgContent == null) return;

            for (int i = _msgContent.childCount - 1; i >= 0; i--) Destroy(_msgContent.GetChild(i).gameObject);

            if (!AppContext.IsAlive || _room == null) return;

            List<RoomChatData> messages = _room.ChatMessages;
            if (messages.Count == 0) return;

            // 模板还没加载完：加载完的回调会自己再刷一次
            if (_bubbleSelfPrefab == null || _bubblePeerPrefab == null)
            {
                EnsureBubblePrefabs();
                return;
            }

            // 模板已在内存里，同步实例化才能保证顺序严格等于消息顺序（最新的在最后）
            foreach (RoomChatData msg in messages)
            {
                GameObject prefab = msg.IsSelf ? _bubbleSelfPrefab : _bubblePeerPrefab;
                if (prefab == null) continue;

                GameObject item = Instantiate(prefab, _msgContent);
                item.GetComponent<ChatItem>()?.SetContent(msg.SenderNickname, msg.SendTime, msg.Content,
                    msg.SenderAvatar);
            }

            ScrollToBottomSoon();
        }

        private void OnSendClicked()
        {
            string content = _msgInput != null ? _msgInput.text : "";
            if (string.IsNullOrEmpty(content)) return;

            _msgInput.text = "";
            _room?.SendChat(content, OnRoomChatReceived);
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

        #endregion

        protected override void OnCloseClicked()
        {
            // 关面板时若还在房间里，顺手退出，避免留下「幽灵房间」
            if (_room != null && _room.IsInRoom) _room.LeaveRoom();
            AppContext.Ui.ClosePanel(this);
        }

        private static void SetButtonLabel(Button btn, string label)
        {
            if (btn == null) return;
            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true)) t.text = label;
        }
    }
}