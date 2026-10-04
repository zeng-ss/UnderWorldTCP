using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 仓库的磁盘
public class DepotItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // UI组件
    public Image depotItemImage;
    public Image equipTipImage;
    public TMP_Text equipBtnText;
    [Header("装备强化面板")]
    public GameObject improveOrEquipPanel;
    public Button improveBtn;
    public Button equipBtn;
    [Header("提示框")]
    public Image tipKuangImage;
    // 数据
    private DepotPanel depotPanel;
    [NonSerialized]
    public DriverDiskDataRuntime CurrentDriverDiskData;
    private GameObject equippedObj; 
    private bool isHover;
    private PlayerDataPanel playerDataPanel;

    private void Start()
    {
        depotPanel = FindObjectOfType<DepotPanel>();
        equipBtn.onClick.AddListener(EquipOrUnequip);
        improveBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenPanel<ImprovePanel>(panel => { panel.UpdateData(CurrentDriverDiskData);});
            improveOrEquipPanel.gameObject.SetActive(false);
        });
        improveOrEquipPanel.GetComponent<Canvas>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && improveOrEquipPanel.activeSelf)
        {
            improveOrEquipPanel.gameObject.SetActive(false);
        }
        // 左键点击面板外区域关闭面板
        if (Input.GetMouseButtonDown(0) && improveOrEquipPanel.activeSelf)
        {
            if (!IsPointerOverImprovePanel())
            {
                improveOrEquipPanel.gameObject.SetActive(false);
            }
        }
        if (!isHover || improveOrEquipPanel.activeSelf) return;
        if (Input.GetMouseButtonDown(1)) { OpenImproveOrEquipPanel(); }
    }

    #region 装备和装备面板相关

    // 右键打开 ImproveOrEquipPanel
    private void OpenImproveOrEquipPanel()
    {
        GameObject otherImproveOrEquipPanel = GameObject.Find("improveOrEquipPanel");
        otherImproveOrEquipPanel?.gameObject.SetActive(false);
        RectTransform rect = improveOrEquipPanel.GetComponent<RectTransform>();
        // 设置锚点和枢轴点为左上角
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        // 将面板位置设置为鼠标位置
        Vector2 mouseScreenPos = Input.mousePosition;
        // 将鼠标坐标转成【item】的本地坐标（而非 Canvas）
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), mouseScreenPos, null, out Vector2 localPosInItem))
        {
            // 设置面板相对于item的本地位置，左上角对齐鼠标
            improveOrEquipPanel.transform.localPosition = localPosInItem;
            improveOrEquipPanel.gameObject.SetActive(true);
        }
    }

    // 装备驱动盘
    private void EquipOrUnequip()
    {
        if (equipBtnText.text == "装备")
        {
            // 装备驱动盘
            depotPanel.EquipDepot(CurrentDriverDiskData, this);
            equipTipImage.gameObject.SetActive(true);
            equipBtnText.text = "卸下";
        }
        else
        {
            // 卸下驱动盘
            depotPanel.UnequipDepot(equippedObj, CurrentDriverDiskData);
            equipTipImage.gameObject.SetActive(false);
            equipBtnText.text = "装备";
        }
        if (playerDataPanel == null)
        {
            playerDataPanel = FindObjectOfType<PlayerDataPanel>();
            if (playerDataPanel == null)
            {
                UIManager.Instance.OpenPanel<PlayerDataPanel>(panel =>
                {
                    playerDataPanel = panel;
                    playerDataPanel.UpdatePlayerData(depotPanel.equipedDepotList);
                });
            }
            else playerDataPanel.UpdatePlayerData(depotPanel.equipedDepotList);
        }
        else playerDataPanel.UpdatePlayerData(depotPanel.equipedDepotList);
    }

    // 设置对应的装备 obj
    public void SetEquipObj(GameObject obj) { equippedObj = obj; }
    
    // 判断鼠标是否点击在面板内
    private bool IsPointerOverImprovePanel()
    {
        if (!improveOrEquipPanel.activeSelf) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        // 检测鼠标是否在面板 UI上
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Any(result => result.gameObject == improveOrEquipPanel);
    }

    #endregion
    
    public void UpdateData(DriverDiskDataRuntime driverDiskData)
    {
        CurrentDriverDiskData = driverDiskData;
        ResMgr.Instance.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
            sprite => { if (depotItemImage) depotItemImage.sprite = sprite; });
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        tipKuangImage.enabled = true;
        depotPanel.UpdateDepotDes(CurrentDriverDiskData);
        isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tipKuangImage.enabled = false;
        isHover = false;
    }
    private void OnDestroy()
    {
        equipBtn.onClick.RemoveAllListeners();
        improveBtn.onClick.RemoveAllListeners();
    }
}
