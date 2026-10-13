using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net.Sockets;


public class ServerBase
{
    //客户端类型
    public ClientType _clientType;

    public Action<int, ByteString> OnReceiveMsg;

    //指令集
    protected Dictionary<int, IContainer> _cmdDic = new Dictionary<int, IContainer>();

    //缓冲区
    private byte[] _buffer = new byte[1024 * 4];

    protected Socket _socket;

    //连接状态
    protected ConnState _connState;

    //发送锁：位置/动作同步会从 tick 线程发出，与网络线程的发送必须串行
    private readonly object _sendLock = new object();

    //作为服务端时需要拿到客户端的引用  
    public NetClient _Client;

    /// <summary>
    /// 开始接收服务端发来的数据
    /// </summary>
    protected void BeginReceive()
    {
        _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceiveCB, null);
    }

    /// <summary>
    /// 处理服务端发来的数据
    /// </summary>
    /// <param name="ar"></param>
    private void OnReceiveCB(IAsyncResult ar)
    {
        try
        {
            //返回的字节数
            int len = _socket.EndReceive(ar);
            if (len > 0)
            {
                while (true)
                {
                    ushort msgLen = BitConverter.ToUInt16(_buffer, 0); //无符号16位整数，范围从0-65535
                    if (len >= msgLen + 2)
                    {
                        //拿到了解析后的最终数据。
                        byte[] data = NetUtils.Instance.ParseData(_buffer, msgLen);
                        if (data != null)
                        {
                            BasePackage basePackage = BasePackage.Parser.ParseFrom(data);
                            //Console.WriteLine("basePackage::" + basePackage.ToString());
                            HandleCommand(basePackage);
                        }

                        len -= msgLen + 2;
                        //如果len还大于0 ，则发生了粘包
                        if (len > 0)
                        {
                            Buffer.BlockCopy(_buffer, msgLen + 2, _buffer, 0, len);
                        }
                    }
                    else break;
                }

                BeginReceive();
            }
            else Disconnect();
        }
        catch (Exception ex)
        {
            Disconnect();
            LogMsg.Info(ex.Message);
        }
    }

    protected virtual void HandleCommand(BasePackage basePackage)
    {
    }

    public virtual void Disconnect()
    {
        _connState = ConnState.Disconnected;

        if (_socket != null)
        {
            _socket.Close();
            _socket = null;
        }
    }


    /// <summary>
    /// 发送数据（客户端调用：protoCode + data）
    /// </summary>
    public void SendData(int protoCode, ByteString data)
    {
        BasePackage basePackage = new BasePackage();
        SendData(basePackage, protoCode, data);
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    public void SendData(BasePackage basePackage, int protoCode = -1, ByteString data = null)
    {
        try
        {
            if (protoCode != -1) basePackage.ProtoCode = protoCode;

            if (data != null) basePackage.Data = data;

            // 高频同步包不打日志，否则 10Hz × 敌人数 × 玩家数会直接刷爆控制台
            switch (basePackage.ProtoCode)
            {
                case NetDefine.CMD_PositionSyncCode:
                case NetDefine.CMD_EnemyPositionSyncCode:
                case NetDefine.CMD_SyneEnemyAniCode:
                case NetDefine.CMD_SyneAniCode:
                case NetDefine.CMD_PlayerVfxCode:
                    break;
                default:
                    LogMsg.Info($"发送了数据:: ProtoCode: {basePackage.ProtoCode} , SessionId: {basePackage.UnitySessionId}");
                    break;
            }

            byte[] bytes = NetUtils.Instance.MakeData(basePackage.ToByteArray());
            lock (_sendLock)
            {
                if (_socket == null) return;
                _socket.Send(bytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    public void SendError(BasePackage basePackage, CmdCode cmdCode)
    {
        ErrMsg errMsg = new ErrMsg
        {
            CmdCode = cmdCode,
        };
        SendData(basePackage, NetDefine.CMD_ErrCode, errMsg.ToByteString());
    }
}