namespace HotUpdate.Service
{
    /// <summary>
    /// 会话 / 账号信息
    /// </summary>
    public class SessionService
    {
        public int AccountId { get; set; }

        public int RoleId { get; set; }

        public int ServerId { get; set; }

        public string PlayerName { get; set; }

        /// <summary>自己的头像编号（0=默认头像，对应 AvatarConfig 的下标）</summary>
        public int Avatar { get; set; }

        /// <summary>自己是不是当前房间的房主（敌人 Owner 模式的判定依据）</summary>
        public bool IsRoomMaster { get; set; }

        /// <summary>服务端下发的完整角色信息</summary>
        public MainRoleInfo MainRoleInfo { get; set; }

        public void Clear()
        {
            AccountId = 0;
            RoleId = 0;
            ServerId = 0;
            PlayerName = null;
            Avatar = 0;
            IsRoomMaster = false;
            MainRoleInfo = null;
        }
    }
}
