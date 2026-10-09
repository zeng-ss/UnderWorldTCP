using System;
using System.Collections.Generic;
using System.Linq;
using HotUpdate.Core;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;
using Random = UnityEngine.Random;

namespace HotUpdate.Data
{
    [CreateAssetMenu(fileName = "NewTaskData", menuName = "Config/TaskData")]
    public class TaskDataConfigSo : ScriptableObject
    {
        [Header("★ 所有任务配置【在这里拖入所有子任务SO】★")] public List<TaskDataSo> taskDataList = new();
    }

    [Serializable]
    public class TaskReward
    {
        public List<int> depotIds = new(); // 获得的驱动盘奖励的 id列表
        public List<int> materialsId = new(); // 获得材料奖励的 id列表

        public string AddMaterialNum()
        {
            string des = "";
            var materialService = AppContext.Material;

            // 先快照要处理的 key，Add 会触发 MaterialNumChanged 事件，避免边遍历边改
            List<int> needUpdateKeys = materialsId.Where(id => materialService.Counts.ContainsKey(id)).ToList();
            foreach (var key in needUpdateKeys)
            {
                int addNum = key == 1 ? Random.Range(400, 1000) : Random.Range(10, 30);
                materialService.Add(key, addNum);

                var runtime = materialService.GetRuntime(key);
                des += $"{runtime?.Name ?? key.ToString()}×{addNum}\n";
            }

            return des;
        }

        public string AddDepotNum(bool isToFinishTask = false)
        {
            string des = "";
            var depotService = AppContext.Depot;
            if (depotService.Config == null || depotService.Config.depots == null) return des;

            foreach (var template in depotService.Config.depots)
            {
                if (!depotIds.Contains(template.depotId)) continue;
                int addNum = isToFinishTask ? 1 : Random.Range(1, 3);
                depotService.AddByTemplate(template, addNum);
                des += $"{template.depotName}×{addNum}\n";
            }

            return des;
        }
    }

    [Serializable]
    public class TaskDataSo
    {
        [Header("任务基础信息")] public int taskID; // 任务唯一ID
        [TextArea] public string taskDesc; // 任务描述
        public bool isUnlock = true; // 是否解锁
        public bool isFinished; // 是否已完成

        [Header("任务目标配置")] public TaskType taskType; // 任务类型
        public int targetCount; // 目标数量（比如建造2个）
        [HideInInspector] public int currentCount; // 当前进度

        [Header("任务奖励")] public TaskReward taskReward; // 奖励内容
    }

    // 任务状态机：任务的完整生命周期状态。
    // 用显式枚举替换原来的 IsUnlock / IsFinished 双 bool，状态迁移集中到 TaskService，
    // 杜绝「解锁了但没完成」「完成了但没领奖」这类靠 bool 组合推断的歧义。
    public enum TaskState
    {
        Locked = 0, // 未解锁
        InProgress = 1, // 已接取，进行中
        Completed = 2, // 条件达成，待领奖
        Claimed = 3, // 已领奖，任务结束
    }

    // TaskDataRuntime.cs - 运行时任务数据 使用运行时 TaskDataSO 的副本
    public class TaskDataRuntime
    {
        public readonly int TaskId;
        public readonly string TaskDesc;
        public TaskState State;
        public readonly TaskType TaskType;
        public readonly int TargetCount;
        public int CurrentCount;
        public readonly TaskReward TaskReward;

        public bool IsUnlock => State != TaskState.Locked;
        public bool IsFinished => State == TaskState.Claimed;

        public TaskDataRuntime()
        {
        }

        // 从 SO创建运行时数据（默认进入 Locked 状态，由服务端进度决定是否已解锁）
        public TaskDataRuntime(TaskDataSo so)
        {
            TaskId = so.taskID;
            TaskDesc = so.taskDesc;
            // 初始状态：SO 里 isUnlock 默认 true 的任务直接进入 InProgress
            State = so.isUnlock ? TaskState.InProgress : TaskState.Locked;
            TaskType = so.taskType;
            TargetCount = so.targetCount;
            CurrentCount = so.currentCount;
            // 深拷贝 TaskReward，避免修改 SO原数据
            TaskReward = new TaskReward
            {
                // 新建 List，而非引用
                depotIds = new List<int>(so.taskReward.depotIds),
                materialsId = new List<int>(so.taskReward.materialsId)
            };
        }
    }

    public enum TaskType
    {
        击败第一个敌人,
        给每一个驱动盘都升一级,
    }
}
