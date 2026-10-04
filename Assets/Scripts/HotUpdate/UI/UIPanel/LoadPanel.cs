using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadPanel : BasePanel
{
    public Image pivot;           // 进度指示器（滑块/箭头/特效）
    public Image loadingBar;      // 进度条
    public TMP_Text progressText; // 进度文本
    
    public Ease animationEase = Ease.OutQuad; // 动画曲线
    
    [Header("Pivot设置")]
    public float pivotMinX;    // pivot最小X位置
    public float pivotMaxX = -1350;  // pivot最大X位置
    private float targetProgress;
    
    private void OnEnable() { ResetProgress(); }
    private void Start()
    {
        EventMgr.Instance.AddEventListener(GameEvent.LoadProgress, UpdateProgress);
        // 初始化
        loadingBar.fillAmount = 0f;
        UpdateProgressText(0f);
        UpdatePivotPosition(0f);
    }
    private void UpdateProgress(EventArgs args)
    {
        float progress = ((LoadProgressArgs)args).Progress;
        targetProgress = Mathf.Clamp01(progress);
        // 终止正在进行的动画
        // 进度条动画
        loadingBar.DOFillAmount(targetProgress,  0.5f)
            .SetEase(animationEase)
            .OnUpdate(() => 
            {
                // 在进度条动画更新时，同步更新文本
                UpdateProgressText(loadingBar.fillAmount);
                // 在这里同步更新 pivot跟随进度条
                UpdatePivotPosition(loadingBar.fillAmount);
            });
    }
    
    /// <summary>
    /// 更新进度文本
    /// </summary>
    private void UpdateProgressText(float progress) { progressText.text = $"{progress * 100:F0}%"; }
    
    /// <summary>
    /// 更新pivot位置
    /// </summary>
    private void UpdatePivotPosition(float progress)
    {
        float targetX = Mathf.Lerp(pivotMinX, pivotMaxX, progress);
        Vector3 localPos = pivot.transform.localPosition;
        localPos.x = targetX;
        pivot.transform.localPosition = localPos;
    }
    private void ResetProgress()
    {
        // 强制重置进度条到0
        loadingBar.fillAmount = 0f;
        UpdateProgressText(0f);
        Vector3 localPos = pivot.transform.localPosition;
        localPos.x = -775; // 重置到最左边
        pivot.transform.localPosition = localPos;
        targetProgress = 0f;
    }
    private void OnDestroy() { EventMgr.Instance.RemoveEventListener(GameEvent.LoadProgress, UpdateProgress); }
}