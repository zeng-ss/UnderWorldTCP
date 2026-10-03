using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TipPanel : BasePanel
{
    public TMP_Text tipText;
    public CanvasGroup canvasGroup;
    [HideInInspector] public float showTime = 1f;
    private float fadeDuration = 0.6f;

    public override void Awake()
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
        seq.Append(canvasGroup.DOFade(1f, fadeDuration))
            .AppendInterval(showTime)
            .Append(canvasGroup.DOFade(0f, fadeDuration))
            .OnComplete(() =>
            {
                showTime = 1;
                UIManager.Instance.ClosePanel<TipPanel>();
            });
    }
}
