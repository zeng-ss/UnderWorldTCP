namespace HotUpdate.Data
{
    // 房间列表项。对应协议 RoomSummary，服务端从内存下发。
    public class RoomItemData
    {
        public int RoomId;
        public string RoomName;
        public int MasterRoleId;
        public string MasterNickname;
        public int PlayerCount;
        public int MaxPlayer;

        // 0=等待中 1=游戏中，对应服务端 RoomState
        public int State;

        public bool IsPlaying => State == 1;
    }

    // 房间内的一名玩家 对应协议 RoomPlayerInfo。
    public class RoomPlayerData
    {
        public int RoleId;
        public string Nickname;
        public bool IsMaster;
        public bool IsReady;
    }

    // 房间内的聊天消息。对应协议 RoomChatMsg。
    public class RoomChatData
    {
        public int SenderRoleId;
        public string SenderNickname;
        public string Content;
        public string SendTime;

        // 是否本地玩家发的
        public bool IsSelf;
    }
}