using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotUpdate.Data
{
    [CreateAssetMenu(menuName = "Config/SkillConfig")]
    public class SkillConfig : ScriptableObject
    {
        public List<AttackData> skillConfigs;

        public int Count => skillConfigs?.Count ?? 0;

        public AttackData GetAttackData(int index)
        {
            if (skillConfigs == null || index < 0 || index >= skillConfigs.Count) return null;
            return skillConfigs[index];
        }
    }

    /// <summary>
    /// 连招配置表在 PlayerCtrl.skillConfigList 里的固定序号。
    /// </summary>
    public enum ComboSet
    {
        Normal = 0,
        Second = 1,
        Heavy = 2,
        Ex = 3
    }

    // 播放特效数据
    [Serializable]
    public class VFXData
    {
        [Header("攻击特效预制体")] public GameObject prefab;
        [Header("特效播放音效")] public AudioClip vfxClip;
        [Header("位移")] public Vector3 spawnPos;
        [Header("旋转")] public Vector3 spawnRot;
        [Header("大小")] public Vector3 spawnScale;
        public float spawnTime; //延迟时间
        public bool canRotate = true; // 是否可以旋转

        [Tooltip("这一刀是否顿帧。多刀连段时只勾最后一刀，前面几刀不顿，节奏才不散")]
        public bool isInHitStop;

        [Tooltip("这一刀的顿帧帧数（60fps）：2 = 轻击，6~10 = 重击，0 = 用 AttackData 的秒数 / 连段递增默认值")]
        public int hitStopFrames;
    }

    // 命中数据
    [Serializable]
    public class HitData
    {
        [Header("命中音效")] public AudioClip hitClip;
        [Header("命中效果")] public List<GameObject> hitPrefabs;
        [Header("屏幕震动")] public float screenImpulseValue;
        [Header("屏幕震动时长（秒，0 = 用相机默认）")] public float shakeDuration;
        [Header("色差效果")] public float chromaticAberrationValue;
        [Header("受击变红效果")] public float vignetteValue;
        public float damageValue; // 敌人的伤害数值 玩家的攻击数值在 PlayerData里面
    }

    // 攻击数据
    [Serializable]
    public class AttackData
    {
        public string attackAnimationName;

        [Header("连招衔接")] [Tooltip("收招动画名。留空表示这一段没有收招动画）")]
        public string endAnimationName;

        [Tooltip("这一段之后按左键衔接的下一段下标。-1 = 自增")] public int nextAttackIndex = -1;

        [Tooltip("播到「可打断时间」之后，允许被移动输入直接打断收招")] public bool canBeInterruptedByMove;

        [Tooltip("允许被移动输入打断的最早时间点（动画归一化时间）")] public float interruptibleTime = 0.5f;

        [Tooltip("这一段是「中键重击」的起手段：普攻到这一段时按中键可直接跳到最后一段")]
        public bool isHeavyEntry;

        [Tooltip("这一段是否冲刺杀")] public bool isRushAttack;

        [Header("同一次挥砍内的重复命中")]
        [Tooltip("大于 0：碰撞盒开着期间，同一目标每隔这么多秒可以再被打一次（0 = 一次挥砍只打一下）")]
        public float rehitInterval;

        [Tooltip("一次挥砍最多打几下（0 = 只受间隔限制）")]
        public int maxHitsPerSwing;

        [Header("顿帧")]
        [Tooltip("本段攻击的顿帧时长（秒）。0 = 按连段次序自动递增（第一段 0.025，每段 +0.02，最多 0.12）")]
        public float hitStopDuration;

        [Header("攻击数据")] public HitData hitData;

        [Header("多段伤害特效数据列表")] public List<VFXData> vfxDataList;
    }


    public class PlayerConfig
    {
        public ulong ID;
        public string Name;
        public bool IsReady;
        public string HeadImageName;
    }

    public class MessageData
    {
        public ulong SenderClientId;
        public string Name;
        public string Message;
        public string SendTime;
    }

    public class PlayerValueData
    {
        public ulong ID;
        public float MaxHealthValue;
        public float AttackValue = 200;
        public float DefenseValue = 10;
        public float BaoJiValue = 10;
        public float ExAttackValue = 500;
    }
}
