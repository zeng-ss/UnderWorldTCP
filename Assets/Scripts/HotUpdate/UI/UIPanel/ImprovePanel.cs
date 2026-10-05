using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PanelPath("Assets/Res/UI/UIPanel/ImprovePanel")]
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
    /// <summary>当前正在强化的驱动盘</summary>
    public DriverDiskDataRuntime CurrentData => _currentDriverDiskData;

    /// <summary>强化面板打开时要锁住角色操作</summary>
    public override bool BlocksGameplayInput => true;

    private DriverDiskDataRuntime _currentDriverDiskData;

    private void Start()
    {
        addExpBtn.onClick.AddListener(RaiseUpgradeRequested);
    }

    private void OnEnable()
    {
        AppContext.Events.AddEventListener(GameEvent.MaterialNumChanged, OnMaterialNumChanged);
        AppContext.Events.AddEventListener(GameEvent.PlayerDataChanged, OnPlayerDataChanged);
    }

    private void OnDisable()
    {
        AppContext.Events.RemoveEventListener(GameEvent.MaterialNumChanged, OnMaterialNumChanged);
        AppContext.Events.RemoveEventListener(GameEvent.PlayerDataChanged, OnPlayerDataChanged);
    }

    private void OnMaterialNumChanged(EventArgs args)
    {
        UpdateMaterialNum();
    }

    private void OnPlayerDataChanged(EventArgs args)
    {
        // 强化成功后属性会重算，借此顺带刷一次等级与经验条
        Refresh();
    }

    /// <summary>只上报意图：够不够材料、扣多少由 Controller 决定</summary>
    private void RaiseUpgradeRequested()
    {
        if (_currentDriverDiskData == null) return;
        AppContext.DepotUI.Upgrade(_currentDriverDiskData);
    }

    /// <summary>按当前数据整体重绘（等级、经验条、材料数量）</summary>
    public void Refresh()
    {
        if (_currentDriverDiskData == null) return;
        fillImage.fillAmount = _currentDriverDiskData.GetLevelProgress();
        fillText.text = $"{_currentDriverDiskData.curLevelFillValue}/{_currentDriverDiskData.curLevelMaxFill}";
        depotLevelText.text = $"等级：{_currentDriverDiskData.level}/15";
        UpdateMaterialNum();
    }
    // 更新面板信息
    public void UpdateData(DriverDiskDataRuntime driverDiskData)
    {
        _currentDriverDiskData = driverDiskData;
        depotName.text = driverDiskData.depotName;
        AppContext.Res.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
            sprite => { if (depotIcon) depotIcon.sprite = sprite; });
        depotLevelText.text = $"等级：{driverDiskData.level.ToString()}/15";
        depotBaseTypeText.text = GetDepotType(driverDiskData.DepotDriverDiskValue.driverDiskType);
        depotBaseText.text = driverDiskData.DepotDriverDiskValue.baseValue.ToString();
        depotAttackText.text = $"{driverDiskData.DepotDriverDiskValue.attackPercent}%";
        depotHealthText.text = $"{driverDiskData.DepotDriverDiskValue.healthPercent}%";
        depotDefenseText.text = $"{driverDiskData.DepotDriverDiskValue.defensePercent}%";
        depotBaoJiText.text = $"{driverDiskData.DepotDriverDiskValue.baoJiPercent}%";
        fillImage.fillAmount = driverDiskData.GetLevelProgress();
        fillText.text = $"{driverDiskData.curLevelFillValue}/{driverDiskData.curLevelMaxFill}";
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

    private Dictionary<int, TextMeshProUGUI> materialNumTextDict = new();
    private void CreateMaterialIcon()
    {
        if (_currentDriverDiskData == null) { return; }
        materialNumTextDict.Clear();
        for (int i = 0; i < iconsContainer.transform.childCount; i++) { Destroy(iconsContainer.transform.GetChild(i).gameObject); }
        foreach (var item in AppContext.Material.RuntimeData)
        {
            if (!_currentDriverDiskData.materialsId.Contains(item.Key)) continue;
            var obj = new GameObject("materialIcon", typeof(Image));
            obj.transform.SetParent(iconsContainer.transform);
            string iconName = AppContext.Material.RuntimeData[item.Key].materialIconName;
            Image materialIcon = obj.GetComponent<Image>();
            AppContext.Res.LoadSpriteAsync($"Res/{iconName}",
                sprite => { if (materialIcon) materialIcon.sprite = sprite; });
            // 创建数量文本
            GameObject countTextObject = new GameObject("CountText");
            countTextObject.transform.SetParent(obj.transform);
        
            // 添加 TextMeshPro组件显示数量
            TextMeshProUGUI countText = countTextObject.AddComponent<TextMeshProUGUI>();
            countText.text = AppContext.Material.GetCount(item.Key).ToString();
            countText.fontSize = 25;
            countText.enableWordWrapping = false;
            countText.color = Color.white;
            countText.alignment = TextAlignmentOptions.TopRight;
        
            // 设置数量文本的位置（右上角）
            RectTransform countRect = countTextObject.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 1);
            countRect.anchorMax = new Vector2(1, 1);
            countRect.pivot = new Vector2(1, 1);
            countRect.anchoredPosition = new Vector2(10, 10);
            countRect.sizeDelta = new Vector2(30, 30);
            materialNumTextDict.Add(item.Key, countText);
        }
    }
    private void UpdateMaterialNum()
    {
        foreach (var item in materialNumTextDict)
        {
            item.Value.text = AppContext.Material.GetCount(item.Key).ToString();
        }
    }
    /// <summary>
    /// 强化面板关闭时连带关闭角色属性面板，保持原有行为；
    /// 逻辑放在这里而不是基类，避免基类反向依赖具体子类。
    /// </summary>
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
