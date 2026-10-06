using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    /// <summary>
    /// 「该任务已达到完成条件」的用户意图。由 TaskPanel 转发给 Controller，
    /// View 本身不认识任何 Manager / Service。
    /// </summary>
    public event Action<TaskDataRuntime> OnFinishRequested;
    [FormerlySerializedAs("img_Fill")] public Image imgFill;
    [FormerlySerializedAs("img_Finish")] public Image imgFinish;
    [FormerlySerializedAs("txt_Desc")] public TMP_Text txtDesc;
    [FormerlySerializedAs("txt_Progress")] public TMP_Text txtProgress;
    private CanvasGroup _itemCg;
    
    private TaskDataRuntime _currentTask;

    private float _fillDuration = 2f;         // 填充动画时长
    private float _numRollDuration = 0.5f;    // 进度数字滚动动画时长
    private float _itemShowDuration = 0.2f;   // 单个任务项入场动画时长


    private void Awake() 
    {
        _itemCg = GetComponent<CanvasGroup>();
        // 初始化缩放和透明度，防止开局异常
        transform.localScale = Vector3.one;
        _itemCg.alpha = 1;
        
        // 初始化填充进度
        imgFill.fillAmount = 0;
    }
    
    public void UpdateData(TaskDataRuntime task)
    {
        _currentTask = task;
        // 达成条件时通知外部去走「完成 + 发奖励」流程，View 只管上报意图
        OnFinishRequested?.Invoke(task);
        txtDesc.text = task.TaskDesc;
        // 进度文本
        txtProgress.text = $"{task.CurrentCount}/{task.TargetCount}";
        // 完成状态：完成则显示完成图标，隐藏进度
        imgFinish.gameObject.SetActive(task.IsFinished);
        txtProgress.gameObject.SetActive(!task.IsFinished);

        // ========== 入场动画：只在未完成时执行 ==========
        if (!task.IsFinished)
        {
            if (_itemCg == null)
            {
                _itemCg = GetComponent<CanvasGroup>();
                if (_itemCg == null) _itemCg = gameObject.GetComponentInChildren<CanvasGroup>();
                _itemCg.alpha = 0;
            }
            transform.localScale = Vector3.zero; // 初始化缩放为 0
            // 执行入场动画
            _itemCg.DOFade(1, _itemShowDuration).SetEase(Ease.OutBack);  
            transform.DOScale(Vector3.one, _itemShowDuration).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    transform.localScale = Vector3.one; // 兜底
                });
            
            // 数字滚动动画
            DOTween.To(()=>0, value=>{
                    int showNum = Mathf.FloorToInt(value);
                    txtProgress.text = $"{showNum}/{task.TargetCount}";
                }, task.CurrentCount, _numRollDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(()=>{
                    txtProgress.text = $"{task.CurrentCount}/{task.TargetCount}";
                });
        }
        else
        {
            // 任务完成时，直接触发填充动画
            TriggerFillAnimation();
        }
        
    }

    // 单独的填充动画触发方法
    private void TriggerFillAnimation()
    {
        imgFill.fillAmount = 0; // 重置填充进度
        // 用DOTween实现平滑填充（替代Update的Lerp，更丝滑）
        imgFill.DOFillAmount(1, _fillDuration).SetEase(Ease.OutCubic)
            .OnComplete(()=>{
                // 填充完成后缩放消失
                transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        transform.DOScale(0f, 0.5f).SetEase(Ease.OutBack)
                            .OnComplete(() => {
                                // 消失后销毁物体，从面板移除
                                Destroy(gameObject);
                            });
                    });
            });
    }
    
}
