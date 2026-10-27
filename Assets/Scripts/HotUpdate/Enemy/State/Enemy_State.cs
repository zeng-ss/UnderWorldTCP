using UnityEngine;

namespace HotUpdate.Enemy
{
    /// <summary>
    /// 敌人状态基类：持有 EnemyCtrl，并提供动画进度判断。
    /// 所有状态里的逻辑都先判 <see cref="EnemyCtrl.IsHost"/>，非宿主端什么也不做。
    /// </summary>
    public class Enemy_State : StateBase
    {
        protected EnemyCtrl enemy;

        public override void _Init(IStateMachineOwner owner)
        {
            base._Init(owner);
            enemy = (EnemyCtrl)owner;
        }

        /// <summary>平滑转向当前目标（移动 / 攻击状态共用；没有目标就不动）</summary>
        protected void FaceTarget(float lerpSpeed = 12f)
        {
            if (!enemy.HasTarget) return;

            Vector3 to = enemy.TargetPosition - enemy.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;

            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation,
                Quaternion.LookRotation(to), lerpSpeed * Time.deltaTime);
        }

        // 当前动画是否播完
        protected bool IsAnimationFinished(string animationName)
        {
            Animator animator = enemy.ModelAnimator;
            if (animator == null) return true;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(animationName) && info.normalizedTime >= 1f;
        }

        // 当前动画是否播过某个进度（0~1，不是秒）
        protected bool IsAnimationMoreThanTime(string animationName, float normalizedTime)
        {
            Animator animator = enemy.ModelAnimator;
            if (animator == null) return true;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(animationName) && info.normalizedTime >= normalizedTime;
        }
    }
}
