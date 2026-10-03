using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueOptionItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    [Header("UI组件")]
    public TMP_Text optionText;
    public Button button;
    public Image backgroundImage;
    private CanvasGroup canvasGroup;
    
    // 动画设置
    private float hoverScale = 1.1f;
    private float clickScale = 0.9f;
    private float animationDuration = 0.2f;
    [Header("悬停颜色")] public Color hoverColor = new (0.9f, 0.9f, 0.9f);

    private Color originalColor;
    private Vector3 originalScale;
    private DialogueOption optionData;
    
    private void Awake()
    {
        button.onClick.AddListener(OnOptionClicked);
        originalScale = transform.localScale;
        originalColor = backgroundImage.color;
        canvasGroup = GetComponent<CanvasGroup>();
    }
    
    /// <summary>
    /// 设置选项数据
    /// </summary>
    public void SetupOption(DialogueOption option)
    {
        optionData = option;
        optionText.text = option.optionText;
        // 入场动画
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        
        transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetDelay(option.index * 0.1f);
        
        canvasGroup.DOFade(1, 0.3f)
            .SetDelay(option.index * 0.1f);
    }
    
    /// <summary>
    /// 选项被点击
    /// </summary>
    private void OnOptionClicked()
    {
        // 点击动画
        DOTween.Sequence()
            .Append(transform.DOScale(originalScale * clickScale, animationDuration * 0.5f))
            .Append(transform.DOScale(originalScale, animationDuration * 0.5f))
            .OnComplete(() =>
            {
                // 通知对话管理器选项被选择
                DialogueManager.Instance.OnOptionSelected(optionData);
                // 按钮消失动画
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0, 0.2f)
                        .OnComplete(() => Destroy(gameObject));
                }
                else { Destroy(gameObject); }
            })
            .Play();
        
        // 播放音效
        //AudioManager.Instance.PlaySFX("option_select");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale * hoverScale, animationDuration);
        backgroundImage.DOColor(hoverColor, animationDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale, animationDuration);
        backgroundImage.DOColor(originalColor, animationDuration);
    }
}
