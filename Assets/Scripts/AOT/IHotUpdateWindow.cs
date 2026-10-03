
// 热更新窗口接口 - 定义 UI更新方法

using System;

public interface IHotUpdateWindow
{
    public void RefreshUI(float progress, string pregressTxt);
    public void Show(long totalBytes,Action OnEnd);  // 显示窗口并设置总下载大小
    public void UpdateDownloadProgress(float progress);  // 更新下载进度（0-1）
    public void UpdateDownloadBytes(long downloadBytes);  // 更新已下载字节数
    public void GetVersion(string versionInfo,string version);
}
