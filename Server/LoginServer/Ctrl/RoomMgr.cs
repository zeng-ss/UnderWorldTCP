using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>
/// 房间管理器（房间只存在 LoginServer 内存里，不落库；进程重启即清空）
/// </summary>
public class RoomMgr : Singleton<RoomMgr>
{
    // 单个房间的人数上限
    public const int MaxPlayer = 4;

    private int _nextRoomId = 1;
    private Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
    private Dictionary<int, int> _playerRoom = new Dictionary<int, int>();
    // sessionId → roleId 映射，供断线清理使用
    private Dictionary<int, int> _sessionRole = new Dictionary<int, int>();

    public CreateRoomRet CreateRoom(int roleId, int sessionId, string nickname, int avatar, string roomName)
    {
        if (_playerRoom.ContainsKey(roleId))
        {
            LogMsg.Info("[RoomMgr]已经创建好了房间！");
            return new CreateRoomRet { CmdCode = CmdCode.ServerError };
        }

        int roomId = _nextRoomId++;
        Room room = new Room
        {
            RoomId = roomId,
            RoomName = string.IsNullOrEmpty(roomName) ? (nickname + "的房间") : roomName,
            MasterRoleId = roleId,
            State = RoomState.Waiting
        };
        room.Players.Add(new RoomPlayerData
        {
            RoleId = roleId,
            SessionId = sessionId,
            Nickname = nickname,
            Avatar = avatar,
            IsMaster = true
        });
        _rooms[roomId] = room;
        _playerRoom[roleId] = roomId;
        _sessionRole[sessionId] = roleId;

        LogMsg.Info($"[RoomMgr]房间创建: roomId={roomId} name={roomName} master={roleId}");

        CreateRoomRet ret = new CreateRoomRet { RoomId = roomId };
        foreach (var p in room.Players)
            ret.Players.Add(p.ToProto());
        return ret;
    }

    public JoinRoomRet JoinRoom(int roleId, int sessionId, string nickname, int avatar, int roomId)
    {
        if (_playerRoom.ContainsKey(roleId))
        {
            LogMsg.Info($"[RoomMgr]玩家：{roleId}，sessionId：{sessionId} 已经在房间了", ConsoleColor.Red);
            return new JoinRoomRet { CmdCode = CmdCode.ServerError };
        }
        if (!_rooms.TryGetValue(roomId, out Room room))
        {
            LogMsg.Info($"[RoomMgr]玩家：{roleId} 加入的房间：{roleId} 不存在", ConsoleColor.Red);
            return new JoinRoomRet { CmdCode = CmdCode.ReqParamError };
        }
        if (room.State != RoomState.Waiting)
        {
            LogMsg.Info($"[RoomMgr]玩家：{roleId} 加入的房间：{roleId} 已经开始游戏了", ConsoleColor.Red);
            return new JoinRoomRet { CmdCode = CmdCode.ServerError };
        }
        if (room.Players.Count >= room.MaxPlayer)
        {
            LogMsg.Info($"[RoomMgr]玩家：{roleId} 加入的房间：{roomId} 人数已满", ConsoleColor.Red);
            return new JoinRoomRet { CmdCode = CmdCode.ServerError };
        }

        room.Players.Add(new RoomPlayerData
        {
            RoleId = roleId,
            SessionId = sessionId,
            Nickname = nickname,
            Avatar = avatar,
            IsMaster = false
        });
        _playerRoom[roleId] = roomId;
        _sessionRole[sessionId] = roleId;

        LogMsg.Info($"[RoomMgr]玩家加入房间: roleId={roleId} roomId={roomId} 人数={room.Players.Count}");

        BroadcastRoomInfo(room);

        JoinRoomRet ret = new JoinRoomRet { RoomId = roomId, CmdCode = CmdCode.Succeed };
        foreach (var p in room.Players) ret.Players.Add(p.ToProto());
        return ret;
    }

