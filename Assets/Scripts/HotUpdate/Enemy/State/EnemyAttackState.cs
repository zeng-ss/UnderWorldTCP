using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.Event;
using UnityEngine;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 攻击状态：只负责「播招式 + 收招冷却」。
    ///   · 位移与转向在 <see cref="EnemyMoveState"/>；
    ///   · 命中判定在武器碰撞盒：动画事件 StartSkillHit / StopSkillHit 开合碰撞盒，
    ///     碰到谁就由宿主上报谁（见 WeaponController），这里不做「在范围就算伤害」。
    /// </summary>
    public class EnemyAttackState : Enemy_State
    {
        // 普通攻击池（冲刺收招用 Attack05，单独处理）
        private static readonly string[] NormalAttacks = { "Attack01", "Attack02", "Attack03", "Attack04" };

        private readonly List<string> _usable = new();

        private EnemyAnimConfig.Entry _config;
        private string _currentAnim = "Attack01";

        public override void Enter()
        {
            if (!enemy.IsHost) return;

            enemy.HurtAnimAllowed = false;

            if (!enemy.HasTarget)
            {
                enemy.ChangeState(EnemyStateType.Idle);
                return;
            }

            // 冲刺过来的这一下用 Attack05 收招；平时在「够得着」的招式里随机
            string anim = enemy.ArrivedFromRun ? "Attack05" : PickAttack();
            enemy.ArrivedFromRun = false;

            _currentAnim = anim;
            _config = enemy.Anim(anim);
            enemy.PlayAnimation(anim);

            FaceTarget(); // 起手先对正
        }

        public override void Update()
        {
            if (!enemy.IsHost || enemy.IsDead) return;

            // 出招过程中持续面向目标：玩家绕背时下一刀才不会打偏（比平时跟得紧一点）
            FaceTarget(16f);

            if (!IsAnimationFinished(_currentAnim)) return;

            // 出招后冷却，冷却期间待机（避免贴脸秒连砍）
            enemy.NextAttackTime = Time.time + _config.cooldown;
            enemy.ChangeState(EnemyStateType.Idle);
        }

        public override void Exit()
        {
            if (!enemy.IsHost) return;
            enemy.HurtAnimAllowed = true;
        }

        // 在「攻击距离够得着当前距离」的招式里随机；都够不着就用射程最远的 Attack04（靠碰撞盒判定）
        private string PickAttack()
        {
            _usable.Clear();
            for (int i = 0; i < NormalAttacks.Length; i++)
            {
                if (enemy.Anim(NormalAttacks[i]).attackRange >= enemy.DistanceToTarget) _usable.Add(NormalAttacks[i]);
            }

            if (_usable.Count == 0) return "Attack04";
            return _usable[Random.Range(0, _usable.Count)];
        }
    }
}
