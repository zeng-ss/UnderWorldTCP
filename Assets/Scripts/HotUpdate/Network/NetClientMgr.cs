using Google.Protobuf;
using HotUpdate.Core;
using UnityEngine;

namespace HotUpdate.Network
{
    public class NetClientMgr : SingleTonMono<NetClientMgr>
    {
        private bool IsConnected { get; set; }

        // 离线调试：整条网络层直接空转，连 socket 都不建
        private bool _offline;

        protected override void Awake()
        {
            base.Awake();

            if (AppContext.IsOffline)
            {
                _offline = true;
                IsConnected = false;
                Debug.Log("NetClientMgr: 离线调试模式，不建立网络连接");
                return;
            }

            NetSocketMgr.Instance.Init();
            IsConnected = true;
            Debug.Log("NetClientMgr: 初始化网络连接");
        }

        /// <summary>
        /// 发送请求到服务器
        /// </summary>
        public void Send(int protoCode, ByteString data)
        {
            // 离线时静默丢弃：业务代码不用到处写 if (offline)
            if (_offline) return;

            if (!IsConnected)
            {
                Debug.LogError("NetClientMgr: 未连接到服务器");
                return;
            }

            NetSocketMgr.Instance.SendMsg(protoCode, data);
            if (protoCode == 11120 || protoCode == 11210 || protoCode == 11230) return;
            Debug.Log($"NetClientMgr: 发送消息 ProtoCode={protoCode}");
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
            if (_offline) return;
            NetSocketMgr.Instance.Disconnect();
            IsConnected = false;
        }
    }
}
