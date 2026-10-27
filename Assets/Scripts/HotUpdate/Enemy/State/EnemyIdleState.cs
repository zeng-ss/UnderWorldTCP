using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 待机：站定播 Idle。有目标时短暂停顿后进入攻击状态（攻击状态自己负责追击）。
    /// </summary>
    public class EnemyIdleState : Enemy_State
    {
        // 从待机切攻击前的短暂停顿，避免来回切得太碎
        private const float IdleBeforeAttack = 0.35f;

        private float _timer;

        public override void Enter()
        {
            if (!enemy.IsHost) return;

            _timer = 0f;
            enemy.HurtAnimAllowed = true;
            enemy.PlayAnimation("Idle");
        }

        public override void Update()
        {
            if (!enemy.IsHost || enemy.IsDead) return;
            if (!enemy.HasTarget) return;

            _timer += Time.deltaTime;
            if (_timer < IdleBeforeAttack) return;
            if (Time.time < enemy.NextAttackTime) return; // 出招冷却中，先站着

            enemy.ChangeState(EnemyStateType.Move);
        }
    }
}