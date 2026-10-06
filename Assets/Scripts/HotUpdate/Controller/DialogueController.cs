using DG.Tweening;
using UnityEngine;

public class DialogueController : MonoBehaviour
{
    [System.Serializable]
    public class DialogueSide
    {
        public GameObject rootObject; // 整个侧边组
        public RectTransform portraitArea; // 头像区域
        public CanvasGroup canvasGroup; // 控制淡入淡出
        [HideInInspector] public bool isActive; // 当前是否激活
    }

    [Header("左右侧配置")] public DialogueSide leftSide; // 左侧（玩家）
    public DialogueSide rightSide; // 右侧（NPC/其他人）

    [Header("动画设置")] private float _sideEnterDuration = 0.5f;
    private float _sideExitDuration = 0.3f;
    private Ease _enterEase = Ease.OutCubic;
    private Ease _exitEase = Ease.InCubic;

    private DialogueSide _currentActiveSide;
    private Tween _currentSideTween;

    private void Awake()
    {
        InitializeSides();
    }

    /// <summary>
    /// 初始化两侧
    /// </summary>
    private void InitializeSides()
    {
        // 初始隐藏两侧
        SetSideAlpha(leftSide, 0);
        SetSideAlpha(rightSide, 0);
        // 初始位置设置（屏幕外）
        SetSideStartPosition(leftSide, true);
        SetSideStartPosition(rightSide, false);
    }

    /// <summary>
    /// 设置侧边起始位置
    /// </summary>
    private void SetSideStartPosition(DialogueSide side, bool isLeft)
    {
        if (side.rootObject == null) return;
        RectTransform rect = side.rootObject.GetComponent<RectTransform>();
        if (rect == null) return;

        // 设置锚点和轴心
        rect.anchorMin = isLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
        rect.anchorMax = isLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
        rect.pivot = isLeft ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);

        // 设置起始位置（屏幕外）
        float startX = isLeft ? -rect.rect.width : rect.rect.width;
        rect.anchoredPosition = new Vector2(startX, 0);
    }

    /// <summary>
    /// 显示说话者（玩家或NPC）
    /// </summary>
    public void ShowSpeaker(bool isPlayer, System.Action onComplete = null)
    {
        DialogueSide targetSide = isPlayer ? leftSide : rightSide;
        DialogueSide otherSide = isPlayer ? rightSide : leftSide;
        // 如果已经在显示目标侧，直接返回
        if (targetSide.isActive && _currentActiveSide == targetSide)
        {
            onComplete?.Invoke();
            return;
        }

        // 停止当前动画
        _currentSideTween?.Kill();
        Sequence sequence = DOTween.Sequence();
        // 如果另一侧正在显示，先淡出
        if (otherSide.isActive && otherSide.canvasGroup != null)
        {
            sequence.Append(otherSide.canvasGroup.DOFade(0, _sideExitDuration)
                .SetEase(_exitEase));
            otherSide.isActive = false;
        }

        // 显示目标侧
        if (targetSide.rootObject != null)
        {
            RectTransform rect = targetSide.rootObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                // 进入动画：从屏幕外滑入
                sequence.Append(rect.DOAnchorPosX(0, _sideEnterDuration)
                    .SetEase(_enterEase));
            }
        }

        // 淡入
        if (targetSide.canvasGroup != null)
        {
            sequence.Join(targetSide.canvasGroup.DOFade(1, _sideEnterDuration));
        }

        // 头像强调动画
        if (targetSide.portraitArea != null)
        {
            targetSide.portraitArea.localScale = Vector3.one * 0.9f;
            sequence.Join(targetSide.portraitArea.DOScale(Vector3.one, _sideEnterDuration * 0.8f)
                .SetEase(Ease.OutBack));
        }

        sequence.OnComplete(() =>
        {
            targetSide.isActive = true;
            _currentActiveSide = targetSide;
            onComplete?.Invoke();
        });
        _currentSideTween = sequence;
        _currentSideTween.Play();
    }

    /// <summary>
    /// 隐藏所有侧边
    /// </summary>
    public void HideAllSides(System.Action onComplete = null)
    {
        // 停止当前动画
        _currentSideTween?.Kill();
        Sequence sequence = DOTween.Sequence();
        // 淡出左侧
        if (leftSide.canvasGroup != null && leftSide.canvasGroup.alpha > 0)
        {
            sequence.Join(leftSide.canvasGroup.DOFade(0, _sideExitDuration));
            // 滑出屏幕
            if (leftSide.rootObject != null)
            {
                RectTransform rect = leftSide.rootObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    sequence.Join(rect.DOAnchorPosX(-rect.rect.width, _sideExitDuration));
                }
            }
        }

        //淡出右侧
        if (rightSide.canvasGroup != null && rightSide.canvasGroup.alpha > 0)
        {
            sequence.Join(rightSide.canvasGroup.DOFade(0, _sideExitDuration));
            // 滑出屏幕
            if (rightSide.rootObject != null)
            {
                RectTransform rect = rightSide.rootObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    sequence.Join(rect.DOAnchorPosX(rect.rect.width, _sideExitDuration));
                }
            }
        }

        sequence.OnComplete(() =>
        {
            leftSide.isActive = false;
            rightSide.isActive = false;
            _currentActiveSide = null;
            onComplete?.Invoke();
        });
        sequence.Play();
    }

    /// <summary>
    /// 获取当前激活的侧边
    /// </summary>
    public DialogueSide GetActiveSide()
    {
        return _currentActiveSide;
    }

    /// <summary>
    /// 检查指定侧边是否激活
    /// </summary>
    public bool IsSideActive(bool isPlayer)
    {
        return isPlayer ? leftSide.isActive : rightSide.isActive;
    }

    /// <summary>
    /// 设置侧边透明度
    /// </summary>
    private void SetSideAlpha(DialogueSide side, float alpha)
    {
        if (side.canvasGroup != null)
        {
            side.canvasGroup.alpha = alpha;
        }
    }
}