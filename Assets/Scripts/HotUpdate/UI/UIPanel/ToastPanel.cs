using System.Collections.Generic;
using System.Linq;
using HotUpdate.UI.UIItem;
using UnityEngine;

namespace HotUpdate.UI.UIPanel
{
    /// <summary>
    /// 左下角提示栈
    /// 用法：AppContext.Ui.ShowToast("击杀哥布林，金币×20")；
    ///      AppContext.Ui.ShowToasts(一批文本) —— 进队列，按 entryGap 一条一条滑入。
    /// </summary>
    public class ToastPanel : MonoBehaviour
    {
        [Tooltip("提示条的父节点")] [SerializeField] private RectTransform container;
        [Tooltip("模板")] [SerializeField] private ToastItem template;
        [Header("布局")] [SerializeField] private float spacing = 8f;
        [SerializeField] private float hold = 2f;
        [Tooltip("排队时相邻两条的间隔")] [SerializeField]
        private float entryGap = 0.22f;

        private readonly List<ToastItem> _alive = new();
        private readonly Stack<ToastItem> _pool = new();

        // 待滑入队列：一批文本先全部进来，再一条一条出
        private readonly Queue<string> _pending = new();
        private float _nextSpawnTime;

        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 900;

            template.gameObject.SetActive(false);
        }

        public void PushBatch(List<string> texts)
        {
            if (texts == null || texts.Count == 0) return;

            foreach (string text in texts)
            {
                if (string.IsNullOrEmpty(text)) continue;
                _pending.Enqueue(text);
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            // 现在一条都没在显示：别等下一帧，立刻弹第一条
            if (_alive.Count == 0)
            {
                SpawnNext();
                _nextSpawnTime = Time.unscaledTime + entryGap;
            }
        }

        private void Update()
        {
            if (_pending.Count == 0) return;
            if (Time.unscaledTime < _nextSpawnTime) return;

            SpawnNext();
            _nextSpawnTime = Time.unscaledTime + entryGap;
        }

        // 从队列里取一条真正显示出来
        private void SpawnNext()
        {
            if (_pending.Count == 0) return;

            ToastItem item = Rent();
            item.ResetVisual();
            _alive.Insert(0, item);

            // 先摆好队形，再让新条从屏幕外滑进来
            Relayout();

            item.Play(_pending.Dequeue(), hold, () => Detach(item));
        }

        // 取一条：池子里有就复用，没有就克隆模板
        private ToastItem Rent()
        {
            ToastItem item = _pool.Count > 0 ? _pool.Pop() : Instantiate(template, container);
            item.Rect.SetParent(container, false);
            item.gameObject.SetActive(true);
            return item;
        }

        // 移出队列 → 回池，并让剩下的条平滑落回
        private void Detach(ToastItem item)
        {
            _alive.Remove(item);
            item.gameObject.SetActive(false);
            _pool.Push(item);
            Relayout();

            // 还有排队没放完的就不能关，关掉 Update 就停了
            if (_alive.Count == 0 && _pending.Count == 0) gameObject.SetActive(false);
        }

        // 最新的在 index 0，往上依次排开
        private void Relayout()
        {
            for (int i = 0; i < _alive.Count; i++)
            {
                _alive[i].MoveTo(i * (_alive[i].Height + spacing));
            }
        }

        // 面板被外部关掉时清干净，下次打开不会有残留的透明条
        private void OnDisable()
        {
            _pending.Clear();
            _nextSpawnTime = 0f;

            foreach (var item in _alive.Where(item => item != null))
            {
                item.ResetVisual();
                item.gameObject.SetActive(false);
                _pool.Push(item);
            }

            _alive.Clear();
        }
    }
}