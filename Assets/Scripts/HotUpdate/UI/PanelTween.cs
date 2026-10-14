using System;
using DG.Tweening;
using UnityEngine;

namespace HotUpdate.UI
{
    // 面板打开动画类型
    public enum PanelAnimType
    {
        None = 0, // 不播放
        Fade = 1, // 只淡入
        Scale = 2, // 只缩放
        ScaleFade = 3, // 缩放 + 淡入
        SlideFromBottom = 4, // 从下方滑入 + 淡入
        SlideFromTop = 5,
        SlideFromLeft = 6,
        SlideFromRight = 7,
    }

    /// <summary>
    /// 面板打开动画组件
    /// 由 HotUpdate.Manager.BasePanel 在 Awake 时自动挂到面板根节点，Show() 时统一调 PlayShow，
    /// 单个面板想换风格或关掉，重写 BasePanel.AnimConfig 即可（返回 null 则沿用本组件在预制体上的配置）。
    /// </summary>
    [DisallowMultipleComponent]
    public class PanelTween : MonoBehaviour
    {
        // 一整份动画参数，供 BasePanel.AnimConfig 返回以覆盖组件上的配置
        [Serializable]
        public struct Config
        {
            public PanelAnimType type;
            public float duration;
            public Ease ease;
            public float startScale;
            public float startAlpha;
            public Vector2 slideOffset;
            public float delay;
            public bool useUnscaledTime;

            // 关闭动画
            public static Config None => new Config { type = PanelAnimType.None, duration = 0f };

            // 默认：0.22s 缩放 + 淡入，OutBack 带一点回弹
            public static Config Default => new Config
            {
                type = PanelAnimType.ScaleFade,
                duration = 0.22f,
                ease = Ease.OutBack,
                startScale = 0.94f,
                startAlpha = 0f,
                slideOffset = new Vector2(0f, -120f),
                delay = 0f,
                useUnscaledTime = true,
            };
        }

        [Header("打开动画")] [Tooltip("动画类型。None 关闭；ScaleFade 为默认的缩放淡入")] [SerializeField]
        private PanelAnimType type = PanelAnimType.ScaleFade;

        [Tooltip("动画时长（秒）")] [SerializeField] private float duration = 0.22f;

        [Tooltip("缩放 / 位移的缓动曲线，淡入固定用 OutQuad 避免透明度越界")] [SerializeField]
        private Ease ease = Ease.OutBack;

        [Tooltip("起始缩放倍数，1 表示不缩放")] [SerializeField]
        private float startScale = 0.94f;

        [Tooltip("起始透明度")] [SerializeField] private float startAlpha = 0f;

        [Tooltip("滑入类型的起始位移偏移（相对面板原始位置）")] [SerializeField]
        private Vector2 slideOffset = new Vector2(0f, -120f);

        [Tooltip("延迟播放时长（秒）")] [SerializeField]
        private float delay = 0f;

        [Tooltip("用不受 Time.timeScale 影响的时间播放（暂停游戏时动画照常）")] [SerializeField]
        private bool useUnscaledTime = true;

        [Tooltip("播放期间关掉射线检测，避免点到还没显示完的按钮")] [SerializeField]
        private bool blockRaycastWhileAnimating = true;

        // 运行时缓存
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Tween _tween;
        private bool _originCached;
        private Vector3 _originScale = Vector3.one;
        private Vector3 _originPosition;
        private float _originAlpha = 1f;
        private bool _originBlocksRaycasts = true;

        // 当前类型是否需要淡入
        private bool IsFadeType => type == PanelAnimType.Fade || type == PanelAnimType.ScaleFade ||
                                   type == PanelAnimType.SlideFromBottom || type == PanelAnimType.SlideFromTop ||
                                   type == PanelAnimType.SlideFromLeft || type == PanelAnimType.SlideFromRight;

        // 当前类型是否需要缩放
        private bool IsScaleType => type == PanelAnimType.Scale || type == PanelAnimType.ScaleFade;

        // 当前类型是否需要位移
        private bool IsSlideType => type == PanelAnimType.SlideFromBottom || type == PanelAnimType.SlideFromTop ||
                                    type == PanelAnimType.SlideFromLeft || type == PanelAnimType.SlideFromRight;

        // 面板根节点的 RectTransform
        private RectTransform SelfRect
        {
            get
            {
                _rect ??= transform as RectTransform;
                return _rect;
            }
        }

        // 把一份配置覆盖到本组件，由 BasePanel 在 Awake 时调用
        public void Apply(Config config)
        {
            type = config.type;
            duration = config.duration;
            ease = config.ease;
            startScale = config.startScale;
            startAlpha = config.startAlpha;
            slideOffset = config.slideOffset;
            delay = config.delay;
            useUnscaledTime = config.useUnscaledTime;
        }

        /// <summary>
        /// 播放打开动画。
        /// </summary>
        public bool PlayShow(Action onComplete = null)
        {
            Kill();
            if (type == PanelAnimType.None) return false;

            CacheOrigin();
            if (duration <= 0f)
            {
                RestoreOrigin();
                onComplete?.Invoke();
                return false;
            }

            Sequence sequence = DOTween.Sequence();

            if (IsFadeType)
            {
                CanvasGroup group = EnsureCanvasGroup();
                group.alpha = startAlpha;
                if (blockRaycastWhileAnimating) group.blocksRaycasts = false;
                sequence.Join(group.DOFade(_originAlpha, duration).SetEase(Ease.OutQuad));
            }

            if (IsScaleType && SelfRect != null)
            {
                SelfRect.localScale = _originScale * startScale;
                sequence.Join(SelfRect.DOScale(_originScale, duration).SetEase(ease));
            }

            if (IsSlideType && SelfRect != null)
            {
                SelfRect.anchoredPosition3D = _originPosition + (Vector3)slideOffset;
                sequence.Join(SelfRect.DOAnchorPos3D(_originPosition, duration).SetEase(ease));
            }

            // 延迟必须在 Join 之后 Prepend，用 Join 的话延迟不会作用到已加入的补间
            if (delay > 0f) sequence.PrependInterval(delay);

            _tween = sequence.SetUpdate(useUnscaledTime).OnComplete(() =>
            {
                ApplyFinalState();
                onComplete?.Invoke();
            });
            return true;
        }

        // 停止当前动画
        public void Kill()
        {
            _tween?.Kill();
            _tween = null;
        }

        // 记录面板的原始缩放 / 位置 / 透明度，首次播放时缓存一次，之后一直复用
        private void CacheOrigin()
        {
            if (_originCached) return;
            if (SelfRect is not null)
            {
                _originScale = SelfRect.localScale;
                _originPosition = SelfRect.anchoredPosition3D;
            }

            _canvasGroup ??= EnsureCanvasGroup();
            if (_canvasGroup is not null)
            {
                _originAlpha = _canvasGroup.alpha;
                _originBlocksRaycasts = _canvasGroup.blocksRaycasts;
            }

            _originCached = true;
        }

        // 动画结束时精确归位，避免缓动残留的小数误差
        private void ApplyFinalState()
        {
            if (SelfRect != null)
            {
                if (IsScaleType) SelfRect.localScale = _originScale;
                if (IsSlideType) SelfRect.anchoredPosition3D = _originPosition;
            }

            if (_canvasGroup != null)
            {
                if (IsFadeType) _canvasGroup.alpha = _originAlpha;
                if (blockRaycastWhileAnimating) _canvasGroup.blocksRaycasts = _originBlocksRaycasts;
            }
        }

        // 把被动画改过的属性原样还原
        private void RestoreOrigin()
        {
            if (!_originCached) return;

            if (SelfRect != null)
            {
                if (IsScaleType) SelfRect.localScale = _originScale;
                if (IsSlideType) SelfRect.anchoredPosition3D = _originPosition;
            }

            if (_canvasGroup != null)
            {
                if (IsFadeType) _canvasGroup.alpha = _originAlpha;
                if (blockRaycastWhileAnimating) _canvasGroup.blocksRaycasts = _originBlocksRaycasts;
            }
        }

        // 取 CanvasGroup，没有就加一个
        private CanvasGroup EnsureCanvasGroup()
        {
            CanvasGroup group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        private void OnDisable()
        {
            // 隐藏 / 销毁时中断动画并归位，避免下次打开从半途状态开始
            Kill();
            RestoreOrigin();
        }
    }
}