using HotUpdate.Data.Config;
using UnityEngine;

namespace HotUpdate.Data
{
    // 材料运行时数据。
    // 静态定义（名称 / 图标 / 经验值）来自 Luban 的 materialData 表导出的 Json，客户端不再有本地 SO 配置；
    // 只有 MaterialValue 是可变的（强化时累加经验），数量属于玩家状态，由 MaterialService 单独持有。
    public class MaterialDataRuntime
    {
        public readonly int ID;
        public readonly string Name;
        public readonly string MaterialIconName;
        public float MaterialValue;

        public MaterialDataRuntime(MaterialDataRow row)
        {
            if (row == null)
            {
                Debug.LogWarning("MaterialDataRuntime: 配置行为空，已按默认值构造");
                ID = 0;
                Name = string.Empty;
                MaterialIconName = string.Empty;
                MaterialValue = 0f;
                return;
            }

            ID = row.materialId;
            Name = row.name;
            MaterialIconName = row.iconName;
            MaterialValue = row.expValue;
        }

        // 累加经验值
        public void AddValue(float addValue)
        {
            MaterialValue += addValue;
        }
    }
}
