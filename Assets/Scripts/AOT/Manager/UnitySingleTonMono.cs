using UnityEngine;

public class UnitySingleTonMono<T> : MonoBehaviour where T:MonoBehaviour  //限制T的类型必须是MonoBehaviour的派生类  
{
    private static T instance;//来存储当前的单例 

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                //new 一个单例对象  
                GameObject obj=new GameObject();
                obj.name = typeof(T).Name;
                instance=(T)obj.AddComponent<T>();
                
            }
            return instance;
        }
    }
    
    public virtual void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (instance == null)
        {
            instance = this as T;
            name = typeof(T).Name;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}