using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// 通用无限循环列表（Vertical）。
    /// SetItemFactory(createItem, fillItem) —— createItem 返回一个新的 item 根，fillItem(index, itemGo) 根据数据下标填充 item
    /// </summary>
    public class LoopList : MonoBehaviour
    {
        [Header("列表布局")] [SerializeField] private float itemHeight = 80f;

        [SerializeField] private float itemWidth; // 每一行的宽度；填 0 = 撑满 Content 宽度

        [SerializeField] private float spacing = 8f; // 行与行之间的间距

        private ScrollRect _scrollRect;
        private RectTransform _content;

        // item 工厂与填充回调
        private Func<GameObject> _createItem;
        private Action<int, GameObject> _fillItem;

        // 数据总数
        private int _count;

        // 当前已实例化的 item 池（下标 → item）
        private readonly Dictionary<int, GameObject> _activeItems = new();

        // 空闲 item 池（待复用）
        private readonly Stack<GameObject> _recyclePool = new();

        // 当前可视范围（首尾下标）
        private int _firstVisible = -1;
        private int _lastVisible = -1;

        private bool _initialized;

        // 是否「新数据在底部」（聊天消息：新消息追加到末尾并滚到底部）
        private bool _scrollToBottomOnRefresh;

        // 一行占用的垂直空间（高度 + 间距）
        private float Step => itemHeight + spacing;

        // Content 需要的总高度：最后一行后面不用再加间距
        private float TotalHeight => _count <= 0 ? 0f : _count * itemHeight + (_count - 1) * spacing;

        public void Init(ScrollRect scrollRect, RectTransform content)
        {
            _scrollRect = scrollRect;
            _content = content;
            _initialized = true;

            // content 锚点固定在顶部，便于按「第 0 项在 y=0」手动排位
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        public void SetItemFactory(Func<GameObject> createItem, Action<int, GameObject> fillItem)
        {
            _createItem = createItem;
            _fillItem = fillItem;
        }

        // 只改行高
        public void SetHeight(float height) => itemHeight = Mathf.Max(1f, height);

        // 行高 + 间距一起设
        public void SetHeight(float height, float space)
        {
            itemHeight = Mathf.Max(1f, height);
            spacing = Mathf.Max(0f, space);
        }

        // 行宽；传 0 表示撑满 Content 宽度
        public void SetWidth(float width) => itemWidth = Mathf.Max(0f, width);

        public void SetSpacing(float space) => spacing = Mathf.Max(0f, space);

        public void ScrollToBottomOnRefresh(bool enable) => _scrollToBottomOnRefresh = enable;

        /// <summary>设置数据总数并刷新。scrollToBottom 用于聊天（新消息滚到底部）。</summary>
        public void SetCount(int count, bool scrollToBottom = false)
        {
            _count = Mathf.Max(0, count);
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, TotalHeight);

            // 释放超出总数的 item（数据变少了）
            var toRecycle = new List<int>();
            foreach (var kv in _activeItems)
            {
                if (kv.Key >= _count) toRecycle.Add(kv.Key);
            }

            foreach (var idx in toRecycle)
            {
                RecycleItem(idx);
            }

            if (scrollToBottom)
            {
                _scrollRect.verticalNormalizedPosition = 0f; // 滚到底部
            }

            RefreshVisibleItems();

            // RefreshVisibleItems 只会「新建」缺失下标的 item，已经在可视范围内的下标会被直接跳过，
            // 但数据本身可能已经变了（例如好友申请被同意后，同一下标从「申请」变成「好友」）。
            // 所以这里再整体重填一次，保证「数量不变、内容变了」的情况也能正确刷新。
            RefreshItems();
        }

        /// <summary>数据没变、但需要重画当前可视项时调用（例如选中态变化、在线状态刷新）</summary>
        public void RefreshItems()
        {
            if (!_initialized) return;

            var keys = new List<int>(_activeItems.Keys);
            foreach (int index in keys)
            {
                if (_activeItems.TryGetValue(index, out var item) && item != null)
                    _fillItem?.Invoke(index, item);
            }
        }

        private void OnScrollValueChanged(Vector2 value) => RefreshVisibleItems();

        private void RefreshVisibleItems()
        {
            if (!_initialized || _count == 0)
            {
                ClearAllItems();
                return;
            }

            float viewHeight = _scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : _scrollRect.GetComponent<RectTransform>().rect.height;

            // content 的 anchoredPosition.y 表示当前滚动偏移（顶部对齐，向下滚动 y 增大）
            float scrollY = _content.anchoredPosition.y;
            if (_scrollRect.verticalNormalizedPosition > 0.999f)
            {
                scrollY = 0f;
            }
            else if (_scrollRect.verticalNormalizedPosition <= 0.001f)
            {
                scrollY = Mathf.Max(0f, TotalHeight - viewHeight);
            }

            // 加缓冲，滚动时边缘不会露白
            float step = Step;
            float buffer = step * 2f;
            float topBound = scrollY - buffer;
            float bottomBound = scrollY + viewHeight + buffer;

            int first = Mathf.Clamp(Mathf.FloorToInt(topBound / step), 0, _count - 1);
            int last = Mathf.Clamp(Mathf.CeilToInt(bottomBound / step) - 1, 0, _count - 1);

            // 回收滑出范围的 item
            var toRecycle = new List<int>();
            foreach (var kv in _activeItems)
            {
                if (kv.Key < first || kv.Key > last) toRecycle.Add(kv.Key);
            }

            foreach (var idx in toRecycle) RecycleItem(idx);

            // 创建 / 复用可视范围内的 item
            for (int i = first; i <= last; i++)
            {
                if (_activeItems.ContainsKey(i)) continue;
                ActivateItem(i);
            }

            _firstVisible = first;
            _lastVisible = last;
        }

        private void ActivateItem(int index)
        {
            GameObject item;
            if (_recyclePool.Count > 0)
            {
                item = _recyclePool.Pop();
            }
            else
            {
                item = _createItem?.Invoke();
                if (item == null) return;
                item.transform.SetParent(_content, false);
            }

            item.SetActive(true);
            SetItemPosition(item, index);
            _activeItems[index] = item;
            _fillItem?.Invoke(index, item);
        }

        private void RecycleItem(int index)
        {
            if (!_activeItems.TryGetValue(index, out var item)) return;
            _activeItems.Remove(index);
            item.SetActive(false);
            _recyclePool.Push(item);
        }

        // 位置和尺寸都由本类写：item 预制体里摆多大都不影响运行时行高
        private void SetItemPosition(GameObject item, int index)
        {
            if (item.transform is not RectTransform rt) return;

            float top = -index * Step;

            if (itemWidth > 0f)
            {
                // 固定宽度：水平居中
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(itemWidth, itemHeight);
                rt.anchoredPosition = new Vector2(0f, top);
                return;
            }

            // 撑满 Content：左右拉满，用 offset 定上下边
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, top - itemHeight);
            rt.offsetMax = new Vector2(0f, top);
        }

        private void ClearAllItems()
        {
            foreach (var kv in _activeItems)
            {
                if (kv.Value is null) continue;
                kv.Value.SetActive(false);
                _recyclePool.Push(kv.Value);
            }

            _activeItems.Clear();
        }

        private void OnDestroy()
        {
            _scrollRect?.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }
}