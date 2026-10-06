using System;

/// <summary>
/// 继承MonoBehaviour的 公共mono的类  注册更新的逻辑    
/// </summary>
public class MonoManager : UnitySingleTonMono<MonoManager>
{
    private Action _updateAction;
    private Action _lateUpdateAction;
    private Action _fixedUpdateAction;

    public void AddUpdateListener(Action action)
    {
        _updateAction += action;
    }

    public void RemoveUpdateListener(Action action)
    {
        _updateAction -= action;
    }

    public void AddFixedUpdateListener(Action action)
    {
        _fixedUpdateAction += action;
    }

    public void RemoveFixedUpdateListener(Action action)
    {
        _fixedUpdateAction -= action;
    }

    public void AddLateUpdateListener(Action action)
    {
        _lateUpdateAction += action;
    }

    public void RemoveLateUpdateListener(Action action)
    {
        _lateUpdateAction -= action;
    }


    private void Update()
    {
        _updateAction?.Invoke();
    }

    private void LateUpdate()
    {
        _lateUpdateAction?.Invoke();
    }

    private void FixedUpdate()
    {
        _fixedUpdateAction?.Invoke();
    }
}