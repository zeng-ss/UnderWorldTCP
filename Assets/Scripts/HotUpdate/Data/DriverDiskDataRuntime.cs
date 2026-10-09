using System;
using System.Collections.Generic;
using HotUpdate.Data.Config;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;
using Random = UnityEngine.Random;

namespace HotUpdate.Data
{
    // 驱动盘的运行时实例。
    // 模板（名称 / 图标 / 初始等级 / 升级消耗 / 属性初值）来自 Luban 的 driverDisk 表导出的 Json；
    // 每个实例持有自己的等级与词条，发放时从模板拷贝一份。
    public class DriverDiskDataRuntime
    {
        public readonly string DepotName;
        public readonly int DepotId;
        public readonly string DepotIconName;
        public readonly List<int> MaterialsId;

        public int Level;
        public float CurLevelMaxFill;
        public float CurLevelFillValue;

        public DriverDiskValueData DepotDriverDiskValue = new();

        // 由配置行构造（模板 → 实例）
        public DriverDiskDataRuntime(DriverDiskRow row)
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

        // 拷贝构造：发放同名驱动盘时复制一份独立实例
        public DriverDiskDataRuntime(DriverDiskDataRuntime other)
        {
            DepotName = other.DepotName;
            DepotId = other.DepotId;
            DepotIconName = other.DepotIconName;
            MaterialsId = new List<int>(other.MaterialsId);
            Level = other.Level;
            CurLevelMaxFill = other.CurLevelMaxFill;
            CurLevelFillValue = other.CurLevelFillValue;
            DepotDriverDiskValue = new DriverDiskValueData
            {
                driverDiskType = other.DepotDriverDiskValue.driverDiskType,
                baseValue = other.DepotDriverDiskValue.baseValue,
                attackPercent = other.DepotDriverDiskValue.attackPercent,
                defensePercent = other.DepotDriverDiskValue.defensePercent,
                healthPercent = other.DepotDriverDiskValue.healthPercent,
                baoJiPercent = other.DepotDriverDiskValue.baoJiPercent,
            };
        }

        // Json 里的 diskType 是裸 int，在接入边界校验后转枚举
        private static DriverDiskType ParseDiskType(int raw)
        {
            if (Enum.IsDefined(typeof(DriverDiskType), raw)) return (DriverDiskType)raw;
            Debug.LogError($"DriverDiskDataRuntime: 配置表 diskType={raw} 非法，已按 Attack 处理");
            return DriverDiskType.Attack;
        }

        // 添加经验值
        public void AddExp(float exp)
        {
            CurLevelFillValue += exp;
            // 检查是否可以升级
            while (CurLevelFillValue >= CurLevelMaxFill)
            {
                Level++;
                if (Level == 2)
                {
                    // 事件化：不再直接调任务系统，改为通知 TaskService 推进
                    AppContext.Task.AdvanceByType(TaskType.给每一个驱动盘都升一级);
                }

                CurLevelFillValue -= CurLevelMaxFill;
                CurLevelMaxFill = (int)Random.Range(CurLevelMaxFill + 200, CurLevelMaxFill + 500);
                // 升级时提升属性
                UpgradeValue();
            }
        }

        // 检测材料数量是否足够增加经验值。
        // 这里只做判定，不再自己弹提示面板 —— 展示层的事交给 Controller 处理。
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

        // 升级属性提升
        private void UpgradeValue()
        {
            // 根据类型提升不同的属性
            switch (DepotDriverDiskValue.driverDiskType)
            {
                case DriverDiskType.Attack:
                    DepotDriverDiskValue.baseValue += Random.Range(20, 100);
                    DepotDriverDiskValue.attackPercent += 10f;
                    DepotDriverDiskValue.defensePercent += 5f;
                    DepotDriverDiskValue.healthPercent += 3f;
                    DepotDriverDiskValue.baoJiPercent += 3f;
                    break;
                case DriverDiskType.Defense:
                    DepotDriverDiskValue.baseValue += Random.Range(20, 100);
                    DepotDriverDiskValue.defensePercent += 10f;
                    DepotDriverDiskValue.healthPercent += 5f;
                    DepotDriverDiskValue.baoJiPercent += 2f;
                    DepotDriverDiskValue.attackPercent += 3f;
                    break;
                case DriverDiskType.Health:
                    DepotDriverDiskValue.baseValue += Random.Range(20, 100);
                    DepotDriverDiskValue.healthPercent += 10f;
                    DepotDriverDiskValue.defensePercent += 5f;
                    DepotDriverDiskValue.baoJiPercent += 1f;
                    DepotDriverDiskValue.attackPercent += 2f;
                    break;
                case DriverDiskType.BaoJi:
                    DepotDriverDiskValue.baseValue += Random.Range(5, 10);
                    DepotDriverDiskValue.baoJiPercent += 5f;
                    DepotDriverDiskValue.attackPercent += 3f;
                    DepotDriverDiskValue.defensePercent += 1f;
                    DepotDriverDiskValue.healthPercent += 1f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // 获取当前等级进度（0-1）
        public float GetLevelProgress()
        {
            return CurLevelMaxFill <= 0f ? 0f : CurLevelFillValue / CurLevelMaxFill;
        }
    }

    // 驱动盘的属性词条（可变，随升级提升）
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