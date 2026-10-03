using UnityEngine;
/// <summary>
/// 玩家状态基类 继承状态基类
/// </summary>
public class Player_State : State_Base
{
    protected PlayerCtrl _player;
    
    public override void _Init(IState_MachineOwner owner)
    {
        base._Init(owner);
        _player = (PlayerCtrl)owner;
    }

    protected virtual bool _CheckAnimatorStateName(string stateName, out float normalizedTime,int layerIndex = 0)
    {
        AnimatorStateInfo nextInfo = _player.playerModel.Animator.GetNextAnimatorStateInfo(layerIndex);
        if (nextInfo.IsName(stateName))//判断当前动画是不是下一个动画
        {
            normalizedTime = nextInfo.normalizedTime;
            return true;
        }
        AnimatorStateInfo info = _player.playerModel.Animator.GetCurrentAnimatorStateInfo(layerIndex);
        normalizedTime = info.normalizedTime;
        return info.IsName(stateName);
    }
    
    // 检查当前动画是否播放完毕
    protected bool IsAnimationFinished(string animationName)
    {
        AnimatorStateInfo stateInfo = _player.playerModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= 1.0f;
    }
    // 检查动画是否播放了特定时间
    protected bool IsAnimationMoreThanTime(string animationName,float time)
    {
        AnimatorStateInfo stateInfo = _player.playerModel.Animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(animationName) && stateInfo.normalizedTime >= time;
    }
    
}