using System.Collections.Generic;
using System.Linq;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Service
{
    /// <summary>
    /// 任务服务：持有服务端下发的任务列表，负责状态迁移 / 进度推进 / 领奖后的状态落地。
    /// 任务定义（描述 / 类型 / 目标数量 / 奖励）以服务端 Luban 配置为权威，客户端不再有本地任务配置；
    /// 奖励的发放在服务端完成，本服务只做状态机与上报。
    /// </summary>
    public class TaskService
    {
        /// <summary>当前所有任务的运行时数据（进游戏时由服务端全量下发重建）</summary>
        public List<TaskDataRuntime> Tasks { get; } = new();

        private bool _online; // 是否已拿到服务端任务数据；离线调试 / 拉取失败时为 false，不再向服务端上报
        private TaskPanel _taskPanel;

        /// <summary>清空本地任务数据。任务内容全部来自服务端，这里没有可初始化的配置。</summary>
        public void Init()
        {
            _online = false;
            Tasks.Clear();
        }

        #region 服务端数据

        /// <summary>
        /// 用服务端下发的全量任务重建列表（进游戏时调用）。
        /// 回包为空时列表为空，任务系统整体静默。
        /// </summary>
        public void ApplyServerTasks(IReadOnlyList<TaskInfo> taskList)
        {
            Tasks.Clear();
            if (taskList != null)
            {
                foreach (TaskInfo info in taskList)
                {
                    if (info != null) Tasks.Add(new TaskDataRuntime(info));
                }
            }

            // 拿到服务端任务数据才认为在线，后续状态变化才上报
            _online = Tasks.Count > 0;
            NotifyTaskChanged();
        }

        /// <summary>
        /// 从服务端拉取全量任务（定义 + 进度）。联机进 GameScene 时调用。
        /// 未登录 / 离线调试时不发起请求，列表保持为空。
        /// </summary>
        public void LoadFromServer()
        {
            if (!AppContext.IsAlive || AppContext.Session.RoleId <= 0) return;

            AppContext.Proto.RequestLoadTaskProgress(ret =>
            {
                if (ret == null || ret.CmdCode != CmdCode.Succeed)
                {
                    Debug.LogWarning($"[TaskService] 任务拉取失败：{(ret == null ? "无回包" : ret.CmdCode.ToString())}");
                    return;
                }

                ApplyServerTasks(ret.TaskList);
            });
        }

        #endregion

        private TaskDataRuntime GetById(int taskId) => Tasks.FirstOrDefault(task => task.TaskId == taskId);

        /// <summary>推进指定类型的所有进行中任务</summary>
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
                // 进度达标 → 自动迁移到 Completed，并立刻上报，服务端据此判定能否领奖
                if (task.CurrentCount >= task.TargetCount)
                {
                    Transition(task, TaskState.Completed);
                    SaveProgress(task);
                }
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

        #region 领奖

        public bool CanClaim(TaskDataRuntime task) => task is { State: TaskState.Completed };

        /// <summary>
        /// 领奖成功后的状态落地（Completed → Claimed）。
        /// 奖励本身由服务端发放（TaskController 走 GetReward 请求），本方法只在服务端确认后推进状态并推进剧情。
        /// </summary>
        public bool Claim(TaskDataRuntime task)
        {
            if (!CanClaim(task)) return false;

            Transition(task, TaskState.Claimed);
            AppContext.Story.Advance();

            NotifyTaskChanged();
            SaveProgress(task);
            return true;
        }

        #endregion

        #region 上报

        /// <summary>把单个任务的状态与进度上报服务端</summary>
        private void SaveProgress(TaskDataRuntime task)
        {
            if (!_online) return; // 离线调试 / 未拿到服务端任务数据时不联网
            AppContext.Proto.RequestSaveTaskProgress(task, null);
        }

        #endregion

        // 同模块 data→view：直接通知任务面板刷新；面板没开就跳过（下次 OnEnable 会自己重建）
        private void NotifyTaskChanged()
        {
            _taskPanel ??= AppContext.Ui.GetPanel<TaskPanel>();
            if (_taskPanel == null) return;
            _taskPanel.RefreshTaskUI(Tasks);
        }

        // 释放本地数据（退出 / 热更重载时调用）
        public void Clear()
        {
            _online = false;
            Tasks.Clear();
        }
    }
}
