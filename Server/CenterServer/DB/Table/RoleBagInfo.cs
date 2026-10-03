using System;
using SqlSugar;

/// <summary>
/// 角色背包物品表（含装备状态）
/// IsEquipped: 0=背包中 1=已装备
/// 物品模板数据由 Luban 管理，这里只存"拥有"和"穿戴"状态
/// </summary>
[SugarTable("role_bag_item", TableDescription = "角色背包物品表")]
public class RoleBagInfo
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RoleId { get; set; }
    public string ItemName {get; set;}
    public string ItemType {get; set;} // DriverDisk / Material
    public int ItemId { get; set; }
    public int Count { get; set; }
    
    public int BaseValue { get; set; }
    public float AttackPercent { get; set; }
    public float DefensePercent { get; set; }
    public float HealthPercent { get; set; }
    public float BaoJiPercent { get; set; }
    public int Level { get; set; }
    public float CurMaxFillValue { get; set; }
    public float CurFillValue { get; set; }

    /*[SugarColumn(DefaultValue = "0")]
    public int IsEquipped { get; set; }*/


    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}