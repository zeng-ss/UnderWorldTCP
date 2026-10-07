using System;
using SqlSugar;

/// <summary>
/// 角色任务进度表。持久化任务状态机的运行时进度：
///   State 对应 TaskState 枚举（0=Locked 1=InProgress 2=Completed 3=Claimed）
///   由任务状态机的迁移驱动写入，启动时读取恢复。
/// </summary>
[SugarTable("role_task_progress", TableDescription = "角色任务进度表")]
public class RoleTaskProgress
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RoleId { get; set; }

    // 任务唯一 id（对应客户端 TaskDataSo.taskID）
    public int TaskId { get; set; }

    // 任务状态：TaskState 枚举值
    public int State { get; set; }

    // 当前进度
    public int CurrentCount { get; set; }

    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}