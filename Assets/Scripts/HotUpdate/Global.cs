using YooAsset;

public class Global : SingleTonMono<Global>
{
    private ResourcePackage _package;

    public ResourcePackage _YooPackage => _package;

    protected override void Awake()
    {
        base.Awake();
        _package = YooAssets.GetPackage("DefaultPackage");

        // 初始化TCP网络连接（连接 GateServer）
        NetSocketMgr.Instance.Init();
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        NetSocketMgr.Instance.Disconnect();
    }
}
