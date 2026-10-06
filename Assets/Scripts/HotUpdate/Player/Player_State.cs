using UnityEngine;
// 玩家状态基类 继承状态基类
public class PlayerState : StateBase
{
    protected PlayerCtrl Player;
    
    public override void _Init(IStateMachineOwner owner)
    {
        base._Init(owner);
        Player = (PlayerCtrl)owner;
    }

    protected virtual bool _CheckAnimatorStateName(string stateName, out float normalizedTime,int layerIndex = 0)
    {
        AnimatorStateInfo nextInfo = Player.playerModel.Animator.GetNextAnimatorStateInfo(layerIndex);
        if (nextInfo.IsName(stateName))//判断当前动画是不是下一个动画
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = Player.playerModel.Animator.GetCurrentAnimatorStateInfo(layerIndex);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
    
    // 检查当前动画是否播放完毕
    protected bool IsAnimationFinished(string animationName)
    {
        AnimatorStateInfo stateInfo = Player.playerModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1.0f;
    }
    // 检查动画是否播放了特定时间
    protected bool IsAnimationMoreThanTime(string animationName,float time)
    {
        AnimatorStateInfo stateInfo = Player.playerModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= time;
    }
    
}