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

    /// <summary>仓库 / 强化 / 属性 这一组面板要在彼此之间传话，所以生命周期是全局的</summary>
    private readonly DepotController _depotUI;

    /// <summary>任务面板与任务奖励流程的协调者</summary>
    private readonly TaskController _taskUI;

    public static DepotController DepotUI => _current._depotUI;
    public static TaskController TaskUI => _current._taskUI;

    #endregion

    #region 基础设施

    private readonly EventMgr _events;
    private readonly UIAssetLoader _uiLoader;

    public static EventMgr Events => _current._events;
    public static UIAssetLoader UILoader => _current._uiLoader;

    #endregion

    private AppContext()
    {
        _current = this;
        _events = new EventMgr();
        _session = new SessionService();
        _material = new MaterialService();
        _depot = new DepotService();
        _playerData = new PlayerDataService();
        _task = new TaskService();
        _story = new StoryService();
        _chat = new ChatService();
        _uiLoader = new UIAssetLoader();
        _depotUI = new DepotController();
        _taskUI = new TaskController();
    }

    /// <summary>由 GameManager 在 Awake 里调用，整个进程只应执行一次</summary>
    public static void Create() => _current ??= new AppContext();

    /// <summary>释放所有跨面板协调者持有的监听（热更重载 / 退出时调用）</summary>
    public void Dispose()
    {
        _taskUI.Dispose();
        _chat.Clear();
        _events.Clear();
        _current = null;
    }
}