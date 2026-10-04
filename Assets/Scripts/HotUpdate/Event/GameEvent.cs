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
    CursorHide
}

/// <summary>
/// 事件参数基类。自定义参数继承它
/// </summary>
public class EventArgs
{
    protected EventArgs(int eventCode)
    {
        EventCode = eventCode;
    }

    // 该参数对应的事件枚举值，用于断言与日志排查
    public int EventCode { get; }

    // 无参事件复用的空参数实例，避免每次触发都 new 一个对象
    public static readonly EventArgs Empty = new EventArgs(0);
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

public enum PlayerStateType
{
    Idle,
    Move,
    Attack,
    Evade,
    Hurt,
    Dead,
    EX
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