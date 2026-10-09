using HotUpdate.Data;
using UnityEngine;

namespace HotUpdate.Event
{
    public enum GameEvent
    {
        CursorShow,
        CursorHide,
    }

    public class EventArgs
    {
        // 该参数对应的事件枚举值
        public int EventCode { get; }

        // 无参事件复用的空参数实例，避免每次触发都 new 一个对象
        public static readonly EventArgs Empty = new(0);

        protected EventArgs(int eventCode)
        {
            EventCode = eventCode;
        }
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
        void OnHurt(HitData hitData, ISkillOwner hurtSource);
    }

    public interface ISkillOwner
    {
        void StartSkillHit(int weaponIndex);

        void StopSkillHit(int weaponIndex);

        void SkillCanSwitch();

        void OnHit(IHurt hurt, Vector3 hurtPos);
    }
}