    /// <summary>
    /// 离开房间（房主离开会移交给下一位玩家；最后一人离开则解散房间）。
    /// 返回是否真的离开了某个房间。
    /// </summary>
    public bool LeaveRoom(int roleId)
    {
        if (!_playerRoom.TryGetValue(roleId, out int roomId)) return false;
        if (!_rooms.TryGetValue(roomId, out Room room)) return false;

        var player = room.Players.Find(p => p.RoleId == roleId);
        if (player != null) _sessionRole.Remove(player.SessionId);
        _playerRoom.Remove(roleId);
        room.Players.RemoveAll(p => p.RoleId == roleId);

        LogMsg.Info($"[RoomMgr]玩家离开房间: roleId={roleId} roomId={roomId} 剩余={room.Players.Count}");

        if (room.Players.Count == 0)
        {
            _rooms.Remove(roomId);
            LogMsg.Info($"[RoomMgr]房间解散: roomId={roomId}");
        }
        else
        {
            if (room.MasterRoleId == roleId)
            {
                room.MasterRoleId = room.Players[0].RoleId;
                room.Players[0].IsMaster = true;
            }
            // 新加入的玩家默认未准备，房主交接后也要重置，避免残留准备态
            BroadcastRoomInfo(room);
        }

        return true;
    }

    /// <summary>
    /// 通过sessionId离开房间（断线清理用）
    /// </summary>
    public void LeaveRoomBySession(int sessionId)
    {
        if (_sessionRole.TryGetValue(sessionId, out int roleId))
            LeaveRoom(roleId);
    }

    /// <summary>
    /// 房主踢人：校验房主身份 / 同房间 / 不能踢自己，
    /// 被踢者先收到 RoomKickedNtf（客户端据此退回房间列表页），再从房间里移除并广播 RoomInfoNtf。
    /// </summary>
    public CmdCode KickPlayer(int masterRoleId, int targetRoleId, out string tip)
    {
        tip = "";

        Room room = GetRoomByRoleId(masterRoleId);
        if (room == null)
        {
            tip = "你当前不在任何房间";
            return CmdCode.ReqParamError;
        }
        if (room.MasterRoleId != masterRoleId)
        {
            tip = "只有房主才能踢人";
            return CmdCode.ServerError;
        }
        if (targetRoleId == masterRoleId)
        {
            tip = "不能踢自己";
            return CmdCode.ReqParamError;
        }

        RoomPlayerData target = room.Players.Find(p => p.RoleId == targetRoleId);
        if (target == null)
        {
            tip = "该玩家不在你的房间里";
            return CmdCode.ReqParamError;
        }

        // 先通知被踢者（他需要退回房间列表页），再把他从房间里摘掉
        RoomKickedNtf ntf = new RoomKickedNtf { Tip = $"你已被房主「{GetMasterNickname(room)}」移出房间" };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_RoomKickedNtfCode, Data = ntf.ToByteString() };
        SessionMgr.Instance.GetSession(target.SessionId)?.SendData(pkg);

