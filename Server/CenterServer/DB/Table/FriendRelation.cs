using System;
using SqlSugar;

/// <summary>
/// 好友关系表。存「单向申请 + 状态」的模型：
///   from_role_id 向 to_role_id 发起申请，status 记录当前关系：
///   0 = 待处理（to 尚未决定）
///   1 = 已同意（此时 from 与 to 互为好友）
///   2 = 已拒绝
/// 好友列表 = status==1 且 (from==我 或 to==我)。
/// 防重复：同一对 (from,to) 在 status<=1 时不允许重复插入。
/// </summary>
[SugarTable("friend_relation", TableDescription = "好友关系表")]
public class FriendRelation
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    // 申请发起方
    public int FromRoleId { get; set; }

    // 被申请方
    public int ToRoleId { get; set; }

    // 关系状态：0=待处理 1=已同意 2=已拒绝
    public int Status { get; set; }

    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}