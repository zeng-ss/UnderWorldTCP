using System;
using SqlSugar;

/// <summary>
/// 用户表，当用户注册时候存放注册信息的表    
/// </summary>
///
[SugarTable("game_server", TableDescription = "服务器列表")]
public class GameServerTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增  IsPrimaryKey表示是否是主键  IsIdentity表示是否自增长 
    public int Id { get; set; }
    //状态  
    [SugarColumn(DefaultValue = "1",IsOnlyIgnoreInsert = true)]
    public byte State{ get; set; }
    
    
    //服务器名称
    [SugarColumn(Length = 30)]
    public string ServerName { get; set; }
    //运行状态 1.爆满 2.拥挤 3.正常  
    public byte RunState { get; set; }
    //是否是新服  1表示新服   0表示不是新服
    public byte IsNew { get; set; } 
    //服务器ip
    [SugarColumn(Length = 30)]
    public string IPHost { get; set; }
    //当前服务器端口号
    public int Port { get; set; }
    //创建时间
    public DateTime CreateDate { get; set; }
    //更新时间
    public DateTime UpdateDate { get; set; }
}
