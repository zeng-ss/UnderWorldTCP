using System;
using System.Collections.Generic;
using HotUpdate.Data.Config;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.Data
{
    // 驱动盘的运行时实例。
    public class DriverDiskDataRuntime
    {
        public readonly string DepotName;
        public readonly int DepotId;
        public readonly string DepotIconName;
        public readonly List<int> MaterialsId;

        public int Level;
        public float CurLevelMaxFill;
        public float CurLevelFillValue;

        public readonly DriverDiskValueData DepotDriverDiskValue = new();

        // 客户端里「已升过一级」的等级门槛：初始 1 级，升到 2 级即算升过一次
        public const int UpgradedLevel = 2;

        // 由配置行构造（只取静态模板字段）
        private DriverDiskDataRuntime(DriverDiskRow row)
        {
            if (row == null)
            {
                Debug.LogWarning("DriverDiskDataRuntime: 配置行为空，已按默认值构造");
                DepotName = string.Empty;
                DepotId = 0;
                DepotIconName = string.Empty;
                MaterialsId = new List<int>();
                return;
            }

            DepotName = row.depotName;
            DepotId = row.depotId;
            DepotIconName = row.iconName;
            MaterialsId = row.upgradeMaterials != null ? new List<int>(row.upgradeMaterials) : new List<int>();
            Level = row.initLevel;
            CurLevelMaxFill = row.initMaxFill;
            CurLevelFillValue = 0f;
            DepotDriverDiskValue = new DriverDiskValueData
            {
                driverDiskType = ParseDiskType(row.diskType),
                baseValue = row.baseValue,
                attackPercent = row.attackPercent,
                defensePercent = row.defensePercent,
                healthPercent = row.healthPercent,
                baoJiPercent = row.baoJiPercent,
            };
        }

        // 由「配置行 + 服务端数据」构造：静态取模板，动态以服务端为准
        public DriverDiskDataRuntime(DriverDiskRow row, DriverDiskInfo info) : this(row)
        {
            ApplyFrom(info);
        }

        /// <summary>
        /// 用服务端数据覆盖动态字段
        /// </summary>
        public void ApplyFrom(DriverDiskInfo info)
        {
            if (info == null) return;

            Level = info.Level;
            CurLevelMaxFill = info.CurMaxFillValue;
            CurLevelFillValue = info.CurFillValue;
            DepotDriverDiskValue.baseValue = info.BaseValue;
            DepotDriverDiskValue.attackPercent = info.AttackPer;
            DepotDriverDiskValue.defensePercent = info.DefensePer;
            DepotDriverDiskValue.healthPercent = info.HealthPer;
            DepotDriverDiskValue.baoJiPercent = info.BaoJiPer;
        }

        // Json 里的 diskType 是裸 int，在接入边界校验后转枚举
        private static DriverDiskType ParseDiskType(int raw)
        {
            if (Enum.IsDefined(typeof(DriverDiskType), raw)) return (DriverDiskType)raw;
            Debug.LogError($"DriverDiskDataRuntime: 配置表 diskType={raw} 非法，已按 Attack 处理");
            return DriverDiskType.Attack;
        }

        // 检测材料数量是否足够升级。
        // 只做本地粗判，用于省掉一次必然失败的往返；真正的校验与扣除在服务端。
        public bool CheckCanAddExp(out string shortageTip)
        {
            shortageTip = "";
            bool canAddExp = true;
            foreach (var materialId in MaterialsId)
            {
                if (AppContext.Material.HasEnough(materialId)) continue;
                shortageTip += $"{AppContext.Material.GetRuntime(materialId)?.Name ?? materialId.ToString()}\n";
                canAddExp = false;
            }

            return canAddExp;
        }

        // 获取当前等级进度（0-1）
        public float GetLevelProgress()
        {
            return CurLevelMaxFill <= 0f ? 0f : CurLevelFillValue / CurLevelMaxFill;
        }
    }

    // 驱动盘的属性词条（由服务端下发，随升级提升）
    public class DriverDiskValueData
    {
        public DriverDiskType driverDiskType;
        public int baseValue;
        public float attackPercent;
        public float defensePercent;
        public float healthPercent;
        public float baoJiPercent;
    }
}