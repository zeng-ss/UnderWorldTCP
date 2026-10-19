using System;
using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Service
{
    // 房间服务：房间列表 / 创建 / 加入 / 离开 / 准备 / 开始 / 房间内聊天。
    //
    // 房间只存在于 LoginServer 内存里，服务端是唯一权威；本类只做「请求 + 本地缓存」。
    public class RoomService
    {
        // 单个房间聊天记录上限
        private const int MaxChatCache = 100;

        // 当前所有房间
        public List<RoomItemData> Rooms { get; } = new();

        // 当前所在房间的成员列表
        public List<RoomPlayerData> Members { get; } = new();

        // 当前房间的聊天记录（按时间升序）
        public List<RoomChatData> ChatMessages { get; } = new();

        // 当前房间 id，0 表示不在任何房间
        public int CurrentRoomId { get; private set; }

        // 自己是否房主
        public bool IsMaster { get; private set; }

        public bool IsInRoom => CurrentRoomId > 0;

        /// <summary>拉取房间列表，完成后回调</summary>
        public void RequestRoomList(Action onRefreshed = null)
        {
            AppContext.Proto.RequestRoomList(ret =>
            {
                Rooms.Clear();

                if (ret is { CmdCode: CmdCode.Succeed })
                {
                    foreach (var r in ret.Rooms)
                    {
                        Rooms.Add(new RoomItemData
                        {
                            RoomId = r.RoomId,
                            RoomName = r.RoomName,
                            MasterRoleId = r.MasterRoleId,
                            MasterNickname = r.MasterNickname,
                            PlayerCount = r.PlayerCount,
                            MaxPlayer = r.MaxPlayer,
                            State = r.State
                        });
                    }
                }

                onRefreshed?.Invoke();
            });
        }

        #region 创建 / 加入 / 离开

        /// <summary>创建房间（成功后即为房主）</summary>
        public void CreateRoom(string roomName, Action<bool, string> callback)
        {
            AppContext.Proto.RequestCreateRoom(AppContext.Session.RoleId, roomName, AppContext.Session.PlayerName,
                ret =>
                {
                    bool ok = ret is { CmdCode: CmdCode.Succeed };
                    string tip = ok ? "" : "创建房间失败";

                    if (ok)
                    {
                        EnterRoom(ret.RoomId, ret.Players);
                        IsMaster = true;
                    }

                    callback?.Invoke(ok, tip);
                });
        }

        /// <summary>加入已存在的房间</summary>
        public void JoinRoom(int roomId, Action<bool, string> callback)
        {
            AppContext.Proto.RequestJoinRoom(AppContext.Session.RoleId, roomId, AppContext.Session.PlayerName,
                ret =>
                {
                    bool ok = ret is { CmdCode: CmdCode.Succeed };
                    string tip = ok ? "" : "加入失败：房间不存在、人数已满或已开始游戏";

                    if (ok)
                    {
                        EnterRoom(ret.RoomId, ret.Players);
                        RefreshMasterFlag();
                    }

                    callback?.Invoke(ok, tip);
                });
        }

        /// <summary>离开当前房间（房主离开会把房主移交给下一位）</summary>
        public void LeaveRoom(Action onDone = null)
        {
            AppContext.Proto.RequestLeaveRoom(AppContext.Session.RoleId, ret =>
            {
                if (ret != null && ret.CmdCode != CmdCode.Succeed && !string.IsNullOrEmpty(ret.Tip))
                    AppContext.Ui.ShowTip(ret.Tip);

                ClearRoomState();
                onDone?.Invoke();
            });
        }

        /// <summary>房主踢人（服务端校验房主身份；被踢者会收到 RoomKickedNtf 自行退回列表）</summary>
        public void RequestKickPlayer(int targetRoleId, Action<bool, string> callback)
        {
            if (!IsMaster)
            {
                callback?.Invoke(false, "只有房主才能踢人");
                return;
            }

            AppContext.Proto.RequestKickPlayer(targetRoleId, ret =>
            {
                bool ok = ret != null && ret.CmdCode == CmdCode.Succeed;
                string tip = ok ? "" : (ret?.Tip ?? "踢人失败");
                callback?.Invoke(ok, tip);
            });
        }

        /// <summary>自己被踢出房间：清掉本地房间数据（房间列表缓存保留）</summary>
        public void ApplyKicked()
        {
            ClearRoomState();
        }

        #endregion

        #region 准备 / 开始 / 聊天

        /// <summary>切换自己的准备状态（服务端会广播 RoomInfoNtf 回来，据此刷新）</summary>
        public void RequestReady(bool ready)
        {
            if (!IsInRoom) return;
            AppContext.Proto.RequestPlayerReady(AppContext.Session.RoleId, ready);
        }

        /// <summary>房主开始游戏（服务端校验后广播 RoomStartGameNtf）</summary>
        public void RequestStartGame()
        {
            if (!IsInRoom) return;
            AppContext.Proto.RequestRoomStartGame(AppContext.Session.RoleId, OnRoomStartGameHandle);
        }

        private void OnRoomStartGameHandle(RoomStartGameNtf ret)
        {
            AppContext.Proto.RequestChangeScene("GameScene", result =>
            {
                if (result.CmdCode != CmdCode.Succeed)
                {
                    Debug.LogError($"跳转场景失败：{result.CmdCode}");
                    return;
                }

                AppContext.Scene.LoadScene("GameScene", 1f, OnCoveredHandle);
            });
        }

        private void OnCoveredHandle()
        {
            AppContext.Ui.ClosePanel<RoomPanel>();
            AppContext.Ui.OpenPanel<PlayerProPanel>();
        }

        /// <summary>发送房间聊天（服务端补昵称 / 时间后广播回来，本地不预填）</summary>
        public void SendChat(string content, Action<RoomChatNtf> callback)
        {
            if (!IsInRoom || string.IsNullOrEmpty(content)) return;
            AppContext.Proto.RequestRoomChat(AppContext.Session.RoleId, content, callback);
        }

        #endregion

        #region 服务端推送落地（由 RoomPanel 转调）

        /// <summary>房间成员 / 准备状态变化（服务端广播）</summary>
        public void ApplyRoomInfo(RoomInfoNtf ntf)
        {
            if (ntf == null) return;
            CurrentRoomId = ntf.RoomId;
            Members.Clear();

            foreach (var p in ntf.Players)
            {
                Members.Add(new RoomPlayerData
                {
                    RoleId = p.RoleId,
                    Nickname = p.Nickname,
                    Avatar = p.Avatar,
                    IsMaster = p.IsMaster,
                    IsReady = p.IsReady
                });
            }

            RefreshMasterFlag();
        }

        /// <summary>收到房间内一条聊天广播</summary>
        public void ApplyRoomChat(RoomChatMsg msg)
        {
            if (msg == null) return;

            ChatMessages.Add(new RoomChatData
            {
                SenderRoleId = msg.SenderRoleId,
                SenderNickname = msg.SenderNickname,
                SenderAvatar = msg.SenderAvatar,
                Content = msg.Content,
                SendTime = msg.SendTime,
                IsSelf = msg.SenderRoleId == AppContext.Session.RoleId
            });

            while (ChatMessages.Count > MaxChatCache) ChatMessages.RemoveAt(0);
        }

        #endregion

        public void Clear() => ClearRoomState();

        // 进入房间后重置本地缓存
        private void EnterRoom(int roomId, IList<RoomPlayerInfo> players)
        {
            ClearRoomState();
            CurrentRoomId = roomId;

            foreach (var p in players)
            {
                Members.Add(new RoomPlayerData
                {
                    RoleId = p.RoleId,
                    Nickname = p.Nickname,
                    Avatar = p.Avatar,
                    IsMaster = p.IsMaster,
                    IsReady = p.IsReady
                });
            }
        }

        // 离开房间 / 清空：房间数据全丢，但房间列表缓存保留（返回列表时无需重拉也能先显示旧数据）
        private void ClearRoomState()
        {
            CurrentRoomId = 0;
            IsMaster = false;
            Members.Clear();
            ChatMessages.Clear();
        }

        private void RefreshMasterFlag()
        {
            IsMaster = Members.Exists(m => m.RoleId == AppContext.Session.RoleId && m.IsMaster);
        }
    }
}