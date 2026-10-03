using System.Collections.Generic;
using UnityEngine.Events;

public class IEventInfo
{
}

public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

public class EventInfo<T, K> : IEventInfo
{
    public UnityAction<T, K> actions;

    public EventInfo(UnityAction<T, K> action)
    {
        actions += action;
    }
}


public class EventCenter : UnitySingleTonMono<EventCenter>
{
    //事件字典 
    private readonly Dictionary<GameEvent, IEventInfo> _eventDict = new();

    /// <summary>
    /// 添加事件监听
    /// </summary>
    /// <param name="eventName">事件名字</param>
    /// <param name="action">要执行的方法</param>
    public void AddEventListener(GameEvent eventName, UnityAction action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo)_eventDict[eventName]).actions += action;
        }
        else
        {
            _eventDict.Add(eventName, new EventInfo(action));
        }
    }

    public void AddEventListener<T>(GameEvent eventName, UnityAction<T> action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo<T>)_eventDict[eventName]).actions += action;
        }
        else
        {
            _eventDict.Add(eventName, new EventInfo<T>(action));
        }
    }

    public void AddEventListener<T, K>(GameEvent eventName, UnityAction<T, K> action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo<T, K>)_eventDict[eventName]).actions += action;
        }
        else
        {
            _eventDict.Add(eventName, new EventInfo<T, K>(action));
        }
    }

    /// <summary>
    /// 移除事件监听  
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="action"></param>
    public void RemoveEventListener(GameEvent eventName, UnityAction action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo)_eventDict[eventName]).actions -= action;
        }
    }

    public void RemoveEventListener<T>(GameEvent eventName, UnityAction<T> action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo<T>)_eventDict[eventName]).actions -= action;
        }
    }

    public void RemoveEventListener<T, K>(GameEvent eventName, UnityAction<T, K> action)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            ((EventInfo<T, K>)_eventDict[eventName]).actions -= action;
        }
    }

    /// <summary>
    /// 事件触发 
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(GameEvent eventName)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            (_eventDict[eventName] as EventInfo)?.actions?.Invoke();
        }
    }

    public void EventTrigger<T>(GameEvent eventName, T info)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            (_eventDict[eventName] as EventInfo<T>)?.actions?.Invoke(info);
        }
    }

    public void EventTrigger<T, K>(GameEvent eventName, T info, K info2)
    {
        if (_eventDict.ContainsKey(eventName))
        {
            (_eventDict[eventName] as EventInfo<T, K>)?.actions?.Invoke(info, info2);
        }
    }

    /// <summary>
    /// 清空字典 
    /// </summary>
    public void Clear()
    {
        _eventDict.Clear();
    }
}