using System.Collections.Generic;
using DG.Tweening;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Manager;
using HotUpdate.UI.UIItem;
using UnityEngine;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("TaskPanel")]
    public class TaskPanel : BasePanel
    {
        private TaskController _taskController;
        [Header("任务面板UI组件")] public Transform taskContent; // 任务项父物体

        // DOTween动画配置 
        private Tweener _openTween;
        private const float PanelShowDuration = 0.3f; // 面板打开/关闭动画时长
        private const Ease PanelEase = Ease.OutQuad; // 面板动画曲线，丝滑回弹
        private const float ItemDelayInterval = 0.1f; // 任务项逐个弹出的间隔
        private CanvasGroup _taskCanvasGroup; // 面板显隐用 CanvasGroup

        protected override void Awake()
        {
            base.Awake();
            _taskController = new TaskController();
            _taskController.Initialize();
            _taskCanvasGroup = GetComponent<CanvasGroup>();
            // 初始状态 - Y轴缩放为0（卷起状态）
            transform.localScale = new Vector3(1, 0, 1);
            _taskCanvasGroup.alpha = 0;
            _taskCanvasGroup.blocksRaycasts = false;
            ClearAllTaskItem();
        }

        private void OnEnable()
        {
            _taskCanvasGroup.blocksRaycasts = true;
            _taskCanvasGroup.interactable = true;
            // 重置为卷起状态
            transform.localScale = new Vector3(1, 0, 1);
            _taskCanvasGroup.alpha = 0;
            // 卷轴打开动画：Y轴从0到1 + 淡入
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScaleY(1, PanelShowDuration).SetEase(PanelEase));
            sequence.Join(_taskCanvasGroup.DOFade(1, PanelShowDuration).SetEase(PanelEase));

            // Model → View：数据一变自己刷新，不需要 Controller 反过来调面板
            AppContext.Events.AddEventListener(GameEvent.TaskChanged, OnTaskChanged);
            RefreshTaskUI(AppContext.Task.Tasks);
        }

        private void OnDisable()
        {
            AppContext.Events.RemoveEventListener(GameEvent.TaskChanged, OnTaskChanged);
        }

        private void OnTaskChanged(EventArgs args)
        {
            RefreshTaskUI(AppContext.Task.Tasks);
        }

        public void ClosePanel()
        {
            _openTween?.Kill();
            // 卷轴关闭动画：Y轴从1到0 + 淡出
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOScaleY(0, PanelShowDuration).SetEase(PanelEase));
            sequence.Join(_taskCanvasGroup.DOFade(0, PanelShowDuration).SetEase(PanelEase));
            sequence.OnComplete(() => { AppContext.Ui.ClosePanel<TaskPanel>(); });
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
                if (task.IsFinished || !task.IsUnlock) continue;
                // 延迟创建，实现逐个弹出的效果
                DOVirtual.DelayedCall(ItemDelayInterval * index, () => { CreateTaskItem(task); });
            }
        }

        /// <summary>
        /// 创建单个任务项
        /// </summary>
        private void CreateTaskItem(TaskDataRuntime task)
        {
            AppContext.Res.LoadAndInstantiateAsync("TaskItem", taskContent, (itemObj =>
            {
                if (itemObj == null)
                {
                    Debug.LogError("加载任务项预制体失败：UI/UIItem/TaskItem");
                    return;
                }

                itemObj.name = "TaskItem_" + task.TaskId;
                TaskItem taskItem = itemObj.GetComponent<TaskItem>();
                taskItem.OnFinishRequested += RaiseTaskFinishRequested;
                taskItem.UpdateData(task);
            }));
        }

        /// <summary>只上报意图，能不能完成、发什么奖励由 Controller 判定</summary>
        private void RaiseTaskFinishRequested(TaskDataRuntime task) => _taskController.RequestFinish(task);

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

        protected override void OnDestroy()
        {
            _openTween?.Kill();
            _taskController.Dispose();
        }
    }
}
