/// <summary>
/// 普通 C# 单例基类（非 MonoBehaviour，不会在场景里生成 GameObject）。
/// 纯逻辑管理器继承即可：public class XxxMgr : SingleTon&lt;XxxMgr&gt;
/// </summary>
public class SingleTon<T> where T : class, new()
{
    private static T instance;
    private static readonly object Locker = new object();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (Locker)
                {
                    instance ??= new T();
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// 置空单例实例。热更 DLL 重载、或需要彻底重建管理器时调用。
    /// </summary>
    public static void Dispose()
    {
        lock (Locker)
        {
            instance = null;
        }
    }
}
