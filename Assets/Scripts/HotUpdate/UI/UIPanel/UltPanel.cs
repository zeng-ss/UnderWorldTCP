using DG.Tweening;
using HotUpdate.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 大招过场：全屏 RawImage 显示 PlayerCameraCtrl 渲染出来的 RenderTexture，
    /// 上下黑边从屏幕外滑入把画面框住，退场时滑出。
    /// </summary>
    [PanelPath("UltPanel")]
    public class UltPanel : BasePanel
    {
        public RawImage screen;
        public RectTransform topBar;
        public RectTransform bottomBar;
        public Image flash;

        [Header("时长")] public float barInDuration = 0.55f;

        public float barOutDuration = 0.26f;
        public float flashDuration = 0.28f;
        public float screenFadeIn = 0.14f;

        [Header("缓慢推近")] public float zoomStart = 1.08f;

        public float zoomDuration = 1.8f;

        [Tooltip("黑边滑出屏幕外要多远（大于黑边高度即可）")] public float barHideOffset = 200f;

        protected override PanelTween.Config? AnimConfig => PanelTween.Config.None;

        // 过场期间玩法输入已经被 ExAttackState 锁住了，这里不再压一层
        public override bool BlocksGameplayInput => false;

        private Sequence _sequence;

        public void Play(RenderTexture texture)
        {
            KillSequence();
            transform.SetAsLastSibling(); // 面板会被缓存复用，重新打开时要盖在 HUD 之上

            screen.texture = texture;
            screen.color = new Color(1f, 1f, 1f, 0f);
            screen.rectTransform.localScale = Vector3.one * zoomStart;
            flash.color = Color.white;

            // 黑边先挪出屏幕，再滑回就位位置
            topBar.anchoredPosition = Vector2.up * barHideOffset;
            bottomBar.anchoredPosition = Vector2.down * barHideOffset;

            _sequence = DOTween.Sequence().SetUpdate(true)
                .Join(screen.DOFade(1f, screenFadeIn).SetEase(Ease.OutQuad))
                .Join(screen.rectTransform.DOScale(Vector3.one, zoomDuration).SetEase(Ease.OutSine))
                .Join(flash.DOFade(0f, flashDuration))
                .Join(topBar.DOAnchorPos(Vector2.zero, barInDuration).SetEase(Ease.OutCubic))
                .Join(bottomBar.DOAnchorPos(Vector2.zero, barInDuration).SetEase(Ease.OutCubic));
        }

        public override void Hide()
        {
            KillSequence();

            _sequence = DOTween.Sequence().SetUpdate(true)
                .Join(topBar.DOAnchorPos(Vector2.up * barHideOffset, barOutDuration).SetEase(Ease.InCubic))
                .Join(bottomBar.DOAnchorPos(Vector2.down * barHideOffset, barOutDuration).SetEase(Ease.InCubic))
                .Join(screen.DOFade(0f, barOutDuration))
                .OnComplete(() =>
                {
                    screen.texture = null;
                    HideImmediate();
                });
        }

        private void OnDisable() => KillSequence();

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        }
    }
}