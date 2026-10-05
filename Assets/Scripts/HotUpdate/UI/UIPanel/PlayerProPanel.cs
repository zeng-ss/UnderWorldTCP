using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[PanelPath("Assets/Res/UI/UIPanel/PlayerProPanel")]
public class PlayerProPanel : BasePanel
{
    public Image healthFill;
    public TMP_Text healthText;
    public Image powerFill;

    public void UpdateHealthFill(float currentHealth, float maxHealth)
    {
        healthFill.DOFillAmount(Mathf.Clamp01(currentHealth / maxHealth), 0.5f);
        healthText.text = $"{currentHealth}/{maxHealth}";
    }
    
    public void UpdatePowerFill(float currentPower, float maxPower)
    {
        healthFill.DOFillAmount(Mathf.Clamp01(currentPower / maxPower), 0.5f);
    }
}
