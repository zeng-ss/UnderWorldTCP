using System;
using System.Collections.Generic;

/// <summary>
/// 显式声明面板预制体在 Addressables 中的地址。
///
/// 地址即资源名（如 ChatPanel / DepotPanel），由 Addressables 分组配置。
/// 面板改名或地址不一致会静默失效，所以显式标注 + 按类名回退。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PanelPathAttribute : Attribute
{
    public string Path { get; }

    public PanelPathAttribute(string path)
    {
        Path = path;
    }
}

/// <summary>面板地址解析器：优先读 <see cref="PanelPathAttribute"/>，没标注则回退类名</summary>
public static class PanelPathResolver
{
    // 反射有开销，解析结果按类型缓存
    private static readonly Dictionary<Type, string> Cache = new Dictionary<Type, string>();

    public static string Resolve<T>() where T : BasePanel
    {
        return Resolve(typeof(T));
    }

    public static string Resolve(Type panelType)
    {
        if (Cache.TryGetValue(panelType, out var cached)) return cached;

        var attribute = (PanelPathAttribute)Attribute.GetCustomAttribute(panelType, typeof(PanelPathAttribute));
        string path = attribute != null ? attribute.Path : panelType.Name;
        Cache[panelType] = path;
        return path;
    }
}