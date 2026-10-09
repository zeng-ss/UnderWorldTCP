using HotUpdate.Network;

namespace HotUpdate.Launcher
{
    public class Global : SingleTonMono<Global>
    {
        protected override void Awake()
        {
            base.Awake();

            // 初始化TCP网络连接（连接 GateServer）
            NetSocketMgr.Instance.Init();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            NetSocketMgr.Instance.Disconnect();
        }
    }
}
