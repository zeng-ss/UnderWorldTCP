using System.Collections.Generic;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.Manager;
using HotUpdate.UI.UIItem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 仓库面板
    /// </summary>
    [PanelPath("DepotPanel")]
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

        // 本面板的 Controller
        private readonly DepotController _ctrl = new();

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
        private void Refresh()
        {
            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }

            foreach (var item in AppContext.Depot.Owned)
            {
                var data = item;
                AppContext.Res.LoadAndInstantiateAsync("DepotItem", content.transform, obj =>
                {
                    if (obj == null) return;
                    var depotItem = obj.GetComponent<DepotItem>();
                    if (depotItem == null)
                    {
                        Destroy(obj);
                        return;
                    }

                    // 悬停看详情纯粹是面板内部的渲染行为，不必动 Controller
                    depotItem.OnHover -= ShowDetail;
                    depotItem.OnHover += ShowDetail;

                    depotItem.UpdateData(data, _ctrl);
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
                AppContext.Res.LoadSpriteAsync($"Res/{data.DepotIconName}",
                    sprite =>
                    {
                        if (image) image.sprite = sprite;
                    });

                // 右键卸下
                var trigger = obj.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(eventData =>
                {
                    if (eventData is PointerEventData { button: PointerEventData.InputButton.Right })
                    {
                        _ctrl.Unequip(data);
                    }
                });
                trigger.triggers.Add(entry);
                return;
            }
        }

        /// <summary>显示某个驱动盘的详情</summary>
        private void ShowDetail(DriverDiskDataRuntime driverDiskData)
        {
            if (driverDiskData == null) return;
            _currentDetail = driverDiskData;

            depotName.text = driverDiskData.DepotName;
            depotLevelText.text = driverDiskData.Level.ToString();
            depotBaseTypeText.text = GetDepotType(driverDiskData.DepotDriverDiskValue.driverDiskType);
            depotBaseText.text = driverDiskData.DepotDriverDiskValue.baseValue.ToString();
            depotAttackText.text = $"{driverDiskData.DepotDriverDiskValue.attackPercent}%";
            depotHealthText.text = $"{driverDiskData.DepotDriverDiskValue.healthPercent}%";
            depotDefenseText.text = $"{driverDiskData.DepotDriverDiskValue.defensePercent}%";
            depotBaoJiText.text = $"{driverDiskData.DepotDriverDiskValue.baoJiPercent}%";

            AppContext.Res.LoadSpriteAsync($"Res/{driverDiskData.DepotIconName}",
                sprite =>
                {
                    if (depotIcon) depotIcon.sprite = sprite;
                });
        }

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
}
