using System;
using DG.Tweening;
using UnityEngine;

namespace HotUpdate.UI
{
    // 面板动画类型
    public enum PanelAnimType
    {
        None = 0, // 不播放
        Fade = 1, // 只淡入 / 淡出
        Scale = 2, // 只缩放
        ScaleFade = 3, // 缩放 + 淡入淡出
        SlideFromBottom = 4, // 从下方滑入 / 向下滑出
        SlideFromTop = 5,
        SlideFromLeft = 6,
        SlideFromRight = 7,
    }

    /// <summary>
    /// 面板开关动画组件。
    /// 由 HotUpdate.Manager.BasePanel 在 Awake 时自动挂到面板根节点，Show / Hide 时统一调 PlayShow / PlayHide；
    /// 单个面板要换风格或关掉，重写 BasePanel.AnimConfig 即可（返回 null 表示沿用本组件在预制体上的配置）。
    ///
    /// 注意：本组件里所有 UnityEngine.Object（CanvasGroup / RectTransform）的判空都只能用
    /// `== null` / `!= null`（Unity 重载过），不能写 `??`、`??=`、`is null`、`is not null`——
    /// 后者走的是 .NET 原生引用比较，识别不出 Unity 的「假空对象」。
    /// 编辑器下 GetComponent 找不到组件时返回的正是假空对象，会让判空失效并抛 MissingComponentException。
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

            // 关闭动画参数：<=0 表示沿用打开动画的对应参数
            public float hideDuration;
            public float hideScale;
            public float hideAlpha;

            // 关掉动画
            public static Config None => new Config { type = PanelAnimType.None, duration = 0f, hideDuration = 0f };

            // 默认：0.22s 缩放淡入 + 0.16s 反向缩放淡出
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
                hideDuration = 0.16f,
                hideScale = 0.94f,
                hideAlpha = 0f,
            };
        }

        [Header("打开动画")]
        [Tooltip("动画类型。None 关闭；ScaleFade 为默认的缩放淡入")]
        [SerializeField] private PanelAnimType type = PanelAnimType.ScaleFade;

        [Tooltip("动画时长（秒）")]
        [SerializeField] private float duration = 0.22f;

        [Tooltip("缩放 / 位移的缓动曲线，淡入淡出固定用 Quad 避免透明度越界")]
        [SerializeField] private Ease ease = Ease.OutBack;

        [Tooltip("起始缩放倍数，1 表示不缩放")]
        [SerializeField] private float startScale = 0.94f;

        [Tooltip("起始透明度")]
        [SerializeField] private float startAlpha = 0f;

        [Tooltip("滑入类型的起始位移偏移（相对面板原始位置）")]
        [SerializeField] private Vector2 slideOffset = new Vector2(0f, -120f);

        [Tooltip("延迟播放时长（秒）")]
        [SerializeField] private float delay = 0f;

        [Tooltip("用不受 Time.timeScale 影响的时间播放（暂停游戏时动画照常）")]
        [SerializeField] private bool useUnscaledTime = true;

        [Tooltip("播放期间关掉射线检测，避免点到还没显示完的按钮")]
        [SerializeField] private bool blockRaycastWhileAnimating = true;

        [Header("关闭动画")]
        [Tooltip("关闭时长（秒），<=0 表示沿用打开动画时长")]
        [SerializeField] private float hideDuration = 0.16f;

        [Tooltip("关闭时缩小到的倍数，<=0 表示沿用起始缩放倍数")]
        [SerializeField] private float hideScale = 0.94f;

        [Tooltip("关闭时淡到的透明度")]
        [SerializeField] private float hideAlpha = 0f;

        // 运行时缓存
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Tween _tween;
        private bool _originCached;
        private Vector3 _originScale = Vector3.one;
        private Vector3 _originPosition;
        private float _originAlpha = 1f;
        private bool _originBlocksRaycasts = true;

        // 当前类型是否需要淡入淡出
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
                if (_rect == null) _rect = transform as RectTransform;
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
            hideDuration = config.hideDuration;
            hideScale = config.hideScale;
            hideAlpha = config.hideAlpha;
        }

        /// <summary>
        /// 播放打开动画。返回 true 表示动画正在播（播完回调）。
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
                if (group != null)
                {
                    group.alpha = startAlpha;
                    if (blockRaycastWhileAnimating) group.blocksRaycasts = false;
                    sequence.Join(group.DOFade(_originAlpha, duration).SetEase(Ease.OutQuad));
                }
            }

            RectTransform rect = SelfRect;
            if (IsScaleType && rect != null)
            {
                rect.localScale = _originScale * startScale;
                sequence.Join(rect.DOScale(_originScale, duration).SetEase(ease));
            }

            if (IsSlideType && rect != null)
            {
                rect.anchoredPosition3D = _originPosition + (Vector3)slideOffset;
                sequence.Join(rect.DOAnchorPos3D(_originPosition, duration).SetEase(ease));
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

        /// <summary>
        /// 播放关闭动画（打开动画的反向：缩回 + 淡出 / 滑出）。返回 true 表示动画正在播（播完回调）。
        /// 关闭终态由 hideScale / hideAlpha 决定，位移方向与滑入方向相反。
        /// </summary>
        public bool PlayHide(Action onComplete = null)
        {
            Kill();
            if (type == PanelAnimType.None)
            {
                onComplete?.Invoke();
                return false;
            }

            CacheOrigin();

            float dur = hideDuration > 0f ? hideDuration : duration;
            float scale = hideScale > 0f ? hideScale : startScale;
            if (dur <= 0f)
            {
                onComplete?.Invoke();
                return false;
            }

            // 起始值就取当前值（补间默认从当前状态开始），中途被打断的话能平滑接上
            Sequence sequence = DOTween.Sequence();

            if (IsFadeType)
            {
                CanvasGroup group = EnsureCanvasGroup();
                if (group != null)
                {
                    if (blockRaycastWhileAnimating) group.blocksRaycasts = false;
                    sequence.Join(group.DOFade(hideAlpha, dur).SetEase(Ease.InQuad));
                }
            }

            RectTransform rect = SelfRect;
            if (IsScaleType && rect != null)
            {
                sequence.Join(rect.DOScale(_originScale * scale, dur).SetEase(Ease.InBack));
            }

            if (IsSlideType && rect != null)
            {
                sequence.Join(rect.DOAnchorPos3D(_originPosition + (Vector3)slideOffset, dur).SetEase(Ease.InQuad));
            }

            _tween = sequence.SetUpdate(useUnscaledTime).OnComplete(() => onComplete?.Invoke());
            return true;
        }

        // 停止当前动画
        public void Kill()
        {
            if (_tween == null) return;
            _tween.Kill();
            _tween = null;
        }

        // 记录面板的原始缩放 / 位置 / 透明度，首次播放时缓存一次，之后一直复用
        private void CacheOrigin()
        {
            if (_originCached) return;

            RectTransform rect = SelfRect;
            if (rect != null)
            {
                _originScale = rect.localScale;
                _originPosition = rect.anchoredPosition3D;
            }

            // 只有需要淡入淡出时才确保有 CanvasGroup，不给用不上的面板凭空加组件
            if (IsFadeType)
            {
                CanvasGroup group = EnsureCanvasGroup();
                if (group != null)
                {
                    _originAlpha = group.alpha;
                    _originBlocksRaycasts = group.blocksRaycasts;
                }
            }

            _originCached = true;
        }

        // 动画结束时精确归位，避免缓动残留的小数误差
        private void ApplyFinalState()
        {
            RectTransform rect = SelfRect;
            if (rect != null)
            {
                if (IsScaleType) rect.localScale = _originScale;
                if (IsSlideType) rect.anchoredPosition3D = _originPosition;
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

            RectTransform rect = SelfRect;
            if (rect != null)
            {
                if (IsScaleType) rect.localScale = _originScale;
                if (IsSlideType) rect.anchoredPosition3D = _originPosition;
            }

            if (_canvasGroup != null)
            {
                if (IsFadeType) _canvasGroup.alpha = _originAlpha;
                if (blockRaycastWhileAnimating) _canvasGroup.blocksRaycasts = _originBlocksRaycasts;
            }
        }

        // 取 CanvasGroup，没有就加一个。
        // 这里绝不能用 `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`：
        // 编辑器下 GetComponent 找不到组件时返回的是「假空对象」（不是真正的 null），
        // `??` 判定不了它，会导致 AddComponent 不执行、然后把假空引用交出去，一访问就抛 MissingComponentException。
        private CanvasGroup EnsureCanvasGroup()
        {
            if (_canvasGroup != null) return _canvasGroup;

            if (!TryGetComponent(out _canvasGroup) || _canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            return _canvasGroup;
        }

        private void OnDisable()
        {
            // 隐藏 / 销毁时中断动画并归位，避免下次打开从半途状态开始
            Kill();
            RestoreOrigin();
        }
    }
}
