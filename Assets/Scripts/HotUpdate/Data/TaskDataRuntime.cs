using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
    // 任务状态机：任务的完整生命周期状态。
    public enum TaskState
    {
        Locked = 0, // 未解锁
        InProgress = 1, // 已接取，进行中
        Completed = 2, // 条件达成，待领奖
        Claimed = 3, // 已领奖，任务结束
    }

    /// <summary>
    /// 任务触发事件，值与服务端 #taskData.xlsx 的 taskType 列一一对应。
    /// 加任务只要在表里填同一个 type + 不同 condition / targetCount，这里不用动；
    /// 只有「出现了新的触发时机」才需要在这里加常量并在对应代码位置上报。
    /// </summary>
    public static class TaskEvent
    {
        public const int KillEnemy = 1;        // 击杀敌人（condition = 敌人配置 id，空 = 任意敌人）
        public const int UpgradeDriveDisk = 2; // 升级任意驱动盘一次
        public const int ClearStage = 3;       // 通关一局
    }

    // 运行时任务数据。客户端不再持有任何本地任务配置。
    public class TaskDataRuntime
    {
        public readonly int TaskId;
        public readonly string TaskDesc;
        public readonly int TaskType;
        public readonly string Condition; // 条件参数（击杀类填敌人配置 id，空 = 任意）
        public readonly int TargetCount;
        public readonly List<int> DepotIds; // 驱动盘奖励 id（服务端物品编号）
        public readonly List<int> MaterialsId; // 材料奖励 id（服务端物品编号）

        public TaskState State;
        public int CurrentCount;

        public bool IsUnlock => State != TaskState.Locked;
        public bool IsFinished => State == TaskState.Claimed;

        // 由服务端下发的任务数据构造（定义 + 进度一次到位）
        public TaskDataRuntime(TaskInfo info)
        {
            TaskId = info.TaskId;
            TaskDesc = info.TaskDesc;
            TaskType = info.TaskType;
            Condition = info.Condition ?? string.Empty;
            TargetCount = info.TargetCount;
            DepotIds = new List<int>(info.DepotIds);
            MaterialsId = new List<int>(info.MaterialsId);
            State = ParseState(info.State);
            CurrentCount = info.CurrentCount;
        }

        private static TaskState ParseState(int raw)
        {
            if (Enum.IsDefined(typeof(TaskState), raw)) return (TaskState)raw;
            Debug.LogError($"TaskDataRuntime: 服务端下发了未知的任务状态 {raw}，已按 Locked 处理");
            return TaskState.Locked;
        }
    }
}