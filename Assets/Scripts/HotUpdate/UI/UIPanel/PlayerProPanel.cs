using DG.Tweening;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 游戏内 HUD：左上角头像 / 血条，底边一条面板快捷栏。
    ///
    /// 整套界面（含快捷栏）都摆在预制体上（见 PlayerProPanelBuilder），这里不再生成任何 UI；
    /// 快捷格的点击由 ShortcutButton 在 Awake 里按 PanelShortcut 总表自己接好。
    /// </summary>
    [PanelPath("PlayerProPanel")]
    public class PlayerProPanel : BasePanel
    {
        // 血条 / 能量条（预制体上绑好）
        public Image healthFill;
        public TMP_Text healthText;
        public Image powerFill;

        // HUD 是常驻的全屏信息条，打开时不需要缩放淡入
        protected override PanelTween.Config? AnimConfig => PanelTween.Config.None;

        /// <summary>刷新血条（数值由 PlayerCombat 在服务端回包后覆盖过来）</summary>
        public void UpdateHealthFill(float currentHealth, float maxHealth)
        {
            healthFill.DOFillAmount(maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f, 0.5f);
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
}