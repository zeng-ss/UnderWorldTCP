using UnityEngine;

/// <summary>
/// 应用上下文：整个客户端唯一的静态入口
/// </summary>
public class AppContext
{
    private static AppContext _current;

    #region Model

    private readonly SessionService _session;
    private readonly MaterialService _material;
    private readonly DepotService _depot;
    private readonly PlayerDataService _playerData;
    private readonly TaskService _task;
    private readonly StoryService _story;
    private readonly ChatService _chat;

    public static SessionService Session => _current._session;
    public static MaterialService Material => _current._material;
    public static DepotService Depot => _current._depot;
    public static PlayerDataService PlayerData => _current._playerData;
    public static TaskService Task => _current._task;
    public static StoryService Story => _current._story;
    public static ChatService Chat => _current._chat;

    #endregion

    #region Controller

    private readonly DepotController _depotUI;

    public static DepotController DepotUI => _current._depotUI;

    #endregion

    #region 基础设施 C#

    private readonly EventMgr _events;
    private readonly UIAssetLoader _uiLoader;
    private readonly ResMgr _resMgr;
    private readonly SceneMgr _sceneMgr;
    private readonly UIManager _uiManager;
    private readonly PoolMgr _pool;
    private readonly SoundManager _sound;
    private readonly ProtoHandler _proto;
    private readonly RemotePlayerManager _remotePlayer;

    public static EventMgr Events => _current._events;
    public static UIAssetLoader UILoader => _current._uiLoader;
    public static ResMgr Res => _current._resMgr;
    public static SceneMgr Scene => _current._sceneMgr;
    public static UIManager Ui => _current._uiManager;
    public static PoolMgr Pool => _current._pool;
    public static SoundManager Sound => _current._sound;
    public static RemotePlayerManager RemotePlayer => _current._remotePlayer;
    public static ProtoHandler Proto => _current._proto;

    #endregion

    #region 基础设施 Mono 管理器

    private DialogueManager _dialogue;

    public static DialogueManager Dialogue => Resolve(ref _current._dialogue);

    private static T Resolve<T>(ref T field) where T : MonoBehaviour
    {
        if (field != null) return field;

        field = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (field != null)
        {
            // 场景里已有的实例提升为跨场景存活（等价于它原本 DontDestroyOnLoad 的行为）
            Object.DontDestroyOnLoad(field.gameObject);
            return field;
        }

        var go = new GameObject(typeof(T).Name);
        Object.DontDestroyOnLoad(go);
        field = go.AddComponent<T>();
        return field;
    }

    #endregion

    private AppContext()
    {
        _current = this;

        _events = new EventMgr();
        _resMgr = new ResMgr();
        _uiLoader = new UIAssetLoader();
        _sceneMgr = new SceneMgr();
        _uiManager = new UIManager();
        _pool = new PoolMgr();
        _sound = new SoundManager();
        _proto = new ProtoHandler();
        _remotePlayer = new RemotePlayerManager();

        _session = new SessionService();
        _material = new MaterialService();
        _depot = new DepotService();
        _playerData = new PlayerDataService();
        _task = new TaskService();
        _story = new StoryService();
        _chat = new ChatService();
        _depotUI = new DepotController();
    }

    /// <summary>由 GameManager 在 Awake 里调用，整个进程只应执行一次</summary>
    public static AppContext Create() => _current ??= new AppContext();

    public static bool IsAlive => _current != null;

    /// <summary>释放所有跨面板协调者持有的监听（热更重载 / 退出时调用）</summary>
    public void Dispose()
    {
        _task.Clear();
        _chat.Clear();
        _events.Clear();
        _resMgr.ReleaseAll();
        _proto.Clear();
        _remotePlayer.Clear();
        _pool.Clear();
        _uiManager.ClearAllPanel();
        _uiLoader.ReleaseAll();
        _current = null;
    }
}