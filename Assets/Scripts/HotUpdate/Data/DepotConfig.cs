using System;
using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Event;
using UnityEngine;
using UnityEngine.Serialization;
using AppContext = HotUpdate.Core.AppContext;
using Random = UnityEngine.Random;

namespace HotUpdate.Data
{
    [CreateAssetMenu(menuName = "Config/DepotConfig")]
    public class DepotConfig : ScriptableObject
    {
        public List<DriverDiskData> depots = new();
    }

    public class DriverDiskDataRuntime
    {
        public string DepotName;
        public int DepotId;
        public int Level;
        public string DepotIconName;
        public List<int> MaterialsId;
        public float CurLevelMaxFill;
        public float CurLevelFillValue;

        [SerializeReference] // 使用 SerializeReference 支持多态
        public DriverDiskValueData DepotDriverDiskValue = new();

        public DriverDiskDataRuntime()
        {
        }

        public DriverDiskDataRuntime(DriverDiskData driverDiskData)
        {
            DepotName = driverDiskData.depotName;
            DepotId = driverDiskData.depotId;
            Level = driverDiskData.level;
            DepotIconName = driverDiskData.depotIconName;
            MaterialsId = driverDiskData.materialsId;
            CurLevelMaxFill = driverDiskData.curLevelMaxFill;
            CurLevelFillValue = driverDiskData.curLevelFillValue;
            // 深拷贝 ValueData
            // 核心修复：空值保护 + 初始化默认值
            if (driverDiskData.depotDriverDiskValue == null)
            {
                DepotDriverDiskValue = new DriverDiskValueData();
                Debug.LogWarning($"DepotId {DepotId} 的depotValue为空，已初始化默认值");
            }
            else
            {
                DepotDriverDiskValue = new DriverDiskValueData
                {
                    driverDiskType = driverDiskData.depotDriverDiskValue.driverDiskType,
                    baseValue = driverDiskData.depotDriverDiskValue.baseValue,
                    attackPercent = driverDiskData.depotDriverDiskValue.attackPercent,
                    defensePercent = driverDiskData.depotDriverDiskValue.defensePercent,
                    healthPercent = driverDiskData.depotDriverDiskValue.healthPercent,
                    baoJiPercent = driverDiskData.depotDriverDiskValue.baoJiPercent
                };
            }
        }

        public DriverDiskDataRuntime(DriverDiskDataRuntime driverDiskData)
        {
            DepotName = driverDiskData.DepotName;
            DepotId = driverDiskData.DepotId;
            Level = driverDiskData.Level;
            DepotIconName = driverDiskData.DepotIconName;
            MaterialsId = driverDiskData.MaterialsId;
            CurLevelMaxFill = driverDiskData.CurLevelMaxFill;
            CurLevelFillValue = driverDiskData.CurLevelFillValue;
            if (driverDiskData.DepotDriverDiskValue == null)
            {
                Debug.LogWarning($"DepotDataRuntime[{DepotId}]的depotValue为null！已创建默认值");
                DepotDriverDiskValue = new DriverDiskValueData();
            }
            else
            {
                DepotDriverDiskValue = new DriverDiskValueData
                {
                    driverDiskType = driverDiskData.DepotDriverDiskValue.driverDiskType,
                    baseValue = driverDiskData.DepotDriverDiskValue.baseValue,
                    attackPercent = driverDiskData.DepotDriverDiskValue.attackPercent,
                    defensePercent = driverDiskData.DepotDriverDiskValue.defensePercent,
                    healthPercent = driverDiskData.DepotDriverDiskValue.healthPercent,
                    baoJiPercent = driverDiskData.DepotDriverDiskValue.baoJiPercent
                };
            }
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
                    // 事件化：不再直接调任务系统的 UpdateProgress，改为广播事件，由 TaskService 订阅推进
                    AppContext.Events.EventTrigger(GameEvent.DriverDiskLevelUp);
                }
                CurLevelFillValue -= CurLevelMaxFill;
                CurLevelMaxFill = (int)Random.Range(CurLevelMaxFill + 200, CurLevelMaxFill + 500);
                // 升级时提升属性
                UpgradeValue();
            }
        }

        /// <summary>
        /// 检测材料数量是否足够增加经验值。
        /// 这里只做判定，不再自己弹提示面板 —— 展示层的事交给 Controller 处理。
        /// </summary>
        /// <param name="shortageTip">不足的材料名称列表，用于 UI 展示</param>
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
            return CurLevelFillValue / CurLevelMaxFill;
        }
    }


    [Serializable]
    public class DriverDiskData
    {
        public string depotName;
        public int depotId;
        public int level;
        [Header("图标集")] public string depotIconName;
        public List<int> materialsId; // 升级所需的材料种类 id
        [Header("当前升级的最大数值")] public float curLevelMaxFill;
        [HideInInspector] public float curLevelFillValue;

        [FormerlySerializedAs("depotValue")] [SerializeReference]
        public DriverDiskValueData depotDriverDiskValue = new();
    }

    [Serializable]
    public class DriverDiskValueData
    {
        [FormerlySerializedAs("depotType")] public DriverDiskType driverDiskType;
        public int baseValue;
        public float attackPercent;
        public float defensePercent;
        public float healthPercent;
        public float baoJiPercent;
    }

    public enum DriverDiskType
    {
        Attack,
        Defense,
        Health,
        BaoJi
    }
}
