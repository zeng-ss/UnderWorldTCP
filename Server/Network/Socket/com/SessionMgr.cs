




using System;
using System.Collections.Generic;
using System.Threading;

public class SessionMgr:Singleton<SessionMgr>
{
    //保存所有Session的字典
    private Dictionary<int,Session> _sessionDic=new Dictionary<int,Session>();
    private int _instanceInter;

    /// <summary>
    /// Session断开连接时触发，参数为sessionId
    /// </summary>
    public event Action<int> OnSessionDisconnected;

    public void OnSessionDisconnect(int sessionId)
    {
        OnSessionDisconnected?.Invoke(sessionId);
    }

    public void AddSession(Session session,int sessionId=-1)
    {
        if (sessionId<=0)
        {
            //生成唯一的id  
            sessionId = GetInstanceInter();
        }
        //如果字典中没有保存过对应的Session，则添加新的Session到字典中 
        if (!_sessionDic.ContainsKey(sessionId))
        {
            //将创建出来的唯一id给当前Session中的id赋值  
            session.SessionId = sessionId;
            _sessionDic.Add(sessionId,session);
        }
    }

    public void RemoveSeesion(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            _sessionDic.Remove(sessionId);
        }
    }

    public Session GetSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            return _sessionDic[sessionId];
        }
        return null;    
    }

    public int GetSessionCount()
    {
        return _sessionDic.Count;
    }

    /// <summary>
    /// 获取所有Session，用于广播消息
    /// </summary>
    public List<Session> GetAllSessions()
    {
        return new List<Session>(_sessionDic.Values);
    }
    
    /// <summary>
    /// 帮我生成唯一的id  
    /// </summary> 
    /// <returns></returns>

    public int GetInstanceInter()
    {
        return Interlocked.Increment(ref _instanceInter);
    }
}


