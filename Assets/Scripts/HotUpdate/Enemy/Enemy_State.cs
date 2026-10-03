using UnityEngine;

public class Enemy_State : State_Base
{
    protected EnemyCtrl enemy;

    public override void _Init(IState_MachineOwner owner)
    {
        base.Init(owner);
        enemy = (EnemyCtrl)owner;
    }

    protected virtual bool CheckAnimatorStateName(string stateName, out float normalizedTime,int layerIndex = 0)
    {
        AnimatorStateInfo nextInfo = enemy.enemyModel.Animator.GetNextAnimatorStateInfo(layerIndex);
        if (nextInfo.IsName(stateName))//判断当前动画是不是下一个动画
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = enemy.enemyModel.Animator.GetCurrentAnimatorStateInfo(layerIndex);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
    
    // 检查当前动画是否播放完毕
    protected bool IsAnimationFinished(string animationName)
    {
        AnimatorStateInfo stateInfo = enemy.enemyModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1.0f;
    }
    // 检查动画是否播放了特定时间
    protected bool IsAnimationMoreThanTime(string animationName,float time)
    {
        AnimatorStateInfo stateInfo = enemy.enemyModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= time;
    }
}
