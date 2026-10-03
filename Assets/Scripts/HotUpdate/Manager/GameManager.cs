using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainSaveData
{
    public string playerName;
    public string playerAccountData;
    public int dialogueIdData;
    public long saveRealTicks;
    public List<TaskDataRuntime> saveTasksData = new();
    public List<DriverDiskDataRuntime> haveDepotsData = new();
    public Dictionary<int, int> materialNumsData = new();
}

public class PlayerConfig
{
    public ulong id;
    public string name;
    public bool isReady;
    public string headImageName;
}

public class MessageData
{
    public ulong senderClientId;
    public string name;
    public string message;
    public string sendTime;
}

public class PlayerData
{
    public ulong id;
    public float maxHealthValue;
    public float attackValue = 200;
    public float defenseValue = 10;
    public float baoJiValue = 10;
    public float exAttackValue = 500;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region 数据

    public string curPlayerName;
    public PlayerData playerBaseData;
    public int accountId;
    public int roleId;
    public int serverId;
    public MainRoleInfo mainRoleInfo;
    public DepotConfig depotConfig;
    [Header("当前解锁的任务列表")] public TaskDataConfigSO taskConfigSO;
    [SerializeField] private MaterialDataSO materialData;

    public Dictionary<int, MaterialDataRuntime> materialDataRuntime = new();
    [HideInInspector] public List<DriverDiskDataRuntime> haveDepotList = new();
    public Dictionary<int, int> materialNumDict = new() { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
    [HideInInspector] public List<TaskDataRuntime> curTasksData = new();
    [HideInInspector] public int dialogueId;
    private MainSaveData mainSaveData;

    #endregion

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance == null)
        {
            Instance = this;
            gameObject.name = "GameManager";
        }
        else
        {
            DestroyImmediate(gameObject);
        }

        foreach (var item in taskConfigSO.taskDataList)
        {
            curTasksData.Add(new TaskDataRuntime(item));
        }
    }

    public void Start()
    {
        foreach (var runtime in materialData.materials)
        {
            materialDataRuntime.Add(runtime.id, new MaterialDataRuntime(runtime));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.Instance.OpenPanel<ExitPanel>();
            if (SceneManager.GetActiveScene().name == "GameScene")
            {
                EventCenter.Instance.EventTrigger(GameEvent.光标出现);
            }
        }
    }

    #region 工具方法

    public bool IsPointerOverSpecificUILayer(LayerMask uiLayerMask)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            if (result.gameObject != null && ((1 << result.gameObject.layer) & uiLayerMask) != 0) return true;
        }

        return false;
    }

    #endregion

    #region 消息

    public ChatPanel chatPanel;

    public void SendMes(string message, string sendTime)
    {
        if (chatPanel == null) chatPanel = FindObjectOfType<ChatPanel>();
        var messageData = new MessageData
        {
            senderClientId = 0,
            name = curPlayerName,
            message = message,
            sendTime = sendTime
        };
        chatPanel?.AddChatItem(messageData, true);
    }

    #endregion

    #region 存档

    /*[HideInInspector] public string currentArchivePath;

    public void LoadFileData()
    {
        string parentPath = Application.persistentDataPath + "/Archive/EveryGame";
        if (!Directory.Exists(parentPath)) return;
        string[] allPath = Directory.GetDirectories(parentPath);
        foreach (var path in allPath)
        {
            string fullJsonPath = Path.Combine(path, "main.json");
            MainSaveData data = JsonMgr.Instance.LoadData<MainSaveData>("main", path);
            if (data.playerAccountData == curPlayerAccount)
            {
                Debug.Log(path);
                UIManager.Instance.OpenPanel<StartPanel>(panel => { panel.nameText.text = $"欢迎你：{data.playerName}"; });
                mainSaveData = data;
                currentArchivePath = fullJsonPath;
                PlayerDataInit();
                break;
            }
        }

        if (mainSaveData == null)
        {
            UIManager.Instance.OpenPanel<StartPanel>(panel => { panel.inputPanel.SetActive(true); });
        }
    }

    public void SaveArchive()
    {
        mainSaveData = new MainSaveData
        {
            playerName = curPlayerName,
            playerAccountData = curPlayerAccount,
            dialogueIdData = dialogueId,
            saveTasksData = new List<TaskDataRuntime>(curTasksData),
            haveDepotsData = new List<DepotDataRuntime>(haveDepotList),
            materialNumsData = new Dictionary<int, int>(materialNumDict),
            saveRealTicks = DateTimeOffset.Now.ToUnixTimeSeconds()
        };
        if (currentArchivePath != null && File.Exists(currentArchivePath))
        {
            string json = JsonConvert.SerializeObject(mainSaveData, Formatting.Indented,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            File.WriteAllText(currentArchivePath, json);
            Debug.Log("已覆盖存档：" + currentArchivePath);
            return;
        }

        JsonMgr.Instance.SaveData(mainSaveData, "main", "Archive/EveryGame/" + mainSaveData.playerName + "/");
    }*/

    #endregion

    public void OnApplicationQuit()
    {
        ProtoHandler.Instance.RequestLeaveRoom(roleId);
    }
}