using System;
using System.Collections.Generic;
using Google.Protobuf;

/// <summary>
/// 网络消息处理委托：收到服务端某个协议号的数据时回调（data 为 protobuf 序列化字节）
/// </summary>
public delegate void OnActionHandler(ByteString data);

/// <summary>
/// 事件总线。普通类，由 AppContext 统一创建，不再自己当单例。
/// 内部有两条独立通道：
///   1. 游戏事件（GameEvent 枚举 + EventArgs）—— 用于系统内广播
///   2. 网络通道（int 协议号 + ByteString）—— 由 NetSocketMgr 收到服务端数据后派发，
///      原 SocketDispatcher 的职责已合并到这里
/// 用法：
///   监听  AppContext.Events.AddEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
///   触发  AppContext.Events.EventTrigger(GameEvent.DialogueEnd, new DialogueEndArgs(id));
///   移除  AppContext.Events.RemoveEventListener(GameEvent.DialogueEnd, OnDialogueEnd);
///   网络  AppContext.Events.AddNetHandler(NetDefine.CMD_LoginCode, OnLoginResult);
/// 注意：移除时请传方法组（如上），不要传匿名 lambda —— lambda 每次 new 出来的委托不相等，移除不掉。
/// </summary>
public class EventMgr
{
    private readonly Dictionary<GameEvent, Action<EventArgs>> _dic = new Dictionary<GameEvent, Action<EventArgs>>();

    // 网络通道：协议号 → 处理器（每个协议号只挂一个处理器，后注册的覆盖先注册的）
    private readonly Dictionary<int, OnActionHandler> _netDic = new Dictionary<int, OnActionHandler>();

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

    #region 网络通道（原 SocketDispatcher 职责）

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
