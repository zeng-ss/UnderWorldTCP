using UnityEngine;

/// <summary>
/// 任务 Controller。
///
/// 职责只有两个：
///   1. 把 View 上报的「任务已达完成条件」翻译成「发奖励 + 联网」（View → Controller → Model）
///   2. 管 Tab 键开关面板
///
/// 这里一行数值逻辑都没有 —— 全部在 TaskService 里。
/// 本类不持有 TaskPanel 引用：需要面板时向 UIManager 要，用完即弃；
/// 数据变化后由面板自己监听 TaskChanged 事件刷新（Model → View 走事件）。
///
/// 普通类，由 AppContext 创建，不是 MonoBehaviour、不是单例。
/// </summary>
public class TaskController
{
    private bool _initialized;

    /// <summary>由 GameManager 在 Awake 里调用（此时 InputManager 已就绪）</summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        InputManager.Instance.RegisterKeyDown(KeyCode.Tab, TogglePanel);
    }

    public void Dispose()
    {
        if (!_initialized) return;
        _initialized = false;

        InputManager.Instance.UnregisterKeyDown(KeyCode.Tab, TogglePanel);
    }

    /// <summary>预先把面板实例化出来并隐藏，避免首次按 Tab 时才加载造成卡顿</summary>
    public void Preload()
    {
        UIManager.Instance.OpenPanel<TaskPanel>(_ => UIManager.Instance.ClosePanel<TaskPanel>());
    }

    private void TogglePanel()
    {
        var panel = UIManager.Instance.GetPanel<TaskPanel>();
        if (panel != null && panel.gameObject.activeInHierarchy)
        {
            panel.ClosePanel();
            return;
        }

        UIManager.Instance.OpenPanel<TaskPanel>();
    }

    /// <summary>处理面板上报的「任务已完成」</summary>
    public void RequestFinish(TaskDataRuntime task)
    {
        if (!AppContext.Task.Finish(task, out string rewardText)) return;

        NotifyServer(task, rewardText);
    }

    private void NotifyServer(TaskDataRuntime task, string rewardText)
    {
        int rewardId = task.taskType == TaskType.击败第一个敌人 ? 1 : 2;
        ProtoHandler.Instance.RequestGetReward(rewardId, ret => { UIManager.Instance.ShowTip(rewardText, 4f); });
    }
}