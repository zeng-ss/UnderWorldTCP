public class Singleton<T> where T : new()
{
    private static T instance;

    private static object instanceLock = new object();

    public static T Instance
    {

        get
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }
}