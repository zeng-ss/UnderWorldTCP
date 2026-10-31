using System;
using HotUpdate.Event;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 敌人状态机（只在宿主端切换状态）。
    /// 其他客户端不吃这套，只等服务端转发的位置 / 动作。
    /// </summary>
    public class EnemyStateMachine
    {
        private readonly EnemyCtrl _enemy;
        private readonly StateMachine _machine = new();

        private EnemyStateType _current = (EnemyStateType)(-1);

        public EnemyStateMachine(EnemyCtrl enemy) => _enemy = enemy;

        public void Init() => _machine.Init(_enemy);

        /// <summary>敌人销毁前停掉，避免状态还在 MonoManager 上被 Update</summary>
        public void Stop() => _machine.Stop();

        public void ChangeTo(EnemyStateType state)
        {
            // Idle / Dead 重复切没意义；Attack / Hurt 每次都重播（连招下一刀、连续受击）
            if (_current == state && state != EnemyStateType.Hurt) return;

            _current = state;
            switch (state)
            {
                case EnemyStateType.Idle:
                    _machine.ChangeState<EnemyIdleState>();
                    break;
                case EnemyStateType.Attack:
                    _machine.ChangeState<EnemyAttackState>();
                    break;
                case EnemyStateType.Move:
                    _machine.ChangeState<EnemyMoveState>();
                    break;
                case EnemyStateType.Hurt:
                    _machine.ChangeState<EnemyHurtState>(true);
                    break;
                case EnemyStateType.Dead:
                    _machine.ChangeState<EnemyDeadState>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}