        LeaveRoom(targetRoleId); // 内部会向房间里剩下的人广播 RoomInfoNtf
        LogMsg.Info($"[RoomMgr]房主踢人: master={masterRoleId} target={targetRoleId} roomId={room.RoomId}");
        return CmdCode.Succeed;
    }

    private static string GetMasterNickname(Room room)
    {
        RoomPlayerData master = room.Players.Find(p => p.RoleId == room.MasterRoleId);
        return master != null ? master.Nickname : "未知";
    }

    /// <summary>
    /// 切换准备状态，广播给房间所有人
    /// </summary>
    public void SetPlayerReady(int roleId, bool isReady)
    {
        Room room = GetRoomByRoleId(roleId);
        if (room == null) return;

        var player = room.Players.Find(p => p.RoleId == roleId);
        if (player != null) player.IsReady = isReady;

        BroadcastRoomInfo(room);
        LogMsg.Info($"[RoomMgr]玩家准备: roleId={roleId} isReady={isReady}");
    }

    public CmdCode StartGame(int roleId)
    {
        if (!_playerRoom.TryGetValue(roleId, out int roomId))
            return CmdCode.ReqParamError;
        if (!_rooms.TryGetValue(roomId, out Room room))
            return CmdCode.ReqParamError;
        if (room.MasterRoleId != roleId)
            return CmdCode.ServerError;

        foreach (var p in room.Players.Where(p => !p.IsMaster && !p.IsReady))
        {
            LogMsg.Info($"[RoomMgr]无法开始游戏，玩家未准备: roleId={p.RoleId}");
            return CmdCode.ServerError;
        }

        room.State = RoomState.Playing;

        RoomStartGameNtf ntf = new RoomStartGameNtf { RoomId = roomId, MapId = 1 };
        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_RoomStartGameCode, Data = ntf.ToByteString() };

        foreach (var p in room.Players)
        {
            SessionMgr.Instance.GetSession(p.SessionId)?.SendData(pkg);
        }

        LogMsg.Info($"[RoomMgr]房间开始游戏: roomId={roomId} 人数={room.Players.Count}");
        return CmdCode.Succeed;
    }

    /// <summary>
    /// 获取所有房间的摘要（房间列表面板用）。按房间 id 升序，方便客户端稳定展示。
    /// </summary>
    public List<RoomSummary> GetRoomList()
    {
        List<RoomSummary> list = new List<RoomSummary>();
        foreach (Room room in _rooms.Values.OrderBy(r => r.RoomId))
        {
            RoomPlayerData master = room.Players.Find(p => p.RoleId == room.MasterRoleId);
            list.Add(new RoomSummary
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                MasterRoleId = room.MasterRoleId,
                MasterNickname = master != null ? master.Nickname : "",
                PlayerCount = room.Players.Count,
                MaxPlayer = room.MaxPlayer,
                State = (int)room.State
            });
        }

        return list;
    }

    /// <summary>
    /// 房间内聊天：服务端补上发送者昵称与时间后广播给房间内所有人（含发送者自己）。
    /// 发送者不在任何房间则忽略。
    /// </summary>
    public bool BroadcastChat(int roleId, string content)
    {
        Room room = GetRoomByRoleId(roleId);
        if (room == null || string.IsNullOrEmpty(content)) return false;

        RoomPlayerData sender = room.Players.Find(p => p.RoleId == roleId);
        if (sender == null) return false;

        RoomChatNtf ntf = new RoomChatNtf
        {
            RoomId = room.RoomId,
            Message = new RoomChatMsg
            {
                SenderRoleId = roleId,
                SenderNickname = sender.Nickname,
                SenderAvatar = sender.Avatar,
                Content = content,
                SendTime = DateTime.Now.ToString("HH:mm:ss")
            }
        };

        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_RoomChatNtfCode, Data = ntf.ToByteString() };
        foreach (var p in room.Players)
            SessionMgr.Instance.GetSession(p.SessionId)?.SendData(pkg);

        return true;
    }

    public Room GetRoomByRoleId(int roleId)
    {
        if (_playerRoom.TryGetValue(roleId, out int roomId))
        {
            _rooms.TryGetValue(roomId, out Room room);
            return room;
        }
        return null;
    }
    /// <summary>
    /// 获取同一房间的所有 sessionId（用于广播）
    /// </summary>
    public List<int> GetSessionIdsInRoom(int roleId)
    {
        Room room = GetRoomByRoleId(roleId);
        List<int> ids = new List<int>();
        if (room == null) return ids;
        foreach (var p in room.Players)
            ids.Add(p.SessionId);
        return ids;
    }

    private void BroadcastRoomInfo(Room room)
    {
        RoomInfoNtf ntf = new RoomInfoNtf { RoomId = room.RoomId };
        foreach (var p in room.Players) ntf.Players.Add(p.ToProto());

        BasePackage pkg = new BasePackage { ProtoCode = NetDefine.CMD_RoomInfoCode, Data = ntf.ToByteString() };

        foreach (var p in room.Players)
        {
            SessionMgr.Instance.GetSession(p.SessionId)?.SendData(pkg); // 向 Unity Session 客户端发送信息
        }
    }
}

public class Room
{
    public int RoomId;
    public string RoomName;
    public int MasterRoleId;
    public RoomState State;
    public int MaxPlayer = RoomMgr.MaxPlayer;
    public List<RoomPlayerData> Players = new List<RoomPlayerData>();
}

/// <summary>
/// 服务端内部使用的玩家数据（含SessionId和Ready状态，不暴露给客户端）
/// </summary>
public class RoomPlayerData
{
    public int RoleId;
    public int SessionId;
    public string Nickname;
    public int Avatar;
    public bool IsMaster;
    public bool IsReady;

    public RoomPlayerInfo ToProto()
    {
        return new RoomPlayerInfo
        {
            RoleId = RoleId,
            Nickname = Nickname,
            Avatar = Avatar,
            IsMaster = IsMaster,
            IsReady = IsReady
        };
    }
}

public enum RoomState
{
    Waiting,
    Playing
}
