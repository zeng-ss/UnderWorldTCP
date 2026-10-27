using HotUpdate.Event;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 受击：播 HurtFront / HurtBack，播完回待机。受击期间不接受新的攻击状态切换。
    /// </summary>
    public class EnemyHurtState : Enemy_State
    {
        private string _anim;

        public override void Enter()
        {
            if (!enemy.IsHost) return;

            enemy.HurtAnimAllowed = false;
            _anim = enemy.PickHurtAnim();
            enemy.PlayAnimation(_anim);
        }

        public override void Update()
        {
            if (!enemy.IsHost || enemy.IsDead) return;
            if (IsAnimationFinished(_anim)) enemy.ChangeState(EnemyStateType.Idle);
        }

        public override void Exit()
        {
            if (!enemy.IsHost) return;
            enemy.HurtAnimAllowed = true;
        }
    }
}
