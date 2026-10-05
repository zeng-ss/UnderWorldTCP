using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 仓库里的单个驱动盘（纯 View）。
///
/// 只负责显示与上报用户意图（装备 / 卸下 / 强化 / 悬停查看详情），
/// 不再自己去找 DepotPanel、PlayerDataPanel 或者去改玩家属性。
/// </summary>
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

    /// <summary>点击「装备」</summary>
    public event Action<DriverDiskDataRuntime> OnEquipClicked;
    /// <summary>点击「卸下」</summary>
    public event Action<DriverDiskDataRuntime> OnUnequipClicked;
    /// <summary>点击「强化」</summary>
    public event Action<DriverDiskDataRuntime> OnImproveClicked;
    /// <summary>鼠标悬停，请求展示详情</summary>
    public event Action<DriverDiskDataRuntime> OnHover;

    [NonSerialized] public DriverDiskDataRuntime CurrentDriverDiskData;

    private bool _isHover;
    private bool _isEquipped;

    private void Start()
    {
        equipBtn.onClick.AddListener(OnEquipBtnClick);
        improveBtn.onClick.AddListener(OnImproveBtnClick);
    }

    private void OnEnable()
    {
        // 不再用 Update 每帧轮询输入，改为在 InputManager 上一次性注册
        InputManager.Instance.RegisterKeyDown(KeyCode.R, ClosePopup);
        InputManager.Instance.RegisterMouseDown(0, ClosePopupIfClickedOutside);
    }

    private void OnDisable()
    {
        InputManager.Instance.UnregisterKeyDown(KeyCode.R, ClosePopup);
        InputManager.Instance.UnregisterMouseDown(0, ClosePopupIfClickedOutside);
        improveOrEquipPanel.SetActive(false);
    }

    #region 渲染

    public void UpdateData(DriverDiskDataRuntime driverDiskData)
    {
        CurrentDriverDiskData = driverDiskData;
        AppContext.Res.LoadSpriteAsync($"Res/{driverDiskData.depotIconName}",
            sprite => { if (depotItemImage) depotItemImage.sprite = sprite; });
    }

    /// <summary>由 DepotPanel 在装备状态变化时同步过来</summary>
    public void SetEquipped(bool equipped)
    {
        _isEquipped = equipped;
        if (equipBtnText != null) equipBtnText.text = equipped ? "卸下" : "装备";
        if (equipTipImage != null) equipTipImage.gameObject.SetActive(equipped);
    }

    #endregion

    #region 交互

    private void OnEquipBtnClick()
    {
        if (CurrentDriverDiskData == null) return;
        improveOrEquipPanel.SetActive(false);

        if (_isEquipped) OnUnequipClicked?.Invoke(CurrentDriverDiskData);
        else OnEquipClicked?.Invoke(CurrentDriverDiskData);
    }

    private void OnImproveBtnClick()
    {
        improveOrEquipPanel.SetActive(false);
        if (CurrentDriverDiskData != null) OnImproveClicked?.Invoke(CurrentDriverDiskData);
    }

    /// <summary>右键呼出操作面板</summary>
    private void OpenPopupAtMouse()
    {
        var rect = improveOrEquipPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(),
                Input.mousePosition, null, out Vector2 localPos))
        {
            improveOrEquipPanel.transform.localPosition = localPos;
            improveOrEquipPanel.SetActive(true);
        }
    }

    private void ClosePopup()
    {
        if (improveOrEquipPanel.activeSelf) improveOrEquipPanel.SetActive(false);
    }

    private void ClosePopupIfClickedOutside()
    {
        if (!improveOrEquipPanel.activeSelf) return;
        if (!IsPointerOverImprovePanel()) improveOrEquipPanel.SetActive(false);
    }

    private bool IsPointerOverImprovePanel()
    {
        if (!improveOrEquipPanel.activeSelf) return false;
        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Any(result => result.gameObject == improveOrEquipPanel);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tipKuangImage.enabled = true;
        _isHover = true;
        if (CurrentDriverDiskData != null) OnHover?.Invoke(CurrentDriverDiskData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tipKuangImage.enabled = false;
        _isHover = false;
    }

    #endregion

    private void OnDestroy()
    {
        equipBtn.onClick.RemoveAllListeners();
        improveBtn.onClick.RemoveAllListeners();
    }
}
