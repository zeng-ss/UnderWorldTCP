using System.Collections;
using HotUpdate.Core;
using HotUpdate.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.Manager
{
    public class BasePanel : MonoBehaviour
    {
        [HideInInspector] public bool isAnimating;
        private RectTransform _rootRect;
        private PanelTween PanelAnimator { get; set; }

        // 关闭动画进行中（防止重复触发关闭）
        private bool _isHiding;

        // 打开动画配置。
        protected virtual PanelTween.Config? AnimConfig => null;

        protected IEnumerator DelayedLayoutUpdate()
        {
            yield return null; // 等待一帧
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rootRect);
        }

        private Button _closeBtn;

        protected virtual void Awake()
        {
            _rootRect = transform as RectTransform;
            _closeBtn = transform.Find("CloseBtn")?.GetComponent<Button>();
            if (_closeBtn != null)
            {
                _closeBtn.onClick.AddListener(OnCloseClicked);
            }

            InitPanelTween();
        }

        // 找到 / 挂上开关动画组件，并把子类给的配置覆盖上去。
        // 注意：判空只能用 TryGetComponent 或 Unity 重载的 == null —— 编辑器下
        // GetComponent<T>() 找不到组件时返回的是「假空对象」，用 ?? / is null 判不出来。
        private void InitPanelTween()
        {
            if (!TryGetComponent(out PanelTween tween)) tween = gameObject.AddComponent<PanelTween>();
            PanelAnimator = tween;
            var config = AnimConfig;
            if (config.HasValue) PanelAnimator.Apply(config.Value);
        }

        /// <summary>
        /// 该面板打开时是否独占操作
        /// </summary>
        public virtual bool BlocksGameplayInput => false;

        /// <summary>
        /// 是否正在播关闭动画。此时面板还没 SetActive(false)，但逻辑上已经算关掉了。
        /// </summary>
        public bool IsClosing => _isHiding;

        protected virtual void OnCloseClicked()
        {
            AppContext.Ui.ClosePanel(this);
        }

        protected virtual void OnDestroy()
        {
            if (_closeBtn != null)
            {
                _closeBtn.onClick.RemoveListener(OnCloseClicked);
            }
        }

        public virtual void Show() //虚函数 能够被重写  
        {
            // 上一轮的关闭动画可能还没播完（面板又被打开），这里直接作废它
            _isHiding = false;

            gameObject.SetActive(true);
            // 动画期间置 isAnimating，外部切换面板 / 逻辑判断可以据此避开动画帧
            if (PanelAnimator == null)
            {
                isAnimating = false;
                return;
            }

            isAnimating = PanelAnimator.PlayShow(() => { isAnimating = false; });
        }

        public virtual void Hide()
        {
            // 关闭动画正在播：忽略重复的关闭请求，避免动画被反复重启
            if (_isHiding) return;

            // 没挂动画组件 / 面板本来就没激活：直接收尾
            if (PanelAnimator == null || !gameObject.activeInHierarchy)
            {
                HideImmediate();
                return;
            }

            // 先播关闭动画，动画结束后才真正 SetActive(false)，状态复原交给 PanelTween.OnDisable
            _isHiding = true;
            isAnimating = PanelAnimator.PlayHide(() =>
            {
                _isHiding = false;
                isAnimating = false;
                gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// 立即隐藏，不播关闭动画。用于卸载资源 / 整体清理等不希望有动画的场合。
        /// </summary>
        public void HideImmediate()
        {
            _isHiding = false;
            if (PanelAnimator != null) PanelAnimator.Kill();
            isAnimating = false;
            gameObject.SetActive(false);
        }
    }
}