using System;
using SqlSugar;

/// <summary>
/// 好友私聊消息表。每个「玩家对」(sender, receiver 无序) 最多保留 100 条，
/// 超出时按时间删除最旧的一条，保证单对聊天记录有上限。
/// </summary>
[SugarTable("friend_message", TableDescription = "好友私聊消息表")]
public class FriendMessage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    // 发送者
    public int SenderRoleId { get; set; }

    // 接收者
    public int ReceiverRoleId { get; set; }

    // 消息内容
    public string Content { get; set; }

    // 发送时间
    public DateTime SendTime { get; set; }
}
