using cfg;

/// <summary>
/// 物品编号表（服务端侧）。
/// 编号与名称的唯一来源是 Luban 的物品表（LubanConfig/MiniTemplate/Datas）：
///   #driverDisk.xlsx   → driverDisk  表，depotId = 1-4
///   #materialData.xlsx → materialData 表，materialId = 5-9
/// 客户端 HotUpdate/Data/ItemCatalog.cs 使用同一套编号区间。
/// </summary>
public static class ItemCatalog
{
    // 金币：驱动盘升级的通用消耗材料（materialId = 5）。
    // 注意：这是编号约定而非配置数据，Luban 表里没有「哪个材料是金币」的标记。
    public const int Gold = 5;

    // role_bag_item.ItemType 取值
    public const string DriverDiskType = "DriverDisk";
    public const string MaterialType = "Material";

    /// <summary>取物品名称（查 Luban 物品表）；表里没有该编号时回退为编号本身</summary>
    public static string GetName(int itemId)
    {
        driverDisk disk = LubanMgr.Instance.GetDriverDiskById(itemId);
        if (disk != null) return disk.DepotName;

        materialData material = LubanMgr.Instance.GetMaterialById(itemId);
        if (material != null) return material.Name;

        return itemId.ToString();
    }

    /// <summary>是否为驱动盘（查 Luban 物品表，表里没有即视为材料）</summary>
    public static bool IsDriverDisk(int itemId)
    {
        return LubanMgr.Instance.GetDriverDiskById(itemId) != null;
    }

    /// <summary>按编号区分物品类型，与 StartGame 读背包时的分流规则同源</summary>
    public static string GetItemType(int itemId)
    {
        return IsDriverDisk(itemId) ? DriverDiskType : MaterialType;
    }
}
