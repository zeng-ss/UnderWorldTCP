using System;
using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Service
{
    /// <summary>
    /// 好友系统服务：好友列表、待处理好友申请、私聊消息（发送 / 接收 / 历史）。
    /// </summary>
    public class FriendService
    {
        // 好友列表（已同意的双向关系）
        public List<FriendData> Friends { get; } = new();

        // 待处理的好友申请（别人加我、我还没处理），每个元素 IsRequest=true
        public List<FriendData> Applies { get; } = new();

        // 当前正在聊天的好友 id（0 表示未打开任何聊天）
        public int CurrentChatRoleId { get; private set; }

        // 每个好友的聊天缓存：friendRoleId → 消息列表（按时间升序）
        private readonly Dictionary<int, List<FriendMsgData>> _chatCache = new();

        public void Init() => AppContext.Proto.OnServerPushMessageReceived += OnServerPushMessage;

        #region 好友列表 / 申请

        /// <summary>
        /// 拉取好友列表 + 待处理申请，完成后回调（用于刷新 UI）。申请也走这个接口：服务端把 status==0 且指向我的记录一并下发，
        /// </summary>
        public void RequestFriendList(Action onRefreshed = null)
        {
            AppContext.Proto.RequestFriendList(ret =>
            {
                Friends.Clear();
                Applies.Clear();

                if (ret is { CmdCode: CmdCode.Succeed })
                {
                    foreach (var info in ret.Friends)
                    {
                        Friends.Add(new FriendData
                        {
                            RoleId = info.RoleId,
                            Nickname = info.Nickname,
                            IsOnline = info.IsOnline,
                            IsRequest = false
                        });
                    }

                    foreach (var info in ret.Applies)
                    {
                        Applies.Add(new FriendData
                        {
                            RoleId = info.RoleId,
                            Nickname = info.Nickname,
                            IsOnline = info.IsOnline,
                            IsRequest = true
                        });
                    }
                }

                onRefreshed?.Invoke();
            });
        }

        /// <summary>发起好友申请</summary>
        public void RequestAddFriend(int targetRoleId)
        {
            AppContext.Proto.RequestAddFriend(AppContext.Session.RoleId, targetRoleId, ret =>
            {
                bool ok = ret is { CmdCode: CmdCode.Succeed };
                string tip = ok ? "好友申请已发送" : (ret?.Tip ?? "发送失败");
                AppContext.Ui.ShowTip(tip);
            });
        }

        /// <summary>
        /// 处理好友申请（同意 / 拒绝）。
        /// 只把结果通过回调交回面板，是否重拉列表由面板决定（申请处理掉后需要重新对齐）。
        /// </summary>
        public void RequestHandleFriend(int fromRoleId, bool accept, Action<bool, string> callback)
        {
            AppContext.Proto.RequestHandleFriend(AppContext.Session.RoleId, fromRoleId, accept, ret =>
            {
                bool ok = ret is { CmdCode: CmdCode.Succeed };
                string tip = ok ? (accept ? "已添加好友" : "已拒绝") : (ret?.Tip ?? "操作失败");
                callback?.Invoke(ok, tip);
            });
        }

        #endregion

        #region 私聊

        /// <summary>打开与某好友的聊天：记录当前对象并拉取最近 100 条历史，完成后回调</summary>
        public void OpenChat(int friendRoleId, Action onLoaded = null)
        {
            CurrentChatRoleId = friendRoleId;
            AppContext.Proto.RequestFriendHistory(friendRoleId, ret =>
            {
                if (ret is { CmdCode: CmdCode.Succeed })
                {
                    List<FriendMsgData> list = GetOrCreateCache(friendRoleId);
                    list.Clear();
                    foreach (var msg in ret.Messages) list.Add(ToData(msg));
                }

                onLoaded?.Invoke();
            });
        }

        /// <summary>关闭聊天窗口</summary>
        public void CloseChat()
        {
            CurrentChatRoleId = 0;
        }

        /// <summary>发送私聊消息；成功后服务端回包自带真实 id 与时间，本地据此入缓存并通知面板</summary>
        public void SendMessage(int friendRoleId, string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            if (friendRoleId <= 0) return;

            AppContext.Proto.RequestSendFriendMsg(friendRoleId, content, ret =>
            {
                if (ret == null || ret.CmdCode != CmdCode.Succeed || ret.Message == null) return;

                FriendMsgData data = ToData(ret.Message);
                List<FriendMsgData> list = GetOrCreateCache(friendRoleId);

                // 去重：实时推送可能先到
                if (list.Exists(m => m.Id == data.Id)) return;

                list.Add(data);
                AppContext.Ui.GetPanel<FriendPanel>()?.OnMessageReceived(data);
            });
        }

        // 收到对方发来的私聊消息
        private void OnServerPushMessage(FriendMsgPushNtf friendMsgPushNtf)
        {
            if (friendMsgPushNtf.Message == null) return;

            FriendMsgData data = ToData(friendMsgPushNtf.Message);
            int peerId = data.SenderRoleId == AppContext.Session.RoleId ? data.ReceiverRoleId : data.SenderRoleId;
            List<FriendMsgData> list = GetOrCreateCache(peerId);
            if (!list.Exists(m => m.Id == data.Id)) list.Add(data);

            AppContext.Ui.GetPanel<FriendPanel>()?.OnMessageReceived(data);
        }

        /// <summary>获取与某好友的聊天缓存（按时间升序，可能为空列表）</summary>
        public List<FriendMsgData> GetChatCache(int friendRoleId)
        {
            return _chatCache.TryGetValue(friendRoleId, out var list) ? list : new List<FriendMsgData>();
        }

        #endregion

        public void Clear()
        {
            Friends.Clear();
            Applies.Clear();
            _chatCache.Clear();
            CurrentChatRoleId = 0;
        }

        private List<FriendMsgData> GetOrCreateCache(int friendRoleId)
        {
            if (!_chatCache.TryGetValue(friendRoleId, out var list))
            {
                list = new List<FriendMsgData>();
                _chatCache[friendRoleId] = list;
            }

            return list;
        }

        private FriendMsgData ToData(FriendMsg msg)
        {
            return new FriendMsgData
            {
                Id = msg.Id,
                SenderRoleId = msg.SenderRoleId,
                ReceiverRoleId = msg.ReceiverRoleId,
                Content = msg.Content,
                SendTime = msg.SendTime,
                IsSelf = msg.SenderRoleId == AppContext.Session.RoleId
            };
        }
    }
}