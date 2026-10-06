using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [Header("房间")]
    private TMP_InputField _roomNameInput;
    private TMP_InputField _roomIdInput;
    private Button _createRoomBtn;
    private Button _joinRoomBtn;
    private Button _leaveRoomBtn;
    private TMP_Text _roomTitleTxt;
    private GameObject _panel;

    [Header("玩家列表")]
    private Transform _playerListRoot;
    private GameObject _playerItemPrefab;

    [Header("准备/开始")]
    private Button _startBtn;
    private Toggle _readyToggle;
    private TMP_Text _readyTipText;

    private Dictionary<ulong, PlayerRoomItem> _playerRoomItemDict = new();
    private Dictionary<ulong, PlayerConfig> _playerConfigDict = new();
    private Dictionary<ulong, RoomPlayerInfo> _roomPlayerInfoDict = new();

    private int _currentRoomId;
    private bool _isMaster;

    private void Start()
    {
        FindUIReferences();

        // 按钮事件
        _createRoomBtn.onClick.AddListener(OnCreateRoom);
        _joinRoomBtn.onClick.AddListener(OnJoinRoom);
        _leaveRoomBtn.onClick.AddListener(OnLeaveRoom);
        _startBtn.onClick.AddListener(OnStartGame);
        _readyToggle.onValueChanged.AddListener(OnToggleReady);

        // 房间事件
        AppContext.Proto.OnRoomInfoChanged += OnRoomInfoChanged;
        AppContext.Proto.OnRoomStartGame += OnRoomStartGame;

        // 初始化本地玩家
        MainRoleInfo info = AppContext.Session.MainRoleInfo;
        string playerName = info != null ? info.BaseInfo.Nickname : AppContext.Session.PlayerName;

        PlayerConfig playerConfig = new PlayerConfig
        {
            ID = (ulong)AppContext.Session.RoleId,
            Name = playerName,
            IsReady = false,
            HeadImageName = "icon_question"
        };
        _playerConfigDict.TryAdd(playerConfig.ID, playerConfig);

        _startBtn.gameObject.SetActive(false);
        _readyTipText.gameObject.SetActive(true);
        _leaveRoomBtn.gameObject.SetActive(false);

        Debug.Log($"LobbyController: 玩家 {playerName} 进入大厅");
    }

    private void FindUIReferences()
    {
        _panel = GameObject.Find("Panel");
        _roomNameInput = GameObject.Find("RoomName")?.GetComponent<TMP_InputField>();
        _roomIdInput = GameObject.Find("roomId")?.GetComponent<TMP_InputField>();
        _createRoomBtn = GameObject.Find("creatBtn")?.GetComponent<Button>();
        _joinRoomBtn = GameObject.Find("joinBtn")?.GetComponent<Button>();
        _leaveRoomBtn = GameObject.Find("LevelBtn")?.GetComponent<Button>();
        _roomTitleTxt = GameObject.Find("title")?.GetComponent<TMP_Text>();

        _playerListRoot = GameObject.Find("RoomItemContent")?.transform;
        _startBtn = GameObject.Find("StartBtn")?.GetComponent<Button>();
        _readyToggle = GameObject.Find("Toggle")?.GetComponent<Toggle>();
        _readyTipText = GameObject.Find("ReadyTipText")?.GetComponent<TMP_Text>();
    }

    #region 房间管理

    private void OnCreateRoom()
    {
        string roomName = _roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName)) roomName = AppContext.Session.PlayerName + "的房间";

        AppContext.Proto.RequestCreateRoom(AppContext.Session.RoleId, roomName, AppContext.Session.PlayerName,
            ret =>
            {
                if (ret.CmdCode == CmdCode.Succeed)
                {
                    _currentRoomId = ret.RoomId;
                    _isMaster = true;
                    RefreshRoomUI(ret.Players);
                    // 打开聊天面板
                    AppContext.Ui.OpenPanel<ChatPanel>();
                    _panel.SetActive(false);
                }
            });
    }

    private void OnJoinRoom()
    {
        if (!int.TryParse(_roomIdInput.text.Trim(), out int roomId)) return;

        AppContext.Proto.RequestJoinRoom(
            AppContext.Session.RoleId, roomId,
            AppContext.Session.PlayerName,
            ret =>
            {
                if (ret.CmdCode == CmdCode.Succeed)
                {
                    _currentRoomId = ret.RoomId;
                    _isMaster = false;
                    RefreshRoomUI(ret.Players);
                    _panel.SetActive(false);
                    // 打开聊天面板
                    AppContext.Ui.OpenPanel<ChatPanel>();
                }
            });
    }

    private void OnLeaveRoom()
    {
        AppContext.Proto.RequestLeaveRoom(AppContext.Session.RoleId);
        _currentRoomId = 0;
        _isMaster = false;
        _roomTitleTxt.text = "";
        _roomPlayerInfoDict.Clear();

        ClearPlayerList();

        _panel.SetActive(true);
        _createRoomBtn.gameObject.SetActive(true);
        _joinRoomBtn.gameObject.SetActive(true);
        _leaveRoomBtn.gameObject.SetActive(false);
        _startBtn.gameObject.SetActive(false);
    }

    #endregion

    #region 房间事件回调

    private void OnRoomInfoChanged(RoomInfoNtf ntf)
    {
        _currentRoomId = ntf.RoomId;
        _isMaster = false;
        foreach (var p in ntf.Players)
        {
            if (p.RoleId == AppContext.Session.RoleId && p.IsMaster) _isMaster = true;
        }
        RefreshRoomUI(ntf.Players);
    }

    private void OnRoomStartGame(RoomStartGameNtf ntf)
    {
        AppContext.Ui.ClosePanel<LoadPanel>();
        Debug.Log("[LobbyController] 房主开始游戏，加载 GameScene");
        AppContext.Scene.LoadScene("GameScene");
    }

    private void RefreshRoomUI(IList<RoomPlayerInfo> players)
    {
        if (!_roomTitleTxt) return;

        _roomTitleTxt.text = $"房间 {_currentRoomId} ({players.Count}人)";
        _leaveRoomBtn.gameObject.SetActive(true);
        _roomPlayerInfoDict.Clear();

        bool allReady = true;
        foreach (var p in players)
        {
            _roomPlayerInfoDict[(ulong)p.RoleId] = p;
            if (!p.IsMaster && !p.IsReady) allReady = false;
        }

        // 房主且所有非房主玩家都已准备时才显示开始按钮
        _startBtn.gameObject.SetActive(_isMaster && allReady);

        // 刷新本地玩家列表显示
        RefreshPlayerListDisplay(players);
    }

    private void RefreshPlayerListDisplay(IList<RoomPlayerInfo> players)
    {
        ClearPlayerList();

        // 以服务器广播的数据为准
        foreach (var p in players)
        {
            SpawnPlayerItem((ulong)p.RoleId, p.Nickname, p.IsReady, p.IsMaster);
        }
    }

    private void SpawnPlayerItem(ulong id, string name, bool isReady, bool isMaster)
    {
        AppContext.Res.LoadAndInstantiateAsync("PlayerRoomItem", _playerListRoot, obj =>
        {
            PlayerRoomItem item = obj.GetComponent<PlayerRoomItem>();
            if (item == null) { Destroy(obj); return; }

            var cfg = new PlayerConfig
            {
                ID = id,
                Name = name + (isMaster ? "(房主)" : ""),
                IsReady = isReady,
                HeadImageName = "icon_question"
            };
            item.Init(cfg);

            if (_playerRoomItemDict.TryAdd(id, item))
            {
                // 如果是房主，显示标记
                //if (isMaster && item.isReadyText) item.isReadyText.text = "(房主)";
            }
            else Destroy(obj);
        });
    }

    private void ClearPlayerList()
    {
        foreach (var kv in _playerRoomItemDict.Where(kv => kv.Value))
        {
            Destroy(kv.Value.gameObject);
        }

        _playerRoomItemDict.Clear();
    }

    #endregion

    #region 准备 / 开始

    private void OnToggleReady(bool isOn)
    {
        _readyTipText.gameObject.SetActive(!isOn);
        if (_currentRoomId <= 0) return;

        // 发送到服务器，服务端广播 RoomInfoNtf 后由 OnRoomInfoChanged 统一刷新 UI
        AppContext.Proto.RequestPlayerReady(AppContext.Session.RoleId, isOn);
    }

    private void OnStartGame()
    {
        if (!_isMaster || _currentRoomId <= 0) return;

        Debug.Log("[LobbyController] 房主开始游戏");
        AppContext.Proto.RequestRoomStartGame(AppContext.Session.RoleId);
    }

    #endregion

    private void OnDestroy()
    {
        AppContext.Proto.OnRoomInfoChanged -= OnRoomInfoChanged;
        AppContext.Proto.OnRoomStartGame -= OnRoomStartGame;

        if (_createRoomBtn) _createRoomBtn.onClick.RemoveAllListeners();
        if (_joinRoomBtn) _joinRoomBtn.onClick.RemoveAllListeners();
        if (_leaveRoomBtn) _leaveRoomBtn.onClick.RemoveAllListeners();
        if (_startBtn) _startBtn.onClick.RemoveAllListeners();
        if (_readyToggle) _readyToggle.onValueChanged.RemoveAllListeners();

        _playerConfigDict.Clear();
        _playerRoomItemDict.Clear();
        _roomPlayerInfoDict.Clear();
    }
}
