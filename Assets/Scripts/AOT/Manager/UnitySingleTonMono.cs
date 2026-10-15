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

    /// <summary>
    /// 自己是不是「多余的那个」（场景里手摆了一份、而单例已经存在的那种）。
    /// 派生类的 Awake 必须判断它并立刻 return —— base.Awake 里的 DestroyImmediate 不会中断当前方法，
    /// 不 return 的话会继续往下执行初始化，产生第二套运行时（多 Canvas / 多对象池根节点）。
    /// </summary>
    protected bool IsDuplicateInstance { get; private set; }

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
            IsDuplicateInstance = true;
            DestroyImmediate(gameObject);
        }
    }
}