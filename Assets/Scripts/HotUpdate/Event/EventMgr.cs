using System;
using System.Collections.Generic;

/// <summary>
/// 事件总线。普通类，由 AppContext 统一创建，不再自己当单例。
/// 用法：
///   监听  AppContext.Events.AddEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
///   触发  AppContext.Events.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(id));
///   移除  AppContext.Events.RemoveEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
/// 注意：移除时请传方法组（如上），不要传匿名 lambda —— lambda 每次 new 出来的委托不相等，移除不掉。
/// </summary>
public class EventMgr
{
    private readonly Dictionary<GameEvent, Action<EventArgs>> _dic = new Dictionary<GameEvent, Action<EventArgs>>();

    public void AddEventListener(GameEvent gameEvent, Action<EventArgs> action)
    {
        if (_dic.ContainsKey(gameEvent))
        {
            _dic[gameEvent] += action;
            return;
        }

        _dic.Add(gameEvent, action);
    }

    public void RemoveEventListener(GameEvent gameEvent, Action<EventArgs> action)
    {
        if (_dic.ContainsKey(gameEvent))
        {
            _dic[gameEvent] -= action;
        }
    }

    public void EventTrigger(GameEvent gameEvent, EventArgs args = null)
    {
        if (_dic.TryGetValue(gameEvent, out var actions))
        {
            actions?.Invoke(args ?? EventArgs.Empty);
        }
    }

    /// <summary>移除某个事件上的全部监听</summary>
    public void RemoveAllListeners(GameEvent gameEvent)
    {
        _dic.Remove(gameEvent);
    }

    /// <summary>清空所有事件（场景大切换 / 热更重载时调用）</summary>
    public void Clear()
    {
        _dic.Clear();
    }
}
