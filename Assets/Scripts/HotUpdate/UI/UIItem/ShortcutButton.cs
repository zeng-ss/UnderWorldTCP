using System;
using DG.Tweening;
using HotUpdate.RedDot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// HUD 快捷栏里的一格：点一下等于按了对应快捷键，右上角跟着红点树亮红点。
    /// 外观全部摆在预制体上（见 PlayerProPanelBuilder），这里只接点击与红点。
    /// </summary>
    public class ShortcutButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 这一格打开哪个面板（对应 PanelShortcut 总表），预制体里已选好
        public PanelId panel;

        // 这一格的点击区，预制体里已连到 Tile 上
        public Button button;

        public GameObject redDot;

        // 悬停时整格放大的倍数 / 时长
        private const float HoverScale = 1.12f;
        private const float HoverDuration = 0.12f;

        private RectTransform _rect;
        private Vector3 _originScale = Vector3.one;

        private RedDotNode _redDotNode;

        private void Awake()
        {
            _rect = transform as RectTransform;
            if (_rect != null) _originScale = _rect.localScale;

            if (!AppContext.IsAlive) return;

            PanelShortcut shortcut = PanelShortcut.Get(panel);
            if (shortcut == null) return;

            if (button != null) button.onClick.AddListener(() => shortcut.Toggle());

            // 红点交给红点树：只订阅自己那一格对应的节点，数值变了才动显隐
            _redDotNode = AppContext.RedDot.GetOrCreate(shortcut.DotPath);
            _redDotNode.AddWatcher(SetRedDotVisible);
        }

        private void OnDestroy()
        {
            _redDotNode?.RemoveWatcher(SetRedDotVisible);
        }

        // 红点数 > 0 才亮
        private void SetRedDotVisible(int count)
        {
            if (redDot == null) return;

            bool show = count > 0;
            if (redDot.activeSelf != show) redDot.SetActive(show);
        }

        public void OnPointerEnter(PointerEventData eventData) => PlayHover(HoverScale);

        public void OnPointerExit(PointerEventData eventData) => PlayHover(1f);

        private void PlayHover(float scale)
        {
            if (_rect == null) return;

            _rect.DOKill();
            _rect.DOScale(_originScale * scale, HoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private void OnDisable()
        {
            if (_rect == null) return;

            _rect.DOKill();
            _rect.localScale = _originScale;
        }
    }
}