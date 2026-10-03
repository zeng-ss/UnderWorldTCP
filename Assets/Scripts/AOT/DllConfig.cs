using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/DllConfig")]
public class DllConfig : ScriptableObject
{
    public List<string> aot;
    public List<string> hotUpdate;
    public List<string> priorityHotUpdate;
}
