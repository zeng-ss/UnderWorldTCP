using System.Collections.Generic;
using Google.Protobuf;

public delegate void OnActionHandler(ByteString data);

public class SocketDispatcher : UnitySingleTonMono<SocketDispatcher>
{
    private readonly Dictionary<int, OnActionHandler> _actionDic = new();

    /// <summary>
    /// 注册事件 
    /// </summary>
    public void AddEventHandler(int protoCode, OnActionHandler handler)
    {
        if (!_actionDic.ContainsKey(protoCode) && handler != null)
        {
            _actionDic.Add(protoCode, handler);
        }
    }

    /// <summary>
    /// 删除事件 
    /// </summary>
    public void RemoveEventHandler(int protoCode)
    {
        _actionDic.Remove(protoCode);
    }

    /// <summary>
    /// 派发事件 
    /// </summary>
    public void DispatcherEvent(int protoCode, ByteString data)
    {
        if (_actionDic.ContainsKey(protoCode))
        {
            _actionDic[protoCode]?.Invoke(data);
        }
    }
}