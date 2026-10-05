using System;
using System.Collections.Generic;

/// <summary>
/// 显式声明面板预制体所在的资源路径。
///
/// 原先 UIManager 里是 "Assets/Res/UI/UIPanel/" + typeof(T).Name 硬拼字符串，
/// 面板一旦挪位置或者命名不一致就会静默失效。现在改成标注 + 约定回退。
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

/// <summary>面板路径解析器：优先读 <see cref="PanelPathAttribute"/>，没标注则回退约定路径</summary>
public static class PanelPathResolver
{
    private const string DefaultRoot = "Assets/Res/UI/UIPanel/";

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
        string path = attribute != null ? attribute.Path : DefaultRoot + panelType.Name;
        Cache[panelType] = path;
        return path;
    }
}
