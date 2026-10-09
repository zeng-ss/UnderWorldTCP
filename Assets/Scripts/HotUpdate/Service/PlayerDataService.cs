using System;
using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;

namespace HotUpdate.Service
{
    /// <summary>
    /// 角色属性服务。持有「基础属性」和「基础 + 已装备驱动盘加成」后的最终属性。
    /// 对外只暴露 <see cref="Current"/>（BindableProperty），订阅它的 OnValueChanged 即可刷新。
    /// </summary>
    public class PlayerDataService
    {
        /// <summary>不含任何驱动盘加成的基础属性</summary>
        private PlayerValueData _baseValueData;

        /// <summary>当前生效属性。View / 战斗系统应订阅它。</summary>
        public BindableProperty<PlayerValueData> Current { get; } = new(new PlayerValueData());

        public void Init(PlayerValueData baseValueData) => _baseValueData = baseValueData ?? new PlayerValueData();

        /// <summary>
        /// 根据已装备的驱动盘重算当前属性。赋值 Current 即触发 OnValueChanged，订阅方自行刷新。
        /// </summary>
        public PlayerValueData ApplyEquipped(IReadOnlyList<DriverDiskDataRuntime> equipped)
        {
            PlayerValueData result = Calculate(_baseValueData, equipped);
            Current.Value = result;
            return result;
        }

        /// <summary>
        /// 属性计算公式：先乘百分比加成，再叠加固定值。
        /// 纯函数，方便写测试。
        /// </summary>
        private PlayerValueData Calculate(PlayerValueData baseValueData, IReadOnlyList<DriverDiskDataRuntime> equipped)
        {
            if (baseValueData == null) baseValueData = new PlayerValueData();

            float baoJi = baseValueData.BaoJiValue;
            float attack = baseValueData.AttackValue;
            float defense = baseValueData.DefenseValue;
            float health = baseValueData.MaxHealthValue;
            float exAttack = baseValueData.ExAttackValue;

            if (equipped == null) return MakeResult(baseValueData, health, attack, defense, baoJi, exAttack);

            foreach (var depot in equipped)
            {
                if (depot?.DepotDriverDiskValue == null) continue;
                var value = depot.DepotDriverDiskValue;

                baoJi = (float)Math.Round(baoJi * (1 + value.baoJiPercent / 100), 2);
                attack = (float)Math.Round(attack * (1 + value.attackPercent / 100), 2);
                health = (float)Math.Round(health * (1 + value.healthPercent / 100), 2);
                defense = (float)Math.Round(defense * (1 + value.defensePercent / 100), 2);
                exAttack = (float)Math.Round(exAttack * (1 + value.attackPercent / 100), 2);

                switch (value.driverDiskType)
                {
                    case DriverDiskType.Attack: attack += value.baseValue; break;
                    case DriverDiskType.BaoJi: baoJi += value.baseValue; break;
                    case DriverDiskType.Defense: defense += value.baseValue; break;
                    case DriverDiskType.Health: health += value.baseValue; break;
                    default: throw new ArgumentOutOfRangeException(nameof(value), value.driverDiskType, null);
                }
            }

            return MakeResult(baseValueData, health, attack, defense, baoJi, exAttack);
        }

        private PlayerValueData MakeResult(PlayerValueData baseValueData, float health, float attack, float defense,
            float baoJi, float exAttack)
        {
            return new PlayerValueData
            {
                ID = baseValueData.ID,
                MaxHealthValue = health,
                AttackValue = attack,
                DefenseValue = defense,
                BaoJiValue = baoJi,
                ExAttackValue = exAttack
            };
        }
    }
}
