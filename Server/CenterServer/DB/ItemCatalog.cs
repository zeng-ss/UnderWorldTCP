using cfg;

/// <summary>
/// 物品编号表
/// </summary>
public static class ItemCatalog
{
    public const int Gold = 5;

    // role_bag_item.ItemType 取值
    public const string DriverDiskType = "DriverDisk";
    public const string MaterialType = "Material";

    public static string GetName(int itemId)
    {
        driverDisk disk = LubanMgr.Instance.GetDriverDiskById(itemId);
        if (disk != null) return disk.DepotName;

        materialData material = LubanMgr.Instance.GetMaterialById(itemId);
        if (material != null) return material.Name;

        return itemId.ToString();
    }

    public static bool IsDriverDisk(int itemId) => LubanMgr.Instance.GetDriverDiskById(itemId) != null;

    public static string GetItemType(int itemId)
    {
        return IsDriverDisk(itemId) ? DriverDiskType : MaterialType;
    }
}