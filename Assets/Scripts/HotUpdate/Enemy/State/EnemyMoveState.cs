using HotUpdate.Data;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 移动状态：所有「位移 + 转向」都只在这里做。
    ///   · 距离 ≤ RunDistance：WalkF 走过去；
    ///   · 距离 &gt; RunDistance：Attack05Start 起手 → Attack05Run 冲刺；
    ///   · 进入攻击距离就切 Attack（冲刺过来的会告诉 Attack 用 Attack05 收招）。
    /// 每段动画用不吃根运动、跑多快，全看 EnemyAnimConfig 里那一行配置。
    /// </summary>
    public class EnemyMoveState : Enemy_State
    {
        private enum Phase
        {
            Walk,
            DashStart,
            DashRun,
        }

        // 超过这个距离改用冲刺
        private const float RunDistance = 8f;

        private Phase _phase;

        public override void Enter()
        {
            if (!enemy.IsHost) return;

            enemy.HurtAnimAllowed = true;

            if (!enemy.HasTarget)
            {
                enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            if (enemy.DistanceToTarget > RunDistance)
            {
                _phase = Phase.DashStart;
                enemy.PlayAnimation("Attack05Start");
            }
            else
            {
                _phase = Phase.Walk;
                enemy.PlayAnimation("WalkF");
            }
        }

        public override void Update()
        {
            if (!enemy.IsHost || enemy.IsDead) return;
            if (enemy.IsInHitStop) return; // 顿帧期间冻结

            if (!enemy.HasTarget)
            {
                enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            // 进入攻击距离：冲刺过来的用 Attack05 收招
            if (enemy.DistanceToTarget <= enemy.StopDistance)
            {
                enemy.ArrivedFromRun = _phase == Phase.DashRun;
                enemy.ChangeState(EnemyStateType.Attack);
                return;
            }

            FaceTarget();

            switch (_phase)
            {
                case Phase.Walk:
                    Move(enemy.Anim("WalkF"));
                    break;

                case Phase.DashStart:
                    if (IsAnimationFinished("Attack05Start"))
                    {
                        _phase = Phase.DashRun;
                        enemy.PlayAnimation("Attack05Run");
                    }

                    break;

                case Phase.DashRun:
                    Move(enemy.Anim("Attack05Run"));
                    break;
            }
        }

        // 有根运动就让动画推；没有就按配置里的速度平移，靠近到攻击距离就停
        private void Move(EnemyAnimConfig.Entry entry)
        {
            if (entry.useRootMotion) return;

            float speed = entry.moveSpeed;
            if (speed <= 0f) return;

            Vector3 to = enemy.TargetPosition - enemy.transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.05f) return;

            float step = Mathf.Min(speed * Time.deltaTime, Mathf.Max(0f, dist - enemy.StopDistance));
            enemy.transform.position += to / dist * step;
        }
    }
}