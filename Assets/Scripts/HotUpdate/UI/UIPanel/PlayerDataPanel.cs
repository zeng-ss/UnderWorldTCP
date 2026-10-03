using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerDataPanel : BasePanel
{
    public TMP_Text attackText;
    public TMP_Text healthText;
    public TMP_Text defenseText;
    public TMP_Text baoJiText;
    public TMP_Text exAttackText;

    private void OnEnable()
    {
        if (attackText.text == "？？？")
        {
            attackText.text = $"{GameManager.Instance.playerBaseData.attackValue}";
            healthText.text = $"{GameManager.Instance.playerBaseData.maxHealthValue}";
            defenseText.text = $"{GameManager.Instance.playerBaseData.defenseValue}";
            baoJiText.text = $"{GameManager.Instance.playerBaseData.baoJiValue}%";
            exAttackText.text = $"{GameManager.Instance.playerBaseData.exAttackValue}";
        }
    }

    public void UpdatePlayerData(List<DriverDiskDataRuntime> depotsData)
    {
        PlayerCtrl player = FindObjectOfType<PlayerCtrl>();
        if (!player) return;
        var newPlayerData = player.CalculatePlayerData(depotsData);
        player.UpdatePlayerData(newPlayerData);

        attackText.text = $"{newPlayerData.attackValue}";
        healthText.text = $"{newPlayerData.maxHealthValue}";
        defenseText.text = $"{newPlayerData.defenseValue}";
        baoJiText.text = $"{newPlayerData.baoJiValue}%";
        exAttackText.text = $"{newPlayerData.exAttackValue}";
    }
}
