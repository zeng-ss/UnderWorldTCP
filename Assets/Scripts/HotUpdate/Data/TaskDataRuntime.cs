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

    public enum TaskType
    {
        击败第一个敌人 = 0,
        给每一个驱动盘都升一级 = 1,
    }

    // 运行时任务数据。客户端不再持有任何本地任务配置。
    public class TaskDataRuntime
    {
        public readonly int TaskId;
        public readonly string TaskDesc;
        public readonly TaskType TaskType;
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
            TaskType = ParseTaskType(info.TaskType);
            TargetCount = info.TargetCount;
            DepotIds = new List<int>(info.DepotIds);
            MaterialsId = new List<int>(info.MaterialsId);
            State = ParseState(info.State);
            CurrentCount = info.CurrentCount;
        }

        // 协议层的裸 int 一律在接入边界校验后转枚举
        private static TaskType ParseTaskType(int raw)
        {
            if (Enum.IsDefined(typeof(TaskType), raw)) return (TaskType)raw;
            Debug.LogError($"TaskDataRuntime: 服务端下发了未知的任务类型 {raw}，已按默认值处理");
            return default;
        }

        private static TaskState ParseState(int raw)
        {
            if (Enum.IsDefined(typeof(TaskState), raw)) return (TaskState)raw;
            Debug.LogError($"TaskDataRuntime: 服务端下发了未知的任务状态 {raw}，已按 Locked 处理");
            return TaskState.Locked;
        }
    }
}