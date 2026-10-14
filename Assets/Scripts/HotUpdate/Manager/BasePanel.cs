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

        // 找到 / 挂上打开动画组件，并把子类给的配置覆盖上去
        private void InitPanelTween()
        {
            PanelAnimator = GetComponent<PanelTween>();
            if (PanelAnimator == null) PanelAnimator = gameObject.AddComponent<PanelTween>();
            var config = AnimConfig;
            if (config.HasValue) PanelAnimator.Apply(config.Value);
        }

        /// <summary>
        /// 该面板打开时是否独占操作
        /// </summary>
        public virtual bool BlocksGameplayInput => false;

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
            // 关闭不做动画：这里只把打开动画中断并把状态归位，避免残留半途的缩放 / 透明度
            PanelAnimator?.Kill();
            isAnimating = false;
            gameObject.SetActive(false);
        }
    }
}