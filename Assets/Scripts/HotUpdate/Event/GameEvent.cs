using UnityEngine;

public enum GameEvent
{
    进度条加载,
    游戏开始,
    对话结束,
    光标出现,
    光标消失
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
    /// 受伤的方法,判断是否挡住了伤害
    /// </summary>
    /// <param name="hitData">技能伤害信息</param>
    /// <param name="hurtSource">受到的伤害的来源</param>
    void OnHurt(HitData hitData , ISkillOwner hurtSource);
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
