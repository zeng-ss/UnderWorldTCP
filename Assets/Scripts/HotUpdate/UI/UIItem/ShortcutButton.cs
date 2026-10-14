using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// HUD 快捷栏里的一格：方块底图 + 图标 + 中文名 + 右下角按键角标。
    ///
    /// 外观全部摆在预制体上（见 PlayerProPanelBuilder），这里只做两件事：
    ///   ① 按快捷键总表把点击接上 —— 点一下等于按了对应的快捷键；
    ///   ② 鼠标悬停时整格放大。
    /// </summary>
    public class ShortcutButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 这一格打开哪个面板（对应 PanelShortcut 总表），预制体里已选好
        public PanelId panel;

        // 这一格的点击区，预制体里已连到 Tile 上
        public Button button;

        // 悬停时整格放大的倍数 / 时长
        private const float HoverScale = 1.12f;
        private const float HoverDuration = 0.12f;

        private RectTransform _rect;
        private Vector3 _originScale = Vector3.one;

        private void Awake()
        {
            _rect = transform as RectTransform;
            if (_rect != null) _originScale = _rect.localScale;

            PanelShortcut shortcut = PanelShortcut.Get(panel);
            if (shortcut == null || button == null) return;

            button.onClick.AddListener(() => shortcut.Toggle());
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
