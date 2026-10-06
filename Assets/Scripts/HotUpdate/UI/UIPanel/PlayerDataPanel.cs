using TMPro;
using UnityEngine;

/// <summary>
/// 角色属性面板（纯 View）。
///
/// 只订阅 PlayerDataService.Current 并渲染，不再自己去找 PlayerCtrl 算属性、也不再改玩家数据。
/// 原先靠判断 attackText.text == "？？？" 来区分是否首次初始化，这个开关现在改由数据层驱动。
/// </summary>
[PanelPath("PlayerDataPanel")]
public class PlayerDataPanel : BasePanel
{
    public TMP_Text attackText;
    public TMP_Text healthText;
    public TMP_Text defenseText;
    public TMP_Text baoJiText;
    public TMP_Text exAttackText;

    /// <summary>属性面板打开时要锁住角色操作</summary>
    public override bool BlocksGameplayInput => true;

    private void OnEnable()
    {
        AppContext.PlayerData.Current.OnValueChanged -= Render;
        AppContext.PlayerData.Current.OnValueChanged += Render;
        Render(AppContext.PlayerData.Current.Value);
    }

    private void OnDisable()
    {
        AppContext.PlayerData.Current.OnValueChanged -= Render;
    }

    private void Render(PlayerValueData valueData)
    {
        if (valueData == null) return;

        attackText.text = $"{valueData.AttackValue}";
        healthText.text = $"{valueData.MaxHealthValue}";
        defenseText.text = $"{valueData.DefenseValue}";
        baoJiText.text = $"{valueData.BaoJiValue}%";
        exAttackText.text = $"{valueData.ExAttackValue}";
    }
}
