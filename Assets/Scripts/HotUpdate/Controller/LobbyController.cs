using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [Header("房间")]
    private TMP_InputField roomNameInput;
    private TMP_InputField roomIdInput;
    private Button createRoomBtn;
    private Button joinRoomBtn;
    private Button leaveRoomBtn;
    private TMP_Text roomTitleTxt;
    private GameObject panel;

    [Header("玩家列表")]
    private Transform playerListRoot;
    private GameObject playerItemPrefab;

    [Header("准备/开始")]
    private Button startBtn;
    private Toggle readyToggle;
    private TMP_Text readyTipText;

    private Dictionary<ulong, PlayerRoomItem> playerRoomItemDict = new();
    private Dictionary<ulong, PlayerConfig> playerConfigDict = new();
    private Dictionary<ulong, RoomPlayerInfo> roomPlayerInfoDict = new();

    private int _currentRoomId;
    private bool _isMaster;

    private void Start()
    {
        FindUIReferences();

        // 按钮事件
        createRoomBtn.onClick.AddListener(OnCreateRoom);
        joinRoomBtn.onClick.AddListener(OnJoinRoom);
        leaveRoomBtn.onClick.AddListener(OnLeaveRoom);
        startBtn.onClick.AddListener(OnStartGame);
        readyToggle.onValueChanged.AddListener(OnToggleReady);

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
        playerConfigDict.TryAdd(playerConfig.ID, playerConfig);

        startBtn.gameObject.SetActive(false);
        readyTipText.gameObject.SetActive(true);
        leaveRoomBtn.gameObject.SetActive(false);

        Debug.Log($"LobbyController: 玩家 {playerName} 进入大厅");
    }

    private void FindUIReferences()
    {
        panel = GameObject.Find("Panel");
        roomNameInput = GameObject.Find("RoomName")?.GetComponent<TMP_InputField>();
        roomIdInput = GameObject.Find("roomId")?.GetComponent<TMP_InputField>();
        createRoomBtn = GameObject.Find("creatBtn")?.GetComponent<Button>();
        joinRoomBtn = GameObject.Find("joinBtn")?.GetComponent<Button>();
        leaveRoomBtn = GameObject.Find("LevelBtn")?.GetComponent<Button>();
        roomTitleTxt = GameObject.Find("title")?.GetComponent<TMP_Text>();

        playerListRoot = GameObject.Find("RoomItemContent")?.transform;
        startBtn = GameObject.Find("StartBtn")?.GetComponent<Button>();
        readyToggle = GameObject.Find("Toggle")?.GetComponent<Toggle>();
        readyTipText = GameObject.Find("ReadyTipText")?.GetComponent<TMP_Text>();
    }

    #region 房间管理

    private void OnCreateRoom()
    {
        string roomName = roomNameInput.text.Trim();
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
                    panel.SetActive(false);
                }
            });
    }

    private void OnJoinRoom()
    {
        if (!int.TryParse(roomIdInput.text.Trim(), out int roomId)) return;

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
                    panel.SetActive(false);
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
        roomTitleTxt.text = "";
        roomPlayerInfoDict.Clear();

        ClearPlayerList();

        panel.SetActive(true);
        createRoomBtn.gameObject.SetActive(true);
        joinRoomBtn.gameObject.SetActive(true);
        leaveRoomBtn.gameObject.SetActive(false);
        startBtn.gameObject.SetActive(false);
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
        if (!roomTitleTxt) return;

        roomTitleTxt.text = $"房间 {_currentRoomId} ({players.Count}人)";
        leaveRoomBtn.gameObject.SetActive(true);
        roomPlayerInfoDict.Clear();

        bool allReady = true;
        foreach (var p in players)
        {
            roomPlayerInfoDict[(ulong)p.RoleId] = p;
            if (!p.IsMaster && !p.IsReady) allReady = false;
        }

        // 房主且所有非房主玩家都已准备时才显示开始按钮
        startBtn.gameObject.SetActive(_isMaster && allReady);

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
        AppContext.Res.LoadAndInstantiateAsync("Assets/Res/UI/UIItem/PlayerRoomItem", playerListRoot, obj =>
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

            if (playerRoomItemDict.TryAdd(id, item))
            {
                // 如果是房主，显示标记
                //if (isMaster && item.isReadyText) item.isReadyText.text = "(房主)";
            }
            else Destroy(obj);
        });
    }

    private void ClearPlayerList()
    {
        foreach (var kv in playerRoomItemDict.Where(kv => kv.Value))
        {
            Destroy(kv.Value.gameObject);
        }

        playerRoomItemDict.Clear();
    }

    #endregion

    #region 准备 / 开始

    private void OnToggleReady(bool isOn)
    {
        readyTipText.gameObject.SetActive(!isOn);
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

        if (createRoomBtn) createRoomBtn.onClick.RemoveAllListeners();
        if (joinRoomBtn) joinRoomBtn.onClick.RemoveAllListeners();
        if (leaveRoomBtn) leaveRoomBtn.onClick.RemoveAllListeners();
        if (startBtn) startBtn.onClick.RemoveAllListeners();
        if (readyToggle) readyToggle.onValueChanged.RemoveAllListeners();

        playerConfigDict.Clear();
        playerRoomItemDict.Clear();
        roomPlayerInfoDict.Clear();
    }
}
