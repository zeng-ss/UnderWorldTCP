using System;
using SqlSugar;

/// <summary>
/// 角色表
/// </summary>
///
[SugarTable("role", TableDescription = "角色表")]
public class RoleTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] //数据库是自增才配自增  IsPrimaryKey表示是否是主键  IsIdentity表示是否自增长 
    public int Id { get; set; }

    //状态  
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte State { get; set; }

    //用户id  
    public int AccountId { get; set; }

    //昵称
    public string NickName { get; set; }


    //角色当前所在的地图 
    [SugarColumn(Length = 50)] public string SceneName { get; set; }


    //当前角色所属的服务器id  
    public int ServerId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}