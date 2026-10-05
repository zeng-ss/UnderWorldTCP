using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 仓库面板（纯 View）。
///
/// 只做三件事：渲染列表、渲染详情、把用户操作作为事件抛给 DepotController。
/// 不再持有「已装备列表」这类业务状态（已归于 DepotService），
/// 也不再直接调用 PlayerDataPanel / PlayerCtrl。
/// </summary>
[PanelPath("Assets/Res/UI/UIPanel/DepotPanel")]
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

    // 仓库列表容器
    public GameObject content;
    // 装备槽容器
    public List<GameObject> contentList;

    /// <summary>本面板的 Controller，由 AppContext 提供，生命周期不跟随面板</summary>
    private DepotController Ctrl => AppContext.DepotUI;

    private DriverDiskDataRuntime _currentDetail;

    /// <summary>仓库打开时要锁住角色操作</summary>
    public override bool BlocksGameplayInput => true;

    #region 生命周期

    private void OnEnable()
    {
        Refresh();
        AppContext.Events.AddEventListener(GameEvent.DepotChanged, OnDepotChanged);
        AppContext.Events.AddEventListener(GameEvent.EquippedChanged, OnEquippedChanged);
    }

    private void OnDisable()
    {
        AppContext.Events.RemoveEventListener(GameEvent.DepotChanged, OnDepotChanged);
        AppContext.Events.RemoveEventListener(GameEvent.EquippedChanged, OnEquippedChanged);
    }

    #endregion

    #region Model → View

    private void OnDepotChanged(EventArgs args)
    {
        Refresh();
    }

    private void OnEquippedChanged(EventArgs args)
    {
        RefreshEquippedIcons();
        RefreshItemStates();
        if (_currentDetail != null) ShowDetail(_currentDetail);
    }

    /// <summary>重建整个仓库列表与装备槽</summary>
    public void Refresh()
    {
        for (int i = content.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(content.transform.GetChild(i).gameObject);
        }

        foreach (var item in AppContext.Depot.Owned)
        {
            var data = item;
            ResMgr.Instance.LoadAndInstantiateAsync("Assets/Res/UI/UIItem/DepotItem", content.transform, obj =>
            {
                if (obj == null) return;
                var depotItem = obj.GetComponent<DepotItem>();
                if (depotItem == null) { Destroy(obj); return; }

                depotItem.OnEquipClicked -= RaiseEquipRequested;
                depotItem.OnEquipClicked += RaiseEquipRequested;
                depotItem.OnUnequipClicked -= RaiseUnequipRequested;
                depotItem.OnUnequipClicked += RaiseUnequipRequested;
                depotItem.OnImproveClicked -= RaiseImproveRequested;
                depotItem.OnImproveClicked += RaiseImproveRequested;
                // 悬停看详情纯粹是面板内部的渲染行为，不必惊动 Controller
                depotItem.OnHover -= ShowDetail;
                depotItem.OnHover += ShowDetail;

                depotItem.UpdateData(data);
                depotItem.SetEquipped(AppContext.Depot.IsEquipped(data));
            });
        }

        RefreshEquippedIcons();
    }

    /// <summary>只更新列表项的装备态，不重建列表（避免打断用户操作）</summary>
    private void RefreshItemStates()
    {
        foreach (Transform child in content.transform)
        {
            var depotItem = child.GetComponent<DepotItem>();
            if (depotItem == null) continue;
            depotItem.SetEquipped(AppContext.Depot.IsEquipped(depotItem.CurrentDriverDiskData));
        }
    }

    /// <summary>只重建装备槽里的图标</summary>
    private void RefreshEquippedIcons()
    {
        foreach (var slot in contentList)
        {
            if (slot == null) continue;
            for (int i = slot.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(slot.transform.GetChild(i).gameObject);
            }
        }

        foreach (var item in AppContext.Depot.Equipped)
        {
            AddEquippedIcon(item);
        }
    }

    private void AddEquippedIcon(DriverDiskDataRuntime data)
    {
        foreach (var slot in contentList)
        {
            if (slot == null || slot.transform.childCount > 0) continue;

            var obj = new GameObject("DepotObj", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(slot.transform);
            obj.transform.localPosition = Vector3.zero;

            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 150f);

            var image = obj.GetComponent<Image>();
            ResMgr.Instance.LoadSpriteAsync($"Res/{data.depotIconName}",
                sprite => { if (image) image.sprite = sprite; });

            // 右键卸下
            var trigger = obj.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(eventData =>
            {
                if (eventData is PointerEventData pointerData && pointerData.button == PointerEventData.InputButton.Right)
                {
                    RaiseUnequipRequested(data);
                }
            });
            trigger.triggers.Add(entry);
            return;
        }
    }

    /// <summary>显示某个驱动盘的详情</summary>
    public void ShowDetail(DriverDiskDataRuntime driverDiskData)
    {
        if (driverDiskData == null) return;
        _currentDetail = driverDiskData;

        depotName.text = driverDiskData.depotName;
        depotLevelText.text = driverDiskData.level.ToString();
        depotBaseTypeText.text = GetDepotType(driverDiskData.DepotDriverDiskValue.driverDiskType);
        depotBaseText.text = driverDiskData.DepotDriverDiskValue.baseValue.ToString();
        depotAttackText.text = $"{driverDiskData.DepotDriverDiskValue.attackPercent}%";
        depotHealthText.text = $"{driverDiskData.DepotDriverDiskValue.healthPercent}%";
        depotDefenseText.text = $"{driverDiskData.DepotDriverDiskValue.defensePercent}%";
        depotBaoJiText.text = $"{driverDiskData.DepotDriverDiskValue.baoJiPercent}%";

        ResMgr.Instance.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
            sprite => { if (depotIcon) depotIcon.sprite = sprite; });
    }

    #endregion

    #region View → Controller

    private void RaiseEquipRequested(DriverDiskDataRuntime data) => Ctrl.Equip(data);
    private void RaiseUnequipRequested(DriverDiskDataRuntime data) => Ctrl.Unequip(data);
    private void RaiseImproveRequested(DriverDiskDataRuntime data) => Ctrl.RequestImprove(data);

    #endregion

    private string GetDepotType(DriverDiskType driverDiskType)
    {
        return driverDiskType switch
        {
            DriverDiskType.Attack => "攻击力",
            DriverDiskType.Health => "生命值",
            DriverDiskType.Defense => "防御力",
            DriverDiskType.BaoJi => "暴击率",
            _ => "未知"
        };
    }
}
