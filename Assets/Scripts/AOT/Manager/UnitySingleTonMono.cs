using UnityEngine;

public class UnitySingleTonMono<T> : MonoBehaviour where T : MonoBehaviour //限制T的类型必须是MonoBehaviour的派生类  
{
    private static T _instance; //来存储当前的单例 

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                //new 一个单例对象  
                GameObject obj = new GameObject();
                obj.name = typeof(T).Name;
                _instance = (T)obj.AddComponent<T>();
            }

            return _instance;
        }
    }

    public virtual void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (_instance == null)
        {
            _instance = this as T;
            name = typeof(T).Name;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}