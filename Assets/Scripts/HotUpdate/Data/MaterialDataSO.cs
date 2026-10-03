using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Config/MaterialConfig")]
public class MaterialDataSO : ScriptableObject
{
    public List<MaterialData> materials = new();
}

public class MaterialDataRuntime
{
    public int id;
    public string name;
    public string materialIconName;
    public float materialValue;
    
    public MaterialDataRuntime() {}
    public MaterialDataRuntime(MaterialData materialData)
    {
        id = materialData.id;
        name = materialData.name;
        materialIconName = materialData.materialIconName;
        materialValue = materialData.materialValue;
    }
    public void AddValue(float addValue) { materialValue += addValue; }
}

[Serializable]
public class MaterialData
{
    public int id;
    public string name;
    public string materialIconName;
    [Header("材料使用所提升的经验数值")]
    public float materialValue;
}
