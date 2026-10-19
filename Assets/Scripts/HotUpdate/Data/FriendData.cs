namespace HotUpdate.Data
{
    /// <summary>
    /// 好友列表信息
    /// </summary>
    public class FriendData
    {
        public int RoleId;
        public string Nickname;
        public bool IsOnline;
        public int Avatar; // 头像编号（0=默认头像，对应 AvatarConfig 下标）
        // 该好友发给我、我还没读的私聊条数 服务端下发，打开聊天后归 0
        public int Unread;
        // 是否是一条「待处理的好友申请」；true 时列表项展示同意/拒绝按钮而非在线状态
        public bool IsRequest;
    }

    /// <summary>
    /// 好友私聊消息。
    /// </summary>
    public class FriendMsgData
    {
        public int Id;
        public int SenderRoleId;
        public int ReceiverRoleId;
        public string Content;
        public string SendTime;
        // 是否本地玩家发的（用于 UI 左右对齐）
        public bool IsSelf;
    }
}
