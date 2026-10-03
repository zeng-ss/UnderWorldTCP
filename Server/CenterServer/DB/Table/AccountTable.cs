using System;
using SqlSugar;

/// <summary>
/// 用户表，当用户注册时候存放注册信息的表    
/// </summary>
[SugarTable("account", TableDescription = "用户表")]
public class AccountTable
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] //数据库是自增才配自增  IsPrimaryKey表示是否是主键  IsIdentity表示是否自增长 
    public int Id { get; set; }

    //状态  
    [SugarColumn(DefaultValue = "1", IsOnlyIgnoreInsert = true)]
    public byte State { get; set; }


    [SugarColumn(Length = 30)] public string UserName { get; set; }
    [SugarColumn(Length = 15)] public string PhoneNum { get; set; }
    [SugarColumn(Length = 30)] public string Password { get; set; }

    public int LastLoginServerId { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}