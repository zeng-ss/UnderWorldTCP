using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    [AddComponentMenu("UI/Button Hover Tint")]
    public class ButtonHoverTint : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("背景图，留空 = 本物体上的 Image")] [SerializeField]
        private Graphic background;

        [Tooltip("跟随变色的前景，留空 = 所有子 Graphic")] [SerializeField]
        private List<Graphic> foregrounds = new();

        [Header("悬停配色")] [SerializeField] private Color hoverBackground = new Color(0.85f, 1f, 0.24f, 1f);
        [SerializeField] private Color hoverForeground = new Color(0.05f, 0.05f, 0.06f, 1f);

        [Header("节奏")] [SerializeField] private float duration = 0.2f;

        [Tooltip("按下时背景在悬停色基础上压暗多少")] [Range(0f, 0.5f)] [SerializeField]
        private float pressDarken = 0.18f;

        private readonly List<Color> _fgOrigin = new List<Color>();
        private Color _bgOrigin;
        private Selectable _selectable;
        private bool _hovered;
        private bool _pressed;
        private Tween _bgTween;
        private readonly List<Tween> _fgTweens = new List<Tween>();

        /// <summary>运行时改配色 + 过渡时长</summary>
        public void SetColors(Color background, Color foreground, float animDuration)
        {
            hoverBackground = background;
            hoverForeground = foreground;
            if (animDuration > 0f) duration = animDuration;
        }

        /// <summary>
        /// 显式指定跟随变色的前景。默认会自动收集所有子 Graphic，
        /// 但图标那类「Image + sprite」不该被染成纯色，这种情况用这个指定。
        /// </summary>
        public void SetForegrounds(params Graphic[] graphics)
        {
            foregrounds = new List<Graphic>(graphics);
        }

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            _selectable = GetComponent<Selectable>();

            // 没显式指定就自动收集：按钮里的文字、序号、左侧色条全靠这个跟着变色
            if (foregrounds.Count == 0)
            {
                foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
                {
                    if (g != background && g.name != "Bar") foregrounds.Add(g);
                }
            }

            _bgOrigin = background != null ? background.color : Color.white;
            _fgOrigin.Clear();
            _fgTweens.Clear();
            foreach (Graphic g in foregrounds)
            {
                _fgOrigin.Add(g != null ? g.color : Color.white);
                _fgTweens.Add(null);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 不可用的按钮（比如还没连上服务器）不该有悬停反馈
            if (_selectable != null && !_selectable.interactable) return;

            _hovered = true;
            _pressed = false;
            PlayTo(hoverBackground, hoverForeground);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            PlayTo(_bgOrigin, null);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_hovered) return;
            _pressed = true;

            // Color * float 会连 alpha 一起乘，这里只压 RGB，别把按钮压成半透明
            float k = 1f - pressDarken;
            PlayTo(new Color(hoverBackground.r * k, hoverBackground.g * k, hoverBackground.b * k,
                hoverBackground.a), null);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressed) return;
            _pressed = false;
            PlayTo(_hovered ? hoverBackground : _bgOrigin, _hovered ? (Color?)hoverForeground : null);
        }

        // toForeground == null → 前景回到各自的原始色
        private void PlayTo(Color toBackground, Color? toForeground)
        {
            float d = Mathf.Max(0f, duration);

            if (background != null)
            {
                _bgTween?.Kill();
                Graphic bg = background;
                Color from = bg.color;
                _bgTween = DOTween.To(() => from, x =>
                    {
                        from = x;
                        bg.color = x;
                    }, toBackground, d)
                    .SetUpdate(true);
            }

            for (int i = 0; i < foregrounds.Count; i++)
            {
                Graphic g = foregrounds[i];
                if (g == null) continue;

                Color target = toForeground ?? (i < _fgOrigin.Count ? _fgOrigin[i] : g.color);
                _fgTweens[i]?.Kill();

                Graphic fg = g;
                Color start = fg.color;
                _fgTweens[i] = DOTween.To(() => start, x =>
                    {
                        start = x;
                        fg.color = x;
                    }, target, d)
                    .SetUpdate(true);
            }
        }

        // 面板关闭 / 对象复用：把颜色归位，下次打开不该留着悬停色
        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;

            _bgTween?.Kill();
            if (background != null) background.color = _bgOrigin;

            for (int i = 0; i < foregrounds.Count; i++)
            {
                _fgTweens[i]?.Kill();
                if (foregrounds[i] != null && i < _fgOrigin.Count) foregrounds[i].color = _fgOrigin[i];
            }
        }
    }
}