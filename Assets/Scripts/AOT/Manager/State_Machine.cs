using System;
using System.Collections.Generic;

/// <summary>
/// 状态机类 目的来控制状态的转换
/// </summary>
public class StateMachine // 什么时候会new这个状态机？玩家脚本初始化时候 new了状态机对象
{
    private IStateMachineOwner _owner; // 保存宿主对象
    private StateBase _currentState; // 当前的状态
    public StateBase CurrentState => _currentState;
    private Dictionary<Type, StateBase> _stateDic = new();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="owner">宿主对象</param>
    public void Init(IStateMachineOwner owner)
    {
        this._owner = owner;
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="isResfeshState"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool ChangeState<T>(bool isResfeshState = false) where T : StateBase, new()
    {
        //先拿到T状态类型
        Type type = typeof(T);
        //状态一致 不需要切换
        if (!isResfeshState && _currentState != null && _currentState.GetType() == type) return false;
        //退出当前状态 状态不一致 执行exit方法 退出时候状态并没有被销毁
        if (_currentState != null) //有可能从空状态切换过来
        {
            _currentState.Exit();
            //解除公共momo对状态相关刷新的监听
            MonoManager.Instance.RemoveUpdateListener(_currentState.Update);
            MonoManager.Instance.RemoveLateUpdateListener(_currentState.LateUpdate);
            MonoManager.Instance.RemoveFixedUpdateListener(_currentState.FixedUpdate);
        }

        //进入新状态 拿到新状态给currentState赋值
        _currentState = GetType<T>();
        _currentState.Enter();
        //添加新的监听
        MonoManager.Instance.AddUpdateListener(_currentState.Update);
        MonoManager.Instance.AddLateUpdateListener(_currentState.LateUpdate);
        MonoManager.Instance.AddFixedUpdateListener(_currentState.FixedUpdate);
        return false;
    }

    /// <summary>
    /// 从字典中获取状态 如果状态不存在则创建一个新的状态对象放到字典中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private StateBase GetType<T>() where T : StateBase, new()
    {
        if (!_stateDic.TryGetValue(typeof(T), out StateBase state))
        {
            state = new T();
            state._Init(_owner);
            _stateDic.Add(typeof(T), state);
        }

        return state;
    }
}