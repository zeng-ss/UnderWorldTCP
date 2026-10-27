using System;
using System.Collections.Generic;

namespace HotUpdate.RedDot
{
    /// <summary>
    /// 红点树上的一个节点。每个节点有两个数：
    ///   SelfCount  —— 只属于它自己的红点数，谁产生红点谁设（如「未读私聊」= 3）；
    ///   TotalCount —— 自己 + 所有子节点的合计，由子树变化自动重算。
    /// 任意一个节点变了都会沿 Parent 一路向上冒泡，所以父节点永远不用自己维护聚合值 ——
    /// </summary>
    public class RedDotNode
    {
        // 从根到自己的完整路径，如 "Main/Friend/Apply"
        public readonly string Path;
        private RedDotNode Parent { get; }

        private readonly List<RedDotNode> _children = new();

        // 关心这个节点的人（HUD 上的红点图标等）
        private readonly List<Action<int>> _watchers = new();

        private int _selfCount;
        private int TotalCount { get; set; }

        internal RedDotNode(string path, RedDotNode parent)
        {
            Path = path;
            Parent = parent;
        }

        // 挂一个子节点
        internal void AddChild(RedDotNode child)
        {
            _children.Add(child);
            Refresh();
        }

        #region 订阅

        /// <summary>
        /// 关心这个节点的人在这里订阅。订阅时会立刻用当前值回调一次，
        /// </summary>
        public void AddWatcher(Action<int> handler)
        {
            if (handler == null || _watchers.Contains(handler)) return;
            _watchers.Add(handler);
            handler(TotalCount);
        }

        /// <summary>
        /// 退订。组件销毁时必须调，否则回调会打到已销毁的对象上
        /// </summary>
        public void RemoveWatcher(Action<int> handler)
        {
            if (handler == null) return;
            _watchers.Remove(handler);
        }

        #endregion

        #region 改数

        public void SetCount(int count)
        {
            if (count < 0) count = 0;
            if (_selfCount == count) return;
            _selfCount = count;
            Refresh();
        }

        public void AddCount(int delta)
        {
            if (delta == 0) return;
            SetCount(_selfCount + delta);
        }

        public void Clear() => SetCount(0);

        public void ClearAll()
        {
            foreach (var child in _children) child.ClearAll();
            SetCount(0);
        }

        #endregion

        // 重算自己的合计值：变了才通知观察者，并继续往父节点冒泡
        private void Refresh()
        {
            int total = _selfCount;
            foreach (var child in _children) total += child.TotalCount;
            if (total == TotalCount) return;
            TotalCount = total;
            // Notify
            for (var i = _watchers.Count - 1; i >= 0; i--) _watchers[i]?.Invoke(TotalCount);
            Parent?.Refresh();
        }
    }
}