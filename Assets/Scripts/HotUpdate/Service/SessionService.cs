/// <summary>
/// 会话 / 账号信息
/// </summary>
public class SessionService
{
    public int AccountId { get; set; }

    public int RoleId { get; set; }

    public int ServerId { get; set; }

    public string PlayerName { get; set; }

    /// <summary>服务端下发的完整角色信息</summary>
    public MainRoleInfo MainRoleInfo { get; set; }

    public void Clear()
    {
        AccountId = 0;
        RoleId = 0;
        ServerId = 0;
        PlayerName = null;
        MainRoleInfo = null;
    }
}
