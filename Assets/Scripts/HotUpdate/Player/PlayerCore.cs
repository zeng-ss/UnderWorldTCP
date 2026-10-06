/// <summary>
/// 玩家跨子系统共享状态容器（纯数据、无行为、无 Unity 生命周期）。
/// </summary>
public class PlayerCore
{
    /// <summary>输入/开面板锁</summary>
    public bool IsLock { get; set; }

    /// <summary>是否本地玩家</summary>
    public bool IsLocalPlayer { get; set; } = true;

    public float Health { get; set; } = 200f;
    public float MaxHealth { get; set; } = 200f;

    /// <summary>服务器下发的角色属性</summary>
    public PlayerValueData PlayerValueData { get; set; } = new();

    public int PlayerDataId => (int)PlayerValueData.ID;
}