using DG.Tweening;
using HotUpdate.Data;
using HotUpdate.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    public class DialogueOptionItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI组件")] public TMP_Text optionText;
        public Button button;
        public Image backgroundImage;
        private CanvasGroup _canvasGroup;

        // 动画设置
        private const float HoverScale = 1.1f;
        private const float ClickScale = 0.9f;
        private const float AnimationDuration = 0.2f;
        [Header("悬停颜色")] public Color hoverColor = new(0.9f, 0.9f, 0.9f);

        private Color _originalColor;
        private Vector3 _originalScale;
        private DialogueOptionRuntime _optionData;
        private DialogueManager _dialogueManager;

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
        public void SetupOption(DialogueOptionRuntime option, DialogueManager dialogueManager)
        {
            _dialogueManager = dialogueManager;
            _optionData = option;
            optionText.text = option.OptionText;
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
                .Append(transform.DOScale(_originalScale * ClickScale, AnimationDuration * 0.5f))
                .Append(transform.DOScale(_originalScale, AnimationDuration * 0.5f))
                .OnComplete(() =>
                {
                    // 通知对话管理器选项被选择
                    _dialogueManager.OnOptionSelected(_optionData);
                    // 按钮消失动画
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.DOFade(0, 0.2f)
                            .OnComplete(() => Destroy(gameObject));
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                })
                .Play();

            // 播放音效
            //AudioManager.Instance.PlaySFX("option_select");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_originalScale * HoverScale, AnimationDuration);
            backgroundImage.DOColor(hoverColor, AnimationDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(_originalScale, AnimationDuration);
            backgroundImage.DOColor(_originalColor, AnimationDuration);
        }
    }
}