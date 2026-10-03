using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private DepotPanel depotPanel;
    private PlayerDataPanel playerDataPanel;
    private DriverDiskDataRuntime _currentDriverDiskData;

    private void Start()
    {
        depotPanel = UIManager.Instance.GetPanel<DepotPanel>();
        UIManager.Instance.OpenPanel<PlayerDataPanel>(panel => { playerDataPanel = panel;});
        UIManager.Instance.ClosePanel<PlayerDataPanel>();
        addExpBtn.onClick.AddListener(CheckAddExp);
    }
    private void CheckAddExp()
    {
        if (!_currentDriverDiskData.CheckCanAddExp()) return;
        foreach (var materialId in _currentDriverDiskData.materialsId)
        {
            // 遍历保存的已拥有的材料数量
            if (GameManager.Instance.materialNumDict.ContainsKey(materialId))
            {
                int reduceNum = materialId == 1 ? 10 : 1;
                GameManager.Instance.materialNumDict[materialId] -= reduceNum;
            }
        }
        UpdateFillBar(200);
        UpdateMaterialNum();
    }
    // 更新面板信息
    public void UpdateData(DriverDiskDataRuntime driverDiskData)
    {
        _currentDriverDiskData = driverDiskData;
        depotName.text = driverDiskData.depotName;
        depotIcon.sprite = Resources.Load<Sprite>($"Res/{driverDiskData.depotIconName}");
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
            _ => throw new ArgumentOutOfRangeException(nameof(driverDiskType), driverDiskType, null)
        };
    }
    
    /// <summary>
    /// 更新进度
    /// </summary>
    /// <param name="increaseFill">增加的进度值</param>
    public void UpdateFillBar(float increaseFill)
    {
        // 添加经验值
        _currentDriverDiskData.AddExp(increaseFill);
        fillImage.fillAmount = _currentDriverDiskData.GetLevelProgress();
        fillText.text = $"{_currentDriverDiskData.curLevelFillValue}/{_currentDriverDiskData.curLevelMaxFill}";
        depotLevelText.text = _currentDriverDiskData.level.ToString();
        // 更新仓库信息面板
        depotPanel.UpdateDepotDes(_currentDriverDiskData);
        // 更新角色属性面板
        playerDataPanel.UpdatePlayerData(depotPanel.equipedDepotList);
        UpdateData(_currentDriverDiskData);
    }

    private Dictionary<int, TextMeshProUGUI> materialNumTextDict = new();
    private void CreateMaterialIcon()
    {
        if (_currentDriverDiskData == null) { return; }
        materialNumTextDict.Clear();
        for (int i = 0; i < iconsContainer.transform.childCount; i++) { Destroy(iconsContainer.transform.GetChild(i).gameObject); }
        foreach (var item in GameManager.Instance.materialDataRuntime)
        {
            if (!_currentDriverDiskData.materialsId.Contains(item.Key)) continue;
            var obj = new GameObject("materialIcon", typeof(Image));
            obj.transform.SetParent(iconsContainer.transform);
            obj.GetComponent<Image>().sprite = 
                Resources.Load<Sprite>($"Res/{GameManager.Instance.materialDataRuntime[item.Key].materialIconName}");
            // 创建数量文本
            GameObject countTextObject = new GameObject("CountText");
            countTextObject.transform.SetParent(obj.transform);
        
            // 添加 TextMeshPro组件显示数量
            TextMeshProUGUI countText = countTextObject.AddComponent<TextMeshProUGUI>();
            countText.text = GameManager.Instance.materialNumDict[item.Key].ToString();
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
            if (GameManager.Instance.materialNumDict.ContainsKey(item.Key))
            {
                item.Value.text = GameManager.Instance.materialNumDict[item.Key].ToString();
            }
        }
    }
    private void OnDestroy() { addExpBtn.onClick.RemoveAllListeners(); }
}
