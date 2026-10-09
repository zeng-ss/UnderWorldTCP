using Google.Protobuf;
using UnityEngine;

namespace HotUpdate.Network
{
    public class NetClientMgr : SingleTonMono<NetClientMgr>
    {
        private bool IsConnected { get; set; }

        protected override void Awake()
        {
            base.Awake();
            NetSocketMgr.Instance.Init();
            IsConnected = true;
            Debug.Log("NetClientMgr: 初始化网络连接");
        }

        /// <summary>
        /// 发送请求到服务器
        /// </summary>
        public void Send(int protoCode, ByteString data)
        {
            if (!IsConnected)
            {
                Debug.LogError("NetClientMgr: 未连接到服务器");
                return;
            }

            Debug.Log($"NetClientMgr: 发送消息 ProtoCode={protoCode}");
            NetSocketMgr.Instance.SendMsg(protoCode, data);
        }

        /// <summary>
        /// 发送请求到服务器（protobuf消息对象）
        /// </summary>
        public void Send(int protoCode, IMessage message)
        {
            Send(protoCode, message.ToByteString());
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            NetSocketMgr.Instance.Disconnect();
            IsConnected = false;
        }
    }
}
