using System.Collections;
using HotUpdate.Core;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.Manager
{
    public class BasePanel : MonoBehaviour
    {
        [HideInInspector] public bool isAnimating;
        private RectTransform _rootRect;

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
        }

        /// <summary>
        /// 该面板打开时是否独占操作、锁住玩法输入（移动 / 攻击等）。
        /// 默认 false —— 提示类、HUD 类面板不该影响操作；
        /// 需要独占的面板（如仓库、强化、退出确认）重写为 true。
        /// </summary>
        public virtual bool BlocksGameplayInput => false;

        /// <summary>
        /// 关闭按钮点击回调。默认经 UIManager 关闭自身（这样才能正确释放输入锁）。
        /// 需要连带关闭其他面板时由子类重写（见 ImprovePanel）。
        /// </summary>
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
        }


        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
