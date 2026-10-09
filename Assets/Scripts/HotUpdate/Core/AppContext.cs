using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Manager;
using HotUpdate.Network;
using HotUpdate.Service;
using UnityEngine;

namespace HotUpdate.Core
{
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
        }

        public static AppContext Create() => _current ??= new AppContext();

        public void InitAll(MaterialDataSo materialData, DepotConfig depotConfig, TaskDataConfigSo taskConfigSo,
            PlayerValueData basePlayerValueData)
        {
            _material.Init(materialData);
            _depot.Init(depotConfig);
            _task.Init(taskConfigSo);
            _playerData.Init(basePlayerValueData ?? new PlayerValueData());
            // 用当前装备先算一次，保证 Current 一开始就有值而不是全 0
            _playerData.ApplyEquipped(_depot.Equipped);
            _uiManager.Init();
            _sound.Init();
            _proto.Init();
            _story.Init();
            _remotePlayer.Init();
        }

        public static bool IsAlive => _current != null;

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
}