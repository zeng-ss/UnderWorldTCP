using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 仓库面板
public class DepotPanel : BasePanel
{
    #region 详情界面UI组件

    public TMP_Text depotName;
    public Image depotIcon;
    public TMP_Text depotLevelText;
    public TMP_Text depotBaseTypeText;
    public TMP_Text depotBaseText;
    public TMP_Text depotAttackText;
    public TMP_Text depotHealthText;
    public TMP_Text depotDefenseText;
    public TMP_Text depotBaoJiText;

    #endregion
    
    // 仓库界面 UI 组件
    public GameObject content;
    // 装备界面 UI 组件
    public List<GameObject> contentList;
    // 已装备的列表
    public List<DriverDiskDataRuntime> equipedDepotList = new();

    private void OnEnable() { UpdateDepotInfo(); }
    
    #region 更新仓库

    private void UpdateDepotInfo()
    {
        for (int i = 0; i < content.transform.childCount; i++)
        {
            // 已经装备的不销毁
            if (equipedDepotList.Contains(content.transform.GetChild(i).GetComponent<DepotItem>().CurrentDriverDiskData))
                continue;
            Destroy(content.transform.GetChild(i).gameObject);
        }
        foreach (var item in GameManager.Instance.haveDepotList)
        {
            if (equipedDepotList.Contains(item)) continue;
            ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/UI/UIItem/DepotItem",content.transform,obj =>
            {
                obj.GetComponent<DepotItem>().UpdateData(item);
            });
        }
    }

    #endregion

    #region 更新详情面板
    public void UpdateDepotDes(DriverDiskDataRuntime driverDiskData)
    {
        depotName.text = driverDiskData.depotName;
        ResMgr.Instance.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
            sprite => { if (depotIcon) depotIcon.sprite = sprite; });
        depotLevelText.text = driverDiskData.level.ToString();
        depotBaseTypeText.text = GetDepotType(driverDiskData.DepotDriverDiskValue.driverDiskType);
        depotBaseText.text = driverDiskData.DepotDriverDiskValue.baseValue.ToString();
        depotAttackText.text = $"{driverDiskData.DepotDriverDiskValue.attackPercent}%";
        depotHealthText.text = $"{driverDiskData.DepotDriverDiskValue.healthPercent}%";
        depotDefenseText.text = $"{driverDiskData.DepotDriverDiskValue.defensePercent}%";
        depotBaoJiText.text = $"{driverDiskData.DepotDriverDiskValue.baoJiPercent}%";
    }
    private string GetDepotType(DriverDiskType driverDiskType)
    {
        return driverDiskType switch
        {
            DriverDiskType.Attack => "攻击力",
            DriverDiskType.Health => "生命值",
            DriverDiskType.Defense => "防御力",
            DriverDiskType.BaoJi => "暴击率",
            _ => throw new ArgumentOutOfRangeException(nameof(driverDiskType), driverDiskType, null)
        };
    }

    #endregion

    #region 装备和卸下方法
    public void EquipDepot(DriverDiskDataRuntime driverDiskData, DepotItem depotItem)
    {
        foreach (var item in contentList)
        {
            // 检查是否有子物体
            if (item.transform.childCount > 0) continue;
            GameObject obj = new GameObject("DepotObj");
            obj.transform.SetParent(item.transform);
            obj.transform.localPosition = Vector3.zero;
            Image iconImage = obj.AddComponent<Image>();
            ResMgr.Instance.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
                sprite => { if (iconImage) iconImage.sprite = sprite; });
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 150f);
            depotItem.SetEquipObj(obj);
            equipedDepotList.Add(driverDiskData);
            
            // 添加 EventTrigger
            EventTrigger trigger = obj.AddComponent<EventTrigger>();
            // 创建右键点击事件
            EventTrigger.Entry rightClickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            rightClickEntry.callback.AddListener(data => {
                PointerEventData pointerData = (PointerEventData)data;
                if (pointerData.button == PointerEventData.InputButton.Right)
                {
                    UnequipDepot(obj, driverDiskData);
                }
            });
            trigger.triggers.Add(rightClickEntry);
            break;
        }
    }
    public void UnequipDepot(GameObject obj, DriverDiskDataRuntime driverDiskData)
    {
        Destroy(obj);
        equipedDepotList.Remove(driverDiskData);
    }

    #endregion
    
}
