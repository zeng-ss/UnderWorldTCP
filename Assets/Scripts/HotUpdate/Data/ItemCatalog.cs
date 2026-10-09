namespace HotUpdate.Data
{
    // 物品编号表（客户端侧）。
    // 编号与服务端 ItemCatalog 一致，并且与 Luban 配置表严格对应：
    //   #driverDisk.xlsx 的 depotId = 1-4， #materialData.xlsx 的 materialId = 5-9。
    // TODO: 待服务端把物品表也 Luban 化后，本类改为直接查表，删除这些常量。
    public static class ItemCatalog
    {
        // 驱动盘编号区间
        public const int MinDriverDiskId = 1;
        public const int MaxDriverDiskId = 4;

        // 材料编号：5=金币（升级驱动盘的通用消耗），6-9=四种驱动材料
        public const int Gold = 5;
        public const int AttackDrive = 6;
        public const int HealthDrive = 7;
        public const int DefenseDrive = 8;
        public const int BaoJiDrive = 9;

        // 按编号判断是驱动盘还是材料（与服务端同一规则）
        public static bool IsDriverDisk(int itemId) => itemId >= MinDriverDiskId && itemId <= MaxDriverDiskId;
    }
}
