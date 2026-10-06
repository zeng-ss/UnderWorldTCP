using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件类型。一个枚举值对应一类事件，参数统一走 EventArgs 派生类。
/// </summary>
public enum GameEvent
{
    //加载进度变化
    LoadProgress,

    //战斗场景加载完成、正式开局，无参数
    GameStart,

    //一段对话结束
    DialogueEnd,

    //请求显示光标
    CursorShow,

    //请求隐藏光标
    CursorHide,

    //角色属性重算完成
    PlayerDataChanged,

    //拥有的驱动盘列表变化（获得/移除），监听方从 DepotService 重新读取
    DepotChanged,

    //已装备的驱动盘变化，监听方从 DepotService 重新读取
    EquippedChanged,

    //任务数据变化（解锁/进度/完成）
    TaskChanged,

    //材料数量变化
    MaterialNumChanged,

    //收到一条聊天消息（自己发的或别人发的）
    ChatMessageReceived,

    //登录服列表刷新 / 选中项变化
    ServerListChanged
}

/// <summary>
/// 事件参数基类。自定义参数继承它
/// </summary>
public class EventArgs
{
    // 该参数对应的事件枚举值，用于断言与日志排查
    public int EventCode { get; }

    // 无参事件复用的空参数实例，避免每次触发都 new 一个对象
    public static readonly EventArgs Empty = new(0);

    protected EventArgs(int eventCode)
    {
        EventCode = eventCode;
    }
}

/// <summary>加载进度事件参数</summary>
public class LoadProgressArgs : EventArgs
{
    public LoadProgressArgs(float progress) : base((int)GameEvent.LoadProgress)
    {
        Progress = progress;
    }

    public float Progress { get; }
}

/// <summary>对话结束事件参数</summary>
public class DialogueEndArgs : EventArgs
{
    public DialogueEndArgs(int dialogueId) : base((int)GameEvent.DialogueEnd)
    {
        DialogueId = dialogueId;
    }

    /// <summary>结束的对话 id</summary>
    public int DialogueId { get; }
}

/// <summary>角色属性重算完成事件参数</summary>
public class PlayerDataChangedArgs : EventArgs
{
    public PlayerDataChangedArgs(PlayerValueData playerValueData) : base((int)GameEvent.PlayerDataChanged)
    {
        PlayerValueData = playerValueData;
    }

    /// <summary>重算后的角色属性</summary>
    public PlayerValueData PlayerValueData { get; }
}

/// <summary>任务数据变化事件参数</summary>
public class TaskChangedArgs : EventArgs
{
    public TaskChangedArgs(IReadOnlyList<TaskDataRuntime> tasks, int changedTaskId = -1) : base(
        (int)GameEvent.TaskChanged)
    {
        Tasks = tasks;
        ChangedTaskId = changedTaskId;
    }

    /// <summary>变化后的全量任务列表</summary>
    public IReadOnlyList<TaskDataRuntime> Tasks { get; }

    /// <summary>发生变化的任务 id；-1 表示批量变化</summary>
    public int ChangedTaskId { get; }
}

/// <summary>材料数量变化事件参数</summary>
public class MaterialNumChangedArgs : EventArgs
{
    public MaterialNumChangedArgs(int materialId, int totalAmount) : base((int)GameEvent.MaterialNumChanged)
    {
        MaterialId = materialId;
        TotalAmount = totalAmount;
    }

    public int MaterialId { get; }
    public int TotalAmount { get; }
}

/// <summary>聊天消息事件参数</summary>
public class ChatMessageArgs : EventArgs
{
    public ChatMessageArgs(MessageData message) : base((int)GameEvent.ChatMessageReceived)
    {
        Message = message;
    }

    public MessageData Message { get; }
}

/// <summary>服务器列表变化事件参数</summary>
public class ServerListArgs : EventArgs
{
    public ServerListArgs(IReadOnlyList<string> serverNames, int selectedIndex, string stateText, Color stateColor)
        : base((int)GameEvent.ServerListChanged)
    {
        ServerNames = serverNames;
        SelectedIndex = selectedIndex;
        StateText = stateText;
        StateColor = stateColor;
    }

    public IReadOnlyList<string> ServerNames { get; }
    public int SelectedIndex { get; }
    public string StateText { get; }
    public Color StateColor { get; }
}

public enum PlayerStateType
{
    Idle,
    Move,
    Attack,
    Evade,
    Hurt,
    Dead,
    Ex
}

public enum EnemyStateType
{
    Idle,
    Attack,
    Hurt,
    Dead,
}

public interface IHurt
{
    /// <summary>
    /// 受伤的方法
    /// </summary>
    /// <param name="hitData">技能伤害信息</param>
    /// <param name="hurtSource">受到的伤害的来源</param>
    void OnHurt(HitData hitData, ISkillOwner hurtSource);
}

/// <summary>
/// 技能持有者的接口
/// </summary>
public interface ISkillOwner
{
    void StartSkillHit(int weaponIndex);

    void StopSkillHit(int weaponIndex);

    void SkillCanSwitch();

    void OnHit(IHurt hurt, Vector3 hurtPos);
}