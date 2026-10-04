using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class TaskPanel : BasePanel
{
    [Header("任务面板UI组件")]
    public Transform taskContent;       // 任务项父物体
    
    // DOTween动画配置 
    Tweener openTweener;
    private float panelShowDuration = 0.3f;  // 面板打开/关闭动画时长
    private Ease panelEase = Ease.OutQuad;   // 面板动画曲线，丝滑回弹
    
    private float itemDelayInterval = 0.1f;  // 任务项逐个弹出的间隔
    private CanvasGroup taskCanvasGroup;     // 面板显隐用 CanvasGroup

    protected override void Awake()
    {
        base.Awake();
        taskCanvasGroup = GetComponent<CanvasGroup>();
        // 初始状态 - Y轴缩放为0（卷起状态）
        transform.localScale = new Vector3(1, 0, 1);
        taskCanvasGroup.alpha = 0;
        taskCanvasGroup.blocksRaycasts = false;
        ClearAllTaskItem();
    }

    private void OnEnable()
    {
        taskCanvasGroup.blocksRaycasts = true;
        taskCanvasGroup.interactable = true;
        // 重置为卷起状态
        transform.localScale = new Vector3(1, 0, 1);
        taskCanvasGroup.alpha = 0;
        // 卷轴打开动画：Y轴从0到1 + 淡入
        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOScaleY(1, panelShowDuration).SetEase(panelEase));
        sequence.Join(taskCanvasGroup.DOFade(1, panelShowDuration).SetEase(panelEase));
    }

    public void ClosePanel()
    {
        openTweener?.Kill();
        // 卷轴关闭动画：Y轴从1到0 + 淡出
        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOScaleY(0, panelShowDuration).SetEase(panelEase));
        sequence.Join(taskCanvasGroup.DOFade(0, panelShowDuration).SetEase(panelEase));
        sequence.OnComplete(() =>
        {
            UIManager.Instance.ClosePanel<TaskPanel>();
        });
    }

    /// <summary>
    /// 刷新任务面板UI（核心方法）
    /// </summary>
    public void RefreshTaskUI(List<TaskDataRuntime> taskList)
    {
        // 清空旧 Item
        ClearAllTaskItem();
        // 遍历创建任务项，带逐个入场动画
        for (int i = 0; i < taskList.Count; i++)
        {
            int index = i;
            var task = taskList[i];
            // 跳过已完成/未解锁的任务
            if (task.isFinished || !task.isUnlock) continue;
            // 延迟创建，实现逐个弹出的效果
            DOVirtual.DelayedCall(itemDelayInterval * index, ()=>{
                CreateTaskItem(task);
            });
        }
    }

    /// <summary>
    /// 创建单个任务项
    /// </summary>
    private void CreateTaskItem(TaskDataRuntime task)
    {
        ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/UI/UIItem/TaskItem", taskContent,(itemObj =>
        {
            if (itemObj == null)
            {
                Debug.LogError("加载任务项预制体失败：UI/UIItem/TaskItem");
                return;
            }
            itemObj.name = "TaskItem_" + task.taskId;
            itemObj.GetComponent<TaskItem>().UpdateData(task);
        }));
    }

    /// <summary>
    /// 清空所有任务项
    /// </summary>
    private void ClearAllTaskItem()
    {
        // 取消所有未执行的延迟创建任务防止重复生成
        DOTween.Kill(taskContent, true);
        // 删除所有子物体
        for (int i = 0; i < taskContent.childCount; i++)
        {
            Destroy(taskContent.GetChild(i).gameObject);
        }
    }
    
}