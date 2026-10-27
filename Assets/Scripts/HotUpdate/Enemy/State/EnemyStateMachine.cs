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

        public StateMachine Machine { get; } = new();

        // 初值不能是 Idle（枚举 0）：否则第一次 ChangeTo(Idle) 会被下面的去重挡掉，
        // 状态机永远不会启动 —— 表现就是敌人生成后僵在原地。
        private EnemyStateType _current = (EnemyStateType)(-1);

        public EnemyStateType CurrentState => _current;

        public EnemyStateMachine(EnemyCtrl enemy) => _enemy = enemy;

        public void Init() => Machine.Init(_enemy);

        /// <summary>敌人销毁前停掉，避免状态还在 MonoManager 上被 Update</summary>
        public void Stop() => Machine.Stop();

        public void ChangeTo(EnemyStateType state)
        {
            // Idle / Hurt / Dead 重复切没意义；Attack 每次都重播（连招、下一刀）
            if (_current == state && state != EnemyStateType.Attack) return;

            _current = state;
            switch (state)
            {
                case EnemyStateType.Idle:
                    Machine.ChangeState<EnemyIdleState>();
                    break;
                case EnemyStateType.Attack:
                    Machine.ChangeState<EnemyAttackState>(true);
                    break;
                case EnemyStateType.Move:
                    Machine.ChangeState<EnemyMoveState>();
                    break;
                case EnemyStateType.Hurt:
                    Machine.ChangeState<EnemyHurtState>();
                    break;
                case EnemyStateType.Dead:
                    Machine.ChangeState<EnemyDeadState>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}