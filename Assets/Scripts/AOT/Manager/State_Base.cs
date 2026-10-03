public interface IState_MachineOwner
{
} //拿到玩家脚本对象，待会玩家去继承它就可以拿它来代表玩家

/// <summary>
/// 所以状态的基类 有可能是玩家状态 也可能是敌人状态
/// </summary>
public abstract class State_Base //抽象类
{
    //初始化方法 需要拿到宿主
    protected virtual void Init(IState_MachineOwner owner)
    {
    }

    public virtual void _Init(IState_MachineOwner owner)
    {
    }

    //反初始化 清除某些资源
    public virtual void Unit()
    {
    }

    //第一次进入状态时候要调用的方法
    public virtual void Enter()
    {
    }

    //退出状态时候要调用的方法
    public virtual void Exit()
    {
    }

    //自身的update方法 添加到公共mono中监听的
    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {
    }

    public virtual void FixedUpdate()
    {
    }
}