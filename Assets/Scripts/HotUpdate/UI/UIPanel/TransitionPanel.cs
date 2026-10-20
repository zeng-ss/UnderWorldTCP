using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 用法：AppContext.Ui.Transition(() => { 切场景 / 开面板; 完成后调 AppContext.Ui.TransitionDone(); });
    /// </summary>
    public class TransitionPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RawImage scrollLayer;
        [SerializeField] private RectTransform ring;
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform[] dots;

        [Header("流光")] [Tooltip("uvRect 每秒滚动多少（贴图先平铺，再无限偏移）")] [SerializeField]
        private Vector2 scrollSpeed = new(0.08f, 0.03f);

        [Tooltip("横向平铺几列；行数按贴图与屏幕的宽高比自动算，换贴图不会被拉变形")]
        [SerializeField]
        private float scrollColumns = 16f;

        [Header("节奏")] [SerializeField] private float fadeIn = 0.35f;
        [SerializeField] private float fadeOut = 0.4f;

        [Tooltip("遮住之后至少停留多久")] [SerializeField]
        private float minHold = 0.5f;

        [Tooltip("等切换完成的最长时间")] [SerializeField]
        private float timeout = 8f;

        private Coroutine _routine;
        private bool _revealed;
        private Material _scrollMaterial;

        private const string ScrollShader = "UnderWord/UI/ScrollAdditive";

        public bool IsPlaying => _routine != null;

        private void Awake()
        {
            // Canvas 必须实例化到父 Canvas 下之后再挂：预制体根节点上的 Canvas 会被 Unity 当成
            // 「根 Overlay Canvas」，把 RectTransform 的 localScale / anchor / sizeDelta 全存成 0，
            // 结果就是 Hierarchy 里有、Game 里整块 scale 0 看不见。
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;
            gameObject.AddComponent<GraphicRaycaster>();

            PrepareScrollMaterial();
        }

        // 复制一份材质：改 UV 偏移时不污染 Resources 里那个共享资源
        private void PrepareScrollMaterial()
        {
            if (scrollLayer == null) return;

            Material source = scrollLayer.material;
            if (source == null || source.shader == null || source.shader.name != ScrollShader)
                source = Resources.Load<Material>("Materials/TransitionScroll");
            if (source == null) return;

            _scrollMaterial = new Material(source);
            scrollLayer.material = _scrollMaterial;
        }

        private void Update()
        {
            if (_scrollMaterial == null) return;

            // 改材质 UV 偏移，而不是 RawImage.uvRect：
            // uvRect 的 setter 会 SetVerticesDirty，每帧重建整块全屏网格 + 大量 GC，全屏时非常卡
            Vector2 offset = _scrollMaterial.mainTextureOffset + scrollSpeed * Time.deltaTime;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);
            _scrollMaterial.mainTextureOffset = offset;
        }

        public void Play(float minHoldDuration, Action onCovered)
        {
            // 正在转场就忽略重复请求（连点按钮不该让遮罩闪两遍）
            if (IsPlaying)
            {
                Debug.LogWarning("[TransitionPanel] 上一次转场还没结束，忽略这次请求");
                return;
            }

            minHold = minHoldDuration;
            _revealed = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ApplyTiling();
            _routine = StartCoroutine(Run(onCovered));
        }

        /// <summary>
        /// 按「屏幕宽高比 / 贴图宽高比」算平铺次数：每块贴图的宽高比和原图一致，换任何贴图都不会被拉变形。
        /// 只定横向列数，行数自动推出来。平铺次数写在材质上（顶点 UV 固定 0~1）。
        /// </summary>
        private void ApplyTiling()
        {
            if (scrollLayer == null || scrollLayer.texture == null || _scrollMaterial == null) return;

            // 换贴图时新贴图默认是 Clamp，平铺到边界会拉出一条线，这里统一改成 Repeat
            scrollLayer.texture.wrapMode = TextureWrapMode.Repeat;

            // 顶点 UV 固定 0~1：滚动靠材质 UV 偏移，避免每帧重建整块网格
            scrollLayer.uvRect = new Rect(0f, 0f, 1f, 1f);

            float texAspect = (float)scrollLayer.texture.width / scrollLayer.texture.height;
            float screenAspect = (float)Screen.width / Screen.height;
            float rows = Mathf.Max(1f, scrollColumns * texAspect / screenAspect);

            // shader 里 TRANSFORM_TEX 用的就是 _MainTex_ST
            _scrollMaterial.mainTextureScale = new Vector2(scrollColumns, rows);
        }

        public void Done() => _revealed = true;

        private IEnumerator Run(Action onCovered)
        {
            group.blocksRaycasts = true;
            group.alpha = 0f;
            PlayIdle();

            yield return group.DOFade(1f, fadeIn).SetUpdate(true).WaitForCompletion();

            onCovered?.Invoke();

            // 两个条件都满足才退出：切换已完成，且遮罩已经停了 minHold
            float waited = 0f;
            while (waited < minHold || (!_revealed && waited < timeout))
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // 淡出期间动效继续播，等完全看不见了再 SetActive
            yield return group.DOFade(0f, fadeOut).SetUpdate(true).WaitForCompletion();

            _routine = null;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
            ResetUI();
        }

        // 遮住期间的循环动效：光环转圈 + 文字呼吸 + 三个点依次跳动
        private void PlayIdle()
        {
            ResetUI();
            ring.DORotate(new Vector3(0f, 0f, 360f), 1.8f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);

            title.DOFade(0f, 0.4f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true);

            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = dots[i];
                dot.DOScale(1.6f, 0.32f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetDelay(i * 0.12f).SetUpdate(true);
            }
        }

        private void ResetUI()
        {
            ring.DOKill();
            title.DOKill();
            foreach (RectTransform dot in dots) dot.DOKill();
            ring.localRotation = Quaternion.identity;
            title.alpha = 1f;
            foreach (RectTransform dot in dots) dot.localScale = Vector3.one;
        }
    }
}