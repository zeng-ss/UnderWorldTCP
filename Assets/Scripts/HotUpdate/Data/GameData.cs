using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Config/SkillConfig")]
public class SkillConfig : ScriptableObject
{
    public List<AttackData> skillConfigs;
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
    public bool isInHitStop; // 是否需要顿帧
}

// 命中数据
[Serializable]
public class HitData
{
    [Header("命中音效")] public AudioClip hitClip;
    [Header("命中效果")] public List<GameObject> hitPrefabs;
    [Header("屏幕震动")] public float screenImpulseValue;
    [Header("色差效果")] public float chromaticAberrationValue;
    [Header("受击变红效果")] public float vignetteValue;
    public float damageValue; // 敌人的伤害数值 玩家的攻击数值在 PlayerData里面
}

// 攻击数据
[Serializable]
public class AttackData
{
    public string attackAnimationName;

    [FormerlySerializedAs("HitData")] [Header("攻击数据")]
    public HitData hitData;

    [FormerlySerializedAs("VFXDataList")] [Header("多段伤害特效数据列表")]
    public List<VFXData> vfxDataList;
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