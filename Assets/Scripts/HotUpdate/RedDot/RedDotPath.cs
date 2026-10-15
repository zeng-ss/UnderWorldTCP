namespace HotUpdate.RedDot
{
    /// <summary>
    /// 红点树的所有节点路径（全项目唯一一份）。
    ///
    /// 层级是一棵树：父节点的红点数 = 自己 + 所有子节点的合计，
    /// 所以「好友」这种聚合入口不用自己维护数值，往子节点里塞就自动亮。
    ///
    ///   Main                       HUD 底部那排面板入口的公共父节点
    ///   ├── Chat                   聊天：有未读聊天消息
    ///   ├── Depot                  背包：（暂无可触发来源，先占位）
    ///   ├── PlayerData             角色：（暂无可触发来源，先占位）
    ///   ├── Friend                 好友： = 下面两个的合计
    ///   │   ├── Apply              待处理的好友申请数
    ///   │   └── Message            未读私聊消息数
    ///   └── Task                   任务：有奖可领的任务数
    /// </summary>
    public static class RedDotPath
    {
        // HUD 上一排面板入口的公共父节点
        public const string Main = "Main";

        public const string Chat = Main + "/Chat";
        public const string Depot = Main + "/Depot";
        public const string PlayerData = Main + "/PlayerData";
        public const string Friend = Main + "/Friend";
        public const string Task = Main + "/Task";

        // 好友下面的细分来源：HUD 上不直接显示，只用来聚合出 Friend 的红点
        public const string FriendApply = Friend + "/Apply";
        public const string FriendMessage = Friend + "/Message";
    }
}