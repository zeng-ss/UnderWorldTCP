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
    private CanvasGroup _canvasGroup;
    
    // 动画设置
    private float _hoverScale = 1.1f;
    private float _clickScale = 0.9f;
    private float _animationDuration = 0.2f;
    [Header("悬停颜色")] public Color hoverColor = new (0.9f, 0.9f, 0.9f);

    private Color _originalColor;
    private Vector3 _originalScale;
    private DialogueOption _optionData;
    
    private void Awake()
    {
        button.onClick.AddListener(OnOptionClicked);
        _originalScale = transform.localScale;
        _originalColor = backgroundImage.color;
        _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    /// <summary>
    /// 设置选项数据
    /// </summary>
    public void SetupOption(DialogueOption option)
    {
        _optionData = option;
        optionText.text = option.optionText;
        // 入场动画
        transform.localScale = Vector3.zero;
        _canvasGroup.alpha = 0;
        
        transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetDelay(option.Index * 0.1f);
        
        _canvasGroup.DOFade(1, 0.3f)
            .SetDelay(option.Index * 0.1f);
    }
    
    /// <summary>
    /// 选项被点击
    /// </summary>
    private void OnOptionClicked()
    {
        // 点击动画
        DOTween.Sequence()
            .Append(transform.DOScale(_originalScale * _clickScale, _animationDuration * 0.5f))
            .Append(transform.DOScale(_originalScale, _animationDuration * 0.5f))
            .OnComplete(() =>
            {
                // 通知对话管理器选项被选择
                AppContext.Dialogue.OnOptionSelected(_optionData);
                // 按钮消失动画
                if (_canvasGroup != null)
                {
                    _canvasGroup.DOFade(0, 0.2f)
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
        transform.DOScale(_originalScale * _hoverScale, _animationDuration);
        backgroundImage.DOColor(hoverColor, _animationDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(_originalScale, _animationDuration);
        backgroundImage.DOColor(_originalColor, _animationDuration);
    }
}
