using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 网络消息处理委托：收到服务端某个协议号的数据时回调（data 为 protobuf 序列化字节）
/// </summary>
public delegate void OnActionHandler(ByteString data);

public class EventMgr
{
    private readonly Dictionary<GameEvent, Action<EventArgs>> _dic = new();

    // 网络通道：协议号 → 处理器
    private readonly Dictionary<int, OnActionHandler> _netDic = new();

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

    #region 网络通道

    /// <summary>注册某协议号的网络处理器（同协议号重复注册会被忽略）</summary>
    public void AddNetHandler(int protoCode, OnActionHandler handler)
    {
        if (!_netDic.ContainsKey(protoCode) && handler != null)
        {
            _netDic.Add(protoCode, handler);
        }
    }

    /// <summary>移除某协议号的网络处理器</summary>
    public void RemoveNetHandler(int protoCode)
    {
        _netDic.Remove(protoCode);
    }

    /// <summary>由 NetSocketMgr 在收到服务端数据后调用（已切回主线程）</summary>
    public void DispatchNet(int protoCode, ByteString data)
    {
        if (_netDic.TryGetValue(protoCode, out var handler))
        {
            handler?.Invoke(data);
        }
    }

    #endregion

    /// <summary>清空游戏事件（场景大切换 / 热更重载时调用）。
    /// 网络通道不清：ProtoHandler 是 DontDestroyOnLoad 的持久对象，
    /// Awake 不会重跑，其注册的处理器由它自己的 OnDestroy 负责清理</summary>
    public void Clear()
    {
        _dic.Clear();
    }
}
