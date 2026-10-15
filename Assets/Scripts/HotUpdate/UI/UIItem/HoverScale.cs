using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HotUpdate.UI.UIItem
{
    [AddComponentMenu("UI/Hover Scale")]
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 要缩放的目标
        [SerializeField] private RectTransform target;
        [SerializeField] private float scale = 1.12f;
        [SerializeField] private float duration = 0.12f;
        [SerializeField] private Ease ease = Ease.OutQuad;
        // timeScale=0 时仍要响应
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 _originScale = Vector3.one;

        private void Awake()
        {
            if (target == null) target = transform as RectTransform;
            _originScale = target == null ? Vector3.one : target.localScale;
        }

        public void SetScale(float hoverScale, float animDuration = 0.12f)
        {
            scale = hoverScale;
            if (animDuration > 0f) duration = animDuration;
        }

        public void OnPointerEnter(PointerEventData eventData) => Play(_originScale * scale);

        public void OnPointerExit(PointerEventData eventData) => Play(_originScale);

        private void Play(Vector3 to)
        {
            if (target == null) return;

            target.DOKill(true);
            target.DOScale(to, duration).SetEase(ease).SetUpdate(useUnscaledTime);
        }

        // 关闭时归位：面板被隐藏但对象还在（UIManager 缓存复用），下次打开不该留着放大状态
        private void OnDisable()
        {
            if (target == null) return;

            target.DOKill(true);
            target.localScale = _originScale;
        }
    }
}