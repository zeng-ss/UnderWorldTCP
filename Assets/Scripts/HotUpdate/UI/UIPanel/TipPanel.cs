using DG.Tweening;
using HotUpdate.Core;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    [PanelPath("TipPanel")]
    public class TipPanel : BasePanel
    {
        // 面板自带淡入淡出（ShowTip 里直接驱动 CanvasGroup），关掉通用打开动画避免抢 alpha
        protected override PanelTween.Config? AnimConfig => PanelTween.Config.None;

        public TMP_Text tipText;
        public CanvasGroup canvasGroup;
        [HideInInspector] public float showTime = 1f;
        private float _fadeDuration = 0.6f;

        protected override void Awake()
        {
            base.Awake();
            GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void ShowTip(string content)
        {
            tipText.text = content;
            canvasGroup.alpha = 0f;

            // 动画序列
            Sequence seq = DOTween.Sequence();
            seq.Append(canvasGroup.DOFade(1f, _fadeDuration))
                .AppendInterval(showTime)
                .Append(canvasGroup.DOFade(0f, _fadeDuration))
                .OnComplete(() =>
                {
                    showTime = 1;
                    AppContext.Ui.ClosePanel<TipPanel>();
                });
        }
    }
}
