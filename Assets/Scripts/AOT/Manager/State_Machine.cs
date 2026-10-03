using System;
using System.Collections.Generic;

/// <summary>
/// 状态机类 目的来控制状态的转换
/// </summary>
public class State_Machine  // 什么时候会new这个状态机？玩家脚本初始化时候 new了状态机对象
{
    private IState_MachineOwner owner; // 保存宿主对象
    private State_Base currentState;   // 当前的状态
    public State_Base CurrentState => currentState;
    private Dictionary<Type, State_Base> stateDic = new ();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="owner">宿主对象</param>
    public void Init(IState_MachineOwner owner) { this.owner = owner; }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="isResfeshState"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool ChangeState<T>(bool isResfeshState = false) where T : State_Base, new()
    {
        //先拿到T状态类型
        Type type = typeof(T);
        //状态一致 不需要切换
        if(!isResfeshState && currentState != null && currentState.GetType() == type) return false;
        //退出当前状态 状态不一致 执行exit方法 退出时候状态并没有被销毁
        if (currentState != null)//有可能从空状态切换过来
        {
            currentState.Exit();
            //解除公共momo对状态相关刷新的监听
            MonoManager.Instance.RemoveUpdateListener(currentState.Update);
            MonoManager.Instance.RemoveLateUpdateListener(currentState.LateUpdate);
            MonoManager.Instance.RemoveFixedUpdateListener(currentState.FixedUpdate);
        }
        //进入新状态 拿到新状态给currentState赋值
        currentState = GetType<T>();
        currentState.Enter();
        //添加新的监听
        MonoManager.Instance.AddUpdateListener(currentState.Update);
        MonoManager.Instance.AddLateUpdateListener(currentState.LateUpdate);
        MonoManager.Instance.AddFixedUpdateListener(currentState.FixedUpdate);        
        return false;
    }

    /// <summary>
    /// 从字典中获取状态 如果状态不存在则创建一个新的状态对象放到字典中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private State_Base GetType<T>() where T : State_Base, new()
    {
        if (!stateDic.TryGetValue(typeof(T), out State_Base state))
        {
            state = new T();
            state._Init(owner);
            stateDic.Add(typeof(T), state);
        }
        return state;
    }
}
