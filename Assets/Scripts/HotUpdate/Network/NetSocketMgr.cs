using System.Threading;
using Google.Protobuf;

/// <summary>
/// 网络模块管理类 
/// </summary>
public class NetSocketMgr : Singleton<NetSocketMgr>
{
    private static NetClient _client;

    public static NetClient Client => _client;

    SynchronizationContext _synchronizationContext;

    public void Init()
    {
        _synchronizationContext = SynchronizationContext.Current;
        //连接登录服务器  
        ConnectServer(NetDefine.IPHost, NetDefine.LoginServerPort);
    }

    private void ConnectServer(string host, int port)
    {
        Disconnect();

        _client = new NetClient(host, port, ClientType.Unity);
        _client.OnReceiveMsg += OnReciveMsgHandle;
        _client.StartConnect();
    }

    /// <summary>
    /// 收到服务端发来的数据 
    /// </summary>
    /// <param name="protoCode">proto类型</param>
    /// <param name="data">发过来的数据</param>
    private void OnReciveMsgHandle(int protoCode, ByteString data)
    {
        //把子线程切换回主线程 
        _synchronizationContext.Post(_ =>
        {
            //派发事件 
            AppContext.Events.DispatchNet(protoCode, data);
        }, null);
    }

    public void Disconnect()
    {
        if (_client != null)
        {
            _client.IsNeedReconn = false;
            _client.Disconnect();
            _client = null;
        }
    }

    public void SendMsg(int protoCode, ByteString data)
    {
        if (_client == null)
        {
            return;
        }
        _client.SendData(protoCode, data);
    }
}