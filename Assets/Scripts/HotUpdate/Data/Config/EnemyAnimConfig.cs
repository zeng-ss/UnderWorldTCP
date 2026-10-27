using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
    /// <summary>
    /// 敌人动画配置：每个动画一条，解决「不同招式攻击距离 / 要不要根运动 / 追击速度」不一样的问题。
    ///
    /// 在 Assets 里右键 Create → Config → EnemyAnimConfig 建一份，挂到敌人预制体的 EnemyCtrl 上；
    /// 不挂也能跑：没配的动画走 <see cref="Default"/> 里的约定值。
    /// 注意：命中判定不在这张表里 —— 由武器碰撞盒决定（动画事件开合）。
    /// </summary>
    [CreateAssetMenu(menuName = "Config/EnemyAnimConfig")]
    public class EnemyAnimConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string anim = "Attack01";

            [Tooltip("这一招的判定距离（米）：敌人再远就够不着，状态机据此选招")]
            public float attackRange = 2.5f;

            [Tooltip("播这个动画时是否吃动画根运动")]
            public bool useRootMotion = true;

            [Tooltip("不吃根运动时，这段动画期间的移动速度（0 = 原地不动）")]
            public float moveSpeed;

            [Tooltip("这招打完到下一招的最短间隔（秒）")]
            public float cooldown = 1.2f;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, Entry> _map;

        public Entry Get(string anim)
        {
            if (string.IsNullOrEmpty(anim)) return Default("Attack01");

            if (_map == null) BuildMap();
            return _map.TryGetValue(anim, out Entry entry) ? entry : Default(anim);
        }

        public bool UseRootMotion(string anim) => Get(anim).useRootMotion;

        private void BuildMap()
        {
            _map = new Dictionary<string, Entry>();
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.anim)) continue;
                _map[entry.anim] = entry;
            }
        }

        /// <summary>没用资产时的默认值：走路/冲刺吃根运动，普通攻击原地打，冲刺攻击略远一点</summary>
        public static Entry Default(string anim)
        {
            Entry entry = new Entry { anim = anim };

            switch (anim)
            {
                case "WalkF":
                    entry.attackRange = 0f;
                    entry.useRootMotion = true;
                    entry.moveSpeed = 1.6f;
                    entry.cooldown = 0f;
                    break;

                case "Attack05Start":
                    entry.attackRange = 0f;
                    entry.useRootMotion = true;
                    entry.moveSpeed = 0f;
                    entry.cooldown = 0f;
                    break;

                case "Attack05Run":
                    entry.attackRange = 0f;
                    entry.useRootMotion = true;
                    entry.moveSpeed = 5f;
                    entry.cooldown = 0f;
                    break;

                case "Attack05":
                    entry.attackRange = 3.2f;
                    entry.useRootMotion = false;
                    entry.moveSpeed = 0f;
                    entry.cooldown = 1.5f;
                    break;

                default:
                    entry.attackRange = 2.5f;
                    entry.useRootMotion = false;
                    entry.moveSpeed = 0f;
                    entry.cooldown = 1.2f;
                    break;
            }

            return entry;
        }

        // Inspector 右键 → 填充默认条目，省得手打
        [ContextMenu("填充默认条目（WalkF / Attack01~05）")]
        private void FillDefaults()
        {
            entries = new List<Entry>
            {
                Default("WalkF"),
                Default("Attack01"),
                Default("Attack02"),
                Default("Attack03"),
                Default("Attack04"),
                Default("Attack05Start"),
                Default("Attack05Run"),
                Default("Attack05"),
            };
        }
    }
}