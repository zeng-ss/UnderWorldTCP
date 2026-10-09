using System.Collections.Generic;
using System.Linq;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Service
{
    /// <summary>
    /// 任务服务：持有运行时任务列表，负责状态迁移 / 进度推进 / 完成与领奖。
    /// </summary>
    public class TaskService
    {
        /// <summary>当前所有任务的运行时数据</summary>
        public List<TaskDataRuntime> Tasks { get; } = new();

        private bool _registered;
        private bool _persistEnabled; // 是否联网持久化（离线调试传 null config 时关闭）
        private TaskPanel _taskPanel;

        public void Init(TaskDataConfigSo config)
        {
            Tasks.Clear();
            // 离线调试（OfflineDebugLauncher 传 null）不联网持久化；正常联机才开启
            _persistEnabled = config != null;
            if (config == null || config.taskDataList == null) return;

            foreach (var taskSo in config.taskDataList)
            {
                if (taskSo != null) Tasks.Add(new TaskDataRuntime(taskSo));
            }

            RegisterEvents();
        }

        // 订阅领域事件，进度推进由事件驱动
        private void RegisterEvents()
        {
            if (_registered) return;
            _registered = true;
        }

        private void UnregisterEvents()
        {
            if (!_registered) return;
            _registered = false;
        }

        private TaskDataRuntime GetById(int taskId) => Tasks.FirstOrDefault(task => task.TaskId == taskId);

        /// <summary>
        /// 推进指定任务
        /// </summary>
        /// <param name="taskType">任务类型</param>
        public void AdvanceByType(TaskType taskType)
        {
            bool changed = false;
            foreach (var task in Tasks)
            {
                if (task.State != TaskState.InProgress || task.TaskType != taskType) continue;

                int oldCount = task.CurrentCount;
                task.CurrentCount = Mathf.Min(task.CurrentCount + 1, task.TargetCount);
                if (task.CurrentCount == oldCount) continue;

                changed = true;
                // 进度达标 → 自动迁移到 Completed
                if (task.CurrentCount >= task.TargetCount) Transition(task, TaskState.Completed);
            }

            if (changed) NotifyTaskChanged();
        }

        private void Transition(TaskDataRuntime task, TaskState to)
        {
            if (task == null) return;
            if ((int)to <= (int)task.State)
            {
                Debug.LogWarning($"[TaskService] 非法状态迁移：任务 {task.TaskId} 从 {task.State} 迁到 {to}，已忽略");
                return;
            }

            task.State = to;
        }

        #region 解锁

        /// <summary>解锁指定 id 的任务（Locked → InProgress），返回是否真的发生了变化</summary>
        public bool Unlock(int taskId)
        {
            var task = GetById(taskId);
            if (task == null || task.State != TaskState.Locked) return false;
            Transition(task, TaskState.InProgress);
            NotifyTaskChanged();
            SaveProgress(task); // 解锁状态也持久化
            return true;
        }

        /// <summary>批量解锁</summary>
        public void UnlockAll(IReadOnlyList<int> taskIds)
        {
            if (taskIds == null) return;
            foreach (var id in taskIds) Unlock(id);
        }

        #endregion

        #region 完成与领奖

        public bool CanClaim(TaskDataRuntime task) => task is { State: TaskState.Completed };

        /// <summary>
        /// 领奖：Completed → Claimed，发放奖励。UI 弹窗与联网由调用方（Controller）处理。
        /// </summary>
        public bool Claim(TaskDataRuntime task, out string rewardDescription)
        {
            rewardDescription = string.Empty;
            if (!CanClaim(task)) return false;

            Transition(task, TaskState.Claimed);
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
            NotifyTaskChanged();
            SaveProgress(task);
            return true;
        }

        #endregion

        #region 持久化

        /// <summary>把单个任务的进度与状态持久化到服务端</summary>
        private void SaveProgress(TaskDataRuntime task)
        {
            if (!_persistEnabled) return; // 离线调试模式不联网
            AppContext.Proto.RequestSaveTaskProgress(task, null);
        }

        /// <summary>
        /// 用服务端返回的进度恢复任务状态（联机时在 StartGame 后调用）。
        /// 服务端没有该任务记录时保持默认状态。
        /// </summary>
        public void RestoreProgress(int taskId, int state, int currentCount)
        {
            var task = GetById(taskId);
            if (task == null) return;

            task.State = (TaskState)state;
            task.CurrentCount = currentCount;
        }

        /// <summary>
        /// 联机时从服务端拉取全量任务进度并恢复状态机。
        /// 离线（OfflineDebugLauncher）或未登录时跳过，保持本地默认状态。
        /// </summary>
        public void LoadFromServer()
        {
            if (!_persistEnabled) return; // 离线调试模式不联网
            if (!AppContext.IsAlive || AppContext.Session.RoleId <= 0) return;

            AppContext.Proto.RequestLoadTaskProgress(ret =>
            {
                if (ret == null) return;
                foreach (var p in ret.ProgressList)
                {
                    RestoreProgress(p.TaskId, p.State, p.CurrentCount);
                }

                NotifyTaskChanged();
            });
        }

        #endregion

        private void NotifyTaskChanged()
        {
            _taskPanel = AppContext.Ui.GetPanel<TaskPanel>();
            _taskPanel.RefreshTaskUI(AppContext.Task.Tasks);
        }

        // 释放订阅（热更重载 / 退出时调用）
        public void Clear()
        {
            UnregisterEvents();
            Tasks.Clear();
        }
    }
}
