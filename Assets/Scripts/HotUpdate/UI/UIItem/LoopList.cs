using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotUpdate.UI.UIItem
{
    /// <summary>
    /// 通用无限循环列表（Vertical）。
    /// 基于 ScrollRect：只实例化「可视范围 + 缓冲」内的 item，滚动时复用已创建的 item，
    /// 数据量再大也只持有固定数量的 GameObject，避免一次性创建全部造成卡顿。
    ///
    /// 用法：
    ///   1. SetItemFactory(createItem, fillItem)  —— createItem 返回一个新的 item 根（含 RectTransform），
    ///      fillItem(index, itemGo) 根据数据下标填充 item。
    ///   2. SetItemHeight(itemHeight + spacing)   —— 每个 item 的固定高度（含间距）。
    ///   3. SetCount(totalCount)                  —— 数据总数，之后本类接管滚动与复用。
    ///   4. 把本组件挂在一个 ScrollRect 的 content 上（content 需是 VerticalLayoutGroup 或手动排，
    ///      本类自己计算位置，不依赖 LayoutGroup）。
    ///
    /// 约定：item 用 anchoredPosition 手动定位，content 的 pivot 设为 (0, 1)（左上角），
    /// 这样第 0 项在顶部、向下递增。
    /// </summary>
    public class LoopList : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private RectTransform _content;

        // item 工厂与填充回调
        private Func<GameObject> _createItem;
        private Action<int, GameObject> _fillItem;

        // item 高度（含间距）
        private float _itemHeight = 40f;

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

        public void SetItemHeight(float height) => _itemHeight = Mathf.Max(1f, height);

        public void ScrollToBottomOnRefresh(bool enable) => _scrollToBottomOnRefresh = enable;

        /// <summary>设置数据总数并刷新。scrollToBottom 用于聊天（新消息滚到底部）。</summary>
        public void SetCount(int count, bool scrollToBottom = false)
        {
            _count = Mathf.Max(0, count);

            // 计算 content 总高度
            float totalHeight = _count * _itemHeight;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, totalHeight);

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

            // 刷新可视范围
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
                // 顶部
                scrollY = 0f;
            }
            else if (_scrollRect.verticalNormalizedPosition <= 0.001f)
            {
                // 底部
                float totalHeight = _count * _itemHeight;
                scrollY = Mathf.Max(0f, totalHeight - viewHeight);
            }

            // 加缓冲，滚动时边缘不会露白
            float buffer = _itemHeight * 2f;
            float topBound = scrollY - buffer;
            float bottomBound = scrollY + viewHeight + buffer;

            int first = Mathf.Clamp(Mathf.FloorToInt(topBound / _itemHeight), 0, _count - 1);
            int last = Mathf.Clamp(Mathf.CeilToInt(bottomBound / _itemHeight) - 1, 0, _count - 1);

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

        private void SetItemPosition(GameObject item, int index)
        {
            var rt = item.transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(index + 1) * _itemHeight);
            rt.offsetMax = new Vector2(0f, -index * _itemHeight);
        }

        private void ClearAllItems()
        {
            foreach (var kv in _activeItems)
            {
                if (kv.Value == null) continue;
                kv.Value.SetActive(false);
                _recyclePool.Push(kv.Value);
            }

            _activeItems.Clear();
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }
}