using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// 一条左下角提示
    /// </summary>
    public class ToastItem : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        [Header("节奏")] [SerializeField]
        private float slideIn = 0.26f, fadeOut = 0.28f, slideOffset = 24f, moveDuration = 0.16f;

        private Sequence _seq;
        private Tween _moveTween;

        public RectTransform Rect => rect;
        public float Height => rect.sizeDelta.y;

        // 播放一条提示
        public void Play(string text, float hold, Action onDone)
        {
            label.text = text;
            ResetVisual();

            _seq = DOTween.Sequence().SetUpdate(true);
            _seq.Join(rect.DOAnchorPosX(0f, slideIn).SetEase(Ease.OutCubic));
            _seq.Join(group.DOFade(1f, slideIn).SetEase(Ease.OutQuad));
            _seq.AppendInterval(hold);
            _seq.Append(group.DOFade(0f, fadeOut).SetEase(Ease.InQuad));
            _seq.OnComplete(() => onDone?.Invoke());
        }

        public void MoveTo(float y)
        {
            _moveTween?.Kill();
            _moveTween = rect.DOAnchorPosY(y, moveDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>回池前复位，避免下次复用时残留上一轮的透明度 / 位置</summary>
        public void ResetVisual()
        {
            _seq?.Kill();
            _moveTween?.Kill();
            _seq = null;
            _moveTween = null;
            group.alpha = 0f;
            rect.anchoredPosition = new Vector2(-(rect.sizeDelta.x + slideOffset), 0f);
        }
    }
}