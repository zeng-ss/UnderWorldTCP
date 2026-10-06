using System;
using UnityEngine;
using UnityEngine.UI;

// 热更新窗口UI类 - 实现IHotUpdateWindow接口
public class HotUpdateWindow : MonoBehaviour, IHotUpdateWindow
{
    public Image progressBar; // 进度条Image组件
    public GameObject progressBarObj;
    public Text progressText; // 进度文本组件
    public Text descriptionText;
    public Button continueBtn;

    private float updateSpeed = 0.5f; // 进度条动画速度
    private float currentProgress; // 当前显示的进度值（用于平滑动画）
    private float progress; // 目标进度值（实际的下载进度）
    private long totalBytes; // 总下载字节数

    private Action OnEnd;

    private void Update()
    {
        // 平滑过渡：currentProgress逐渐接近progress
        currentProgress = Mathf.MoveTowards(currentProgress, progress, Time.deltaTime * updateSpeed);
        // 更新 UI
        progressBar.fillAmount = currentProgress; // 设置进度条填充量
        //progressText.text = $"{totalBytes * currentProgress / 1024 / 1024}MB/{totalBytes / 1024 / 1024}MB";
        if (!(currentProgress >= 1)) return;
        continueBtn.gameObject.SetActive(true);
        progressBarObj.gameObject.SetActive(false);
        continueBtn.onClick.AddListener(() =>
        {
            OnEnd?.Invoke();
            OnEnd = null;
        });
    }

    public void RefreshUI(float progress, string pregressTxt)
    {
        this.progress = progress;
        progressText.text = pregressTxt;
    }

    /// <summary>
    /// 显示热更新窗口
    /// </summary>
    /// <param name="totalBytes">总下载字节数</param>
    public void Show(long totalBytes, Action OnEnd)
    {
        gameObject.SetActive(true); // 激活窗口
        this.totalBytes = totalBytes; // 保存总大小
        this.OnEnd = OnEnd;
    }

    /// <summary>
    /// 更新下载进度（通过百分比）
    /// </summary>
    /// <param name="progress">下载进度 0-1</param>
    public void UpdateDownloadProgress(float progress)
    {
        this.progress = progress; // 设置目标进度
    }

    /// <summary>
    /// 更新下载进度（通过字节数）
    /// </summary>
    /// <param name="downloadBytes">已下载字节数</param>
    public void UpdateDownloadBytes(long downloadBytes)
    {
        // 计算百分比：已下载字节数 / 总字节数
        progress = (float)downloadBytes / totalBytes;
    }

    /// <summary>
    /// 设置更新信息
    /// </summary>
    public void GetVersion(string version, string versionInfo)
    {
        descriptionText.text = $"版本： {version}\n更新信息：{versionInfo}";
    }

    private void OnDestroy()
    {
        continueBtn.onClick.RemoveAllListeners();
    }
}