using HotUpdate.Data;

namespace HotUpdate.Player
{
    // 玩家跨子系统共享状态容器（纯数据、无行为、无 Unity 生命周期）。
    public class PlayerCore
    {
        // 输入/开面板锁
        public bool IsLock { get; set; }

        // 是否本地玩家
        public bool IsLocalPlayer { get; set; } = true;

        public float Health { get; set; } = 200f;
        public float MaxHealth { get; set; } = 200f;

        // 服务器下发的角色属性
        public PlayerValueData PlayerValueData { get; set; } = new();

        public int PlayerDataId => (int)PlayerValueData.ID;
    }
}
