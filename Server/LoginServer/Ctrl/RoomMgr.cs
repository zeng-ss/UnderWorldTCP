using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

/// <summary>
/// 房间管理器（LoginServer内存管理）
/// </summary>
public class RoomMgr : Singleton<RoomMgr>
{
    private int _nextRoomId = 1;
    private Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
    private Dictionary<int, int> _playerRoom = new Dictionary<int, int>();
    // sessionId → roleId 映射，供断线清理使用
    private Dictionary<int, int> _sessionRole = new Dictionary<int, int>();

    public CreateRoomRet CreateRoom(int roleId, int sessionId, string nickname, string roomName)
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
            RoomName = roomName,
            MasterRoleId = roleId,
            State = RoomState.Waiting
        };
        room.Players.Add(new RoomPlayerData
        {
            RoleId = roleId,
            SessionId = sessionId,
            Nickname = nickname,
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

    public JoinRoomRet JoinRoom(int roleId, int sessionId, string nickname, int roomId)
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

        room.Players.Add(new RoomPlayerData
        {
            RoleId = roleId,
            SessionId = sessionId,
            Nickname = nickname,
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

    public void LeaveRoom(int roleId)
    {
        if (!_playerRoom.TryGetValue(roleId, out int roomId)) return;
        if (!_rooms.TryGetValue(roomId, out Room room)) return;

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
            BroadcastRoomInfo(room);
        }
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

    private Room GetRoomByRoleId(int roleId)
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
    public bool IsMaster;
    public bool IsReady;

    public RoomPlayerInfo ToProto()
    {
        return new RoomPlayerInfo
        {
            RoleId = RoleId,
            Nickname = Nickname,
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
