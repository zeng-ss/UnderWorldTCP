using System.Collections.Generic;
using System.Net.Sockets;
using Google.Protobuf;


public class Session : ServerBase
{
    //保存每个Session的id  方便我们后续找到对应的Session
    public int SessionId { get; set; }

    public Session(Dictionary<int, IContainer> cmdDic, NetClient client)
    {
        _cmdDic = cmdDic;
        _Client = client;
        //在构造函数中将创建出来的Session添加到字典中 方便Session管理器进行管理 
        SessionMgr.Instance.AddSession(this);
    }

    /// <summary>
    /// 开始接收客户端发来的数据
    /// </summary>
    public void ReceiveData(Socket socket)
    {
        _socket = socket;
        BeginReceive();
    }

    /// <summary>
    /// 接收数据的时候执行的   有可能是作为客户端接收服务端的信息  也有可能是作为服务端接收客户端的信息 
    /// </summary>
    protected override void HandleCommand(BasePackage basePackage)
    {
        IContainer container = _cmdDic[basePackage.ProtoCode];
        if (container == null)
        {
            LogMsg.Info("command not regist..");
            return;
        }

        if (_Client != null)
        {
            if (_Client._clientType == ClientType.LoginServer ||
                _Client._clientType == ClientType.GateServer) //判断客户端类型是不是登录服务器 
            {
                basePackage.UnitySessionId = SessionId; //如果是登录服务器 或者网关服务器就要记录对应的unity的SessionId  
            }

            if (_Client._clientType == ClientType.GameServer) //记录与之连接的GateSessionId
            {
                basePackage.GateSessionId = SessionId; //判断类型是游戏逻辑服务器，记录网关服务器的SessionId  
            }
        }

        //服务端处理客户端发过来的数据 
        container.OnServerCommand(this, basePackage);
    }

    public override void Disconnect()
    {
        LogMsg.Info("Disconnect::" + _socket.RemoteEndPoint + "断开了连接");
        SessionMgr.Instance.OnSessionDisconnect(SessionId);
        SessionMgr.Instance.RemoveSeesion(SessionId);
        base.Disconnect();
    }
}