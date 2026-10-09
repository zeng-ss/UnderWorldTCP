using System.Collections.Generic;
using HotUpdate.Controller;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("ImprovePanel")]
    public class ImprovePanel : BasePanel
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
        public Button addExpBtn;
        public Image fillImage;
        public TMP_Text fillText;
        public GameObject iconsContainer;

        #endregion

        // 本面板的 Controller
        private DepotController _ctrl;

        /// <summary>强化面板打开时要锁住角色操作</summary>
        public override bool BlocksGameplayInput => true;

        private DriverDiskDataRuntime _currentDriverDiskData;

        private void Start()
        {
            addExpBtn.onClick.AddListener(RaiseUpgradeRequested);
        }

        private void OnEnable()
        {
            AppContext.PlayerData.Current.OnValueChanged -= OnPlayerDataChanged;
            AppContext.PlayerData.Current.OnValueChanged += OnPlayerDataChanged;
        }

        private void OnDisable()
        {
            AppContext.PlayerData.Current.OnValueChanged -= OnPlayerDataChanged;
        }

        private void OnPlayerDataChanged(PlayerValueData valueData)
        {
            // 强化成功后属性会重算，借此顺带刷一次等级与经验条
            Refresh();
        }

        /// <summary>只上报意图：够不够材料、扣多少由 Controller 决定</summary>
        private void RaiseUpgradeRequested()
        {
            if (_currentDriverDiskData == null) return;
            _ctrl.Upgrade(_currentDriverDiskData);
        }

        /// <summary>按当前数据整体重绘（等级、经验条、材料数量）</summary>
        private void Refresh()
        {
            if (_currentDriverDiskData == null) return;
            fillImage.fillAmount = _currentDriverDiskData.GetLevelProgress();
            fillText.text = $"{_currentDriverDiskData.CurLevelFillValue}/{_currentDriverDiskData.CurLevelMaxFill}";
            depotLevelText.text = $"等级：{_currentDriverDiskData.Level}/15";
            UpdateMaterialNum();
        }

        // 更新面板信息
        public void UpdateData(DriverDiskDataRuntime driverDiskData, DepotController ctrl)
        {
            _ctrl = ctrl;
            _currentDriverDiskData = driverDiskData;
            depotName.text = driverDiskData.DepotName;
            AppContext.Res.LoadSpriteAsync($"Res/{driverDiskData.DepotIconName}",
                sprite =>
                {
                    if (depotIcon) depotIcon.sprite = sprite;
                });
            depotLevelText.text = $"等级：{driverDiskData.Level.ToString()}/15";
            depotBaseTypeText.text = GetDepotType(driverDiskData.DepotDriverDiskValue.driverDiskType);
            depotBaseText.text = driverDiskData.DepotDriverDiskValue.baseValue.ToString();
            depotAttackText.text = $"{driverDiskData.DepotDriverDiskValue.attackPercent}%";
            depotHealthText.text = $"{driverDiskData.DepotDriverDiskValue.healthPercent}%";
            depotDefenseText.text = $"{driverDiskData.DepotDriverDiskValue.defensePercent}%";
            depotBaoJiText.text = $"{driverDiskData.DepotDriverDiskValue.baoJiPercent}%";
            fillImage.fillAmount = driverDiskData.GetLevelProgress();
            fillText.text = $"{driverDiskData.CurLevelFillValue}/{driverDiskData.CurLevelMaxFill}";
            CreateMaterialIcon();
        }

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

        private readonly Dictionary<int, TextMeshProUGUI> _materialNumTextDict = new();

        private void CreateMaterialIcon()
        {
            if (_currentDriverDiskData == null)
            {
                return;
            }

            _materialNumTextDict.Clear();
            for (int i = 0; i < iconsContainer.transform.childCount; i++)
            {
                Destroy(iconsContainer.transform.GetChild(i).gameObject);
            }

            foreach (var item in AppContext.Material.RuntimeData)
            {
                if (!_currentDriverDiskData.MaterialsId.Contains(item.Key)) continue;
                var obj = new GameObject("materialIcon", typeof(Image));
                obj.transform.SetParent(iconsContainer.transform);
                string iconName = AppContext.Material.RuntimeData[item.Key].MaterialIconName;
                Image materialIcon = obj.GetComponent<Image>();
                AppContext.Res.LoadSpriteAsync($"Res/{iconName}",
                    sprite =>
                    {
                        if (materialIcon) materialIcon.sprite = sprite;
                    });
                // 创建数量文本
                GameObject countTextObject = new GameObject("CountText");
                countTextObject.transform.SetParent(obj.transform);

                // 添加 TextMeshPro组件显示数量
                TextMeshProUGUI countText = countTextObject.AddComponent<TextMeshProUGUI>();
                countText.text = AppContext.Material.GetCount(item.Key).ToString();
                countText.fontSize = 25;
                countText.textWrappingMode = TextWrappingModes.NoWrap;
                countText.color = Color.white;
                countText.alignment = TextAlignmentOptions.TopRight;

                // 设置数量文本的位置（右上角）
                RectTransform countRect = countTextObject.GetComponent<RectTransform>();
                countRect.anchorMin = new Vector2(1, 1);
                countRect.anchorMax = new Vector2(1, 1);
                countRect.pivot = new Vector2(1, 1);
                countRect.anchoredPosition = new Vector2(10, 10);
                countRect.sizeDelta = new Vector2(30, 30);
                _materialNumTextDict.Add(item.Key, countText);
            }
        }

        /// <summary>材料数量变化时由 MaterialService 直接调用（同模块 data→view）</summary>
        public void UpdateMaterialNum()
        {
            foreach (var item in _materialNumTextDict)
            {
                item.Value.text = AppContext.Material.GetCount(item.Key).ToString();
            }
        }

        protected override void OnCloseClicked()
        {
            AppContext.Ui.ClosePanel<ImprovePanel>();
            AppContext.Ui.ClosePanel<PlayerDataPanel>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            addExpBtn.onClick.RemoveAllListeners();
        }
    }
}
