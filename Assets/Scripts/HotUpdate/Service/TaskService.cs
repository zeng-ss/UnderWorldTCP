using System.Collections.Generic;

/// <summary>
/// 任务服务。持有运行时任务列表，负责解锁 / 推进进度 / 判定完成与发放奖励。
///
/// 原先这些逻辑写在一个 MonoBehaviour Manager 里，而且直接操作 GameManager 的
/// 字段并顺手弹 UI 面板；现在这里只做数据层的事，UI 与网络由 Controller 负责。
/// 数据变化统一广播 GameEvent.TaskChanged。
/// </summary>
public class TaskService
{
    /// <summary>当前所有任务的运行时数据</summary>
    public List<TaskDataRuntime> Tasks { get; } = new List<TaskDataRuntime>();

    public void Init(TaskDataConfigSo config)
    {
        Tasks.Clear();
        if (config == null || config.taskDataList == null) return;

        foreach (var taskSo in config.taskDataList)
        {
            if (taskSo != null) Tasks.Add(new TaskDataRuntime(taskSo));
        }
    }

    public TaskDataRuntime GetById(int taskId)
    {
        foreach (var task in Tasks)
        {
            if (task.TaskId == taskId) return task;
        }

        return null;
    }

    #region 解锁 / 进度

    /// <summary>解锁指定 id 的任务，返回是否真的发生了变化</summary>
    public bool Unlock(int taskId)
    {
        var task = GetById(taskId);
        if (task == null || task.IsUnlock) return false;
        task.IsUnlock = true;
        NotifyTaskChanged(taskId);
        return true;
    }

    /// <summary>批量解锁（一段对话配了多个任务时使用）</summary>
    public bool UnlockAll(IReadOnlyList<int> taskIds)
    {
        if (taskIds == null) return false;
        bool changed = false;
        foreach (int id in taskIds)
        {
            changed |= Unlock(id);
        }

        return changed;
    }

    /// <summary>
    /// 推进指定类型任务的进度。返回进度是否真的发生了变化。
    /// </summary>
    public bool UpdateProgress(TaskType taskType, int addCount = 1)
    {
        bool changed = false;
        int changedTaskId = -1;

        foreach (var task in Tasks)
        {
            if (task.IsFinished || !task.IsUnlock || task.TaskType != taskType) continue;

            int oldCount = task.CurrentCount;
            task.CurrentCount += addCount;
            if (task.CurrentCount > task.TargetCount) task.CurrentCount = task.TargetCount;

            if (task.CurrentCount != oldCount)
            {
                changed = true;
                changedTaskId = task.TaskId;
            }
        }

        if (changed) NotifyTaskChanged(changedTaskId);
        return changed;
    }

    #endregion

    #region 完成与奖励

    public bool CanFinish(TaskDataRuntime task)
    {
        return task != null && !task.IsFinished && task.CurrentCount >= task.TargetCount;
    }

    /// <summary>
    /// 标记任务完成并发放奖励。UI 弹窗与联网由调用方（Controller）处理。
    /// </summary>
    /// <param name="task">要完成的任务</param>
    /// <param name="rewardDescription">返回给 UI 展示的奖励文本</param>
    /// <returns>是否完成成功</returns>
    public bool Finish(TaskDataRuntime task, out string rewardDescription)
    {
        rewardDescription = string.Empty;
        if (!CanFinish(task)) return false;

        task.IsFinished = true;
        AppContext.Story.Advance();

        // 首个主线任务固定给 1 个驱动盘，其余随机给 1~2 个
        bool giveFixedCount = task.TaskType == TaskType.击败第一个敌人;
        string des = "获得奖励\n";
        if (task.TaskReward != null)
        {
            des += task.TaskReward.AddDepotNum(giveFixedCount);
            des += task.TaskReward.AddMaterialNum();
        }

        rewardDescription = des;
        NotifyTaskChanged(task.TaskId);
        return true;
    }

    #endregion

    private void NotifyTaskChanged(int changedTaskId)
    {
        AppContext.Events.EventTrigger(GameEvent.TaskChanged, new TaskChangedArgs(Tasks, changedTaskId));
    }
}