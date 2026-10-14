namespace HotUpdate.Data
{
    /// <summary>
    /// 好友信息（客户端运行时）。
    /// 服务端下发的是 proto FriendInfo，这里只做一层轻量封装供 UI / 服务层使用。
    /// </summary>
    public class FriendData
    {
        public int RoleId;
        public string Nickname;
        public bool IsOnline;

        // 是否是一条「待处理的好友申请」（别人加我）；true 时列表项展示同意/拒绝按钮而非在线状态
        public bool IsRequest;
    }

    /// <summary>
    /// 好友私聊消息（客户端运行时）。
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
