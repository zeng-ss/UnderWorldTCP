using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 用法：AppContext.Ui.Transition(() => { 切场景 / 开面板; 完成后调 AppContext.Ui.TransitionDone(); });
    /// </summary>
    public class TransitionPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform ring;
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform[] dots;

        [Header("节奏")] [SerializeField] private float fadeIn = 0.35f;
        [SerializeField] private float fadeOut = 0.4f;

        [Tooltip("遮住之后至少停留多久")] [SerializeField]
        private float minHold = 0.5f;

        [Tooltip("等切换完成的最长时间")] [SerializeField]
        private float timeout = 8f;

        private Coroutine _routine;
        private bool _revealed;

        public bool IsPlaying => _routine != null;

        private void Awake()
        {
            // 预制体是根节点时 overrideSorting 存不住（Unity 按根 Canvas 处理，保存时会清成 false），
            // 实例化到父 Canvas 下之后再设一次，999 才真的压过所有面板
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;
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
            _routine = StartCoroutine(Run(onCovered));
        }

        /// <summary>切换完成，可以淡出了</summary>
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
        }

        // 遮住期间的循环动效：光环转圈 + 文字呼吸 + 三个点依次跳动
        private void PlayIdle()
        {
            // 上一次是直接 SetActive(false) 掐断的，动画停在半路，先清掉再重播
            ring.DOKill();
            title.DOKill();
            foreach (RectTransform dot in dots) dot.DOKill();

            ring.localRotation = Quaternion.identity;
            title.alpha = 1f;
            foreach (RectTransform dot in dots) dot.localScale = Vector3.one;

            ring.DORotate(new Vector3(0f, 0f, 360f), 1.8f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);

            title.DOFade(0.35f, 0.3f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true);

            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = dots[i];
                dot.DOScale(1.6f, 0.32f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetDelay(i * 0.12f).SetUpdate(true);
            }
        }
    }
}