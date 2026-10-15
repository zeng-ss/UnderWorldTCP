using DG.Tweening;
using HotUpdate.Data;
using HotUpdate.Manager;
using HotUpdate.Player;
using HotUpdate.UI.UIItem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("PlayerProPanel")]
    public class PlayerProPanel : BasePanel
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image powerFill;
        [SerializeField] private ShortcutBtnConfig shortcutBtnConfig;
        [SerializeField] private Transform shortcutBtnParent;

        // HUD 是常驻的全屏信息条，打开时不需要缩放淡入
        protected override PanelTween.Config? AnimConfig => PanelTween.Config.None;

        protected override void Awake()
        {
            base.Awake();
            if (shortcutBtnConfig == null)
            {
                Debug.LogWarning("[PlayerProPanel] 没绑 shortcutBtnConfig，快捷栏不生成。");
                return;
            }

            // 快捷格按 Config 生成：加 / 删 / 改按钮只动 ShortcutBtnConfig.asset，不用改代码和预制体
            foreach (ShortcutBtnData data in shortcutBtnConfig.shortcutButtonsConfig)
            {
                AppContext.Res.LoadAndInstantiateAsync("HUDSlotItem", shortcutBtnParent, obj =>
                {
                    obj?.GetComponent<ShortcutButton>()?.Init(data);
                });
            }
        }

        public override void Show()
        {
            base.Show();

            // 角色和 HUD 都是异步加载的，HUD 可能晚于「角色属性初始化」才被打开，
            // 那样血条会停在预制体的初始值。打开时主动拉一次当前血量。
            if (!AppContext.IsAlive) return;

            PlayerCtrl player = FindAnyObjectByType<PlayerCtrl>();
            if (player != null) UpdateHealthFill(player.Core.Health, player.Core.MaxHealth);
        }

        // 刷新血条
        public void UpdateHealthFill(float currentHealth, float maxHealth)
        {
            float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

            if (healthFill != null) healthFill.DOFillAmount(ratio, 0.5f);
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
}