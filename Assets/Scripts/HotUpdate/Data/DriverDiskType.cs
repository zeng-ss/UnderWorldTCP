namespace HotUpdate.Data
{
    // 驱动盘类型。
    // 数值必须与服务端 Luban 的 DriverDiskType（Defines/item.xml）保持一致，
    // 对应 driverDisk 表的 diskType 列（Json 里是裸 int，接入时校验后转枚举）。
    public enum DriverDiskType
    {
        Attack = 0, // 攻击
        Defense = 1, // 防御
        Health = 2, // 生命
        BaoJi = 3, // 暴击
    }
}
