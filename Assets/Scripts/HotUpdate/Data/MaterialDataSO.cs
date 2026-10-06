using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Config/MaterialConfig")]
public class MaterialDataSo : ScriptableObject
{
    public List<MaterialData> materials = new();
}

public class MaterialDataRuntime
{
    public int ID;
    public string Name;
    public string MaterialIconName;
    public float MaterialValue;
    
    public MaterialDataRuntime() {}
    public MaterialDataRuntime(MaterialData materialData)
    {
        ID = materialData.id;
        Name = materialData.name;
        MaterialIconName = materialData.materialIconName;
        MaterialValue = materialData.materialValue;
    }
    public void AddValue(float addValue) { MaterialValue += addValue; }
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
