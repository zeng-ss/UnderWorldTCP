using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Enemy
{
    public class EnemyHurtState : Enemy_State
    {
        // 反击前至少把受击动画露出来这么久，否则看起来像「没挨打就还手」
        private const float CounterDelay = 0.12f;

        private string _anim;
        private bool _counterQueued;
        private float _visibleTime; // 受击动画真正播了多久

        public override void Enter()
        {
            if (!enemy.IsHost) return;

            // 受击期间仍然允许被下一次受击打断：连续挨打要能一下一下地播受击动画，
            enemy.HurtAnimAllowed = true;

            _anim = enemy.View.PickHurtAnim();
            enemy.PlayAnimation(_anim);

            // 没有目标就别反击
            _counterQueued = enemy.HasTarget && Random.value < enemy.CounterAttackChance;
        }

        public override void Update()
        {
            if (!enemy.IsHost || enemy.IsDead) return;
            if (enemy.IsInHitStop) return; // 顿帧期间动画停着，不判播完

            _visibleTime += Time.deltaTime;

            if (_counterQueued && _visibleTime >= CounterDelay)
            {
                enemy.ChangeState(EnemyStateType.Attack); // 反击：直接进攻击状态
                return;
            }

            if (IsAnimationFinished(_anim)) enemy.ChangeState(EnemyStateType.Idle);
        }

        public override void Exit()
        {
            if (!enemy.IsHost) return;

            _counterQueued = false;
            enemy.HurtAnimAllowed = true;
            _visibleTime = 0;
        }
    }
}