/// <summary>
/// 头像编号表（服务端侧）。
/// 编号就是客户端 <c>HotUpdate/Data/AvatarConfig.cs</c> 里预设头像的下标，0 为默认头像。
/// 服务端只存编号、只校验范围，不认识贴图；两端数量必须一致（改预设头像时同步改 Count）。
/// </summary>
public static class AvatarCatalog
{
    /// <summary>预设头像数量（= 合法编号 0 ~ Count-1）</summary>
    public const int Count = 9;

    /// <summary>默认头像编号</summary>
    public const int Default = 0;

    public static bool IsValid(int avatar)
    {
        return avatar >= 0 && avatar < Count;
    }

    /// <summary>越界编号一律归到默认头像</summary>
    public static int Normalize(int avatar)
    {
        return IsValid(avatar) ? avatar : Default;
    }
}
