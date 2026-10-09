using System.Collections.Generic;

namespace HotUpdate.Data.Config
{
    // driverDisk 配置表的一行，供 Json 反序列化使用。
    // 字段名必须与 Luban 导出的 tbdriverdisk.json 的键名完全一致（区分大小写）。
    public class DriverDiskRow
    {
        public int depotId; // 驱动盘编号（1-4）
        public string depotName; // 驱动盘名
        public string iconName; // 图标资源名
        public int initLevel; // 初始等级
        public int initMaxFill; // 初始升级所需经验
        public List<int> upgradeMaterials; // 升级消耗的材料编号
        public int diskType; // 驱动盘类型，对应 DriverDiskType
        public int baseValue; // 主属性基础值
        public float attackPercent; // 攻击力加成百分比
        public float defensePercent; // 防御力加成百分比
        public float healthPercent; // 生命值加成百分比
        public float baoJiPercent; // 暴击率加成百分比
    }
}