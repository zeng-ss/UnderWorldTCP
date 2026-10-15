using System.Collections.Generic;

namespace HotUpdate.RedDot
{
    /// <summary>
    /// 红点树本体：持有一棵以空路径为根的树，并按「路径 → 节点」做索引。
    ///
    /// 用法只有两条路：
    ///   产生红点的人 —— AppContext.RedDot.Set(path, n) / Add(path, n) / Clear(path)（路径常量见 <see cref="RedDotPath"/>）；
    ///   显示红点的人 —— AppContext.RedDot.GetOrCreate(path).Watch(count =&gt; 红点显隐)。
    /// </summary>
    public class RedDotMgr
    {
        private const char Separator = '/';

        private readonly RedDotNode _root = new(string.Empty, null);
        private readonly Dictionary<string, RedDotNode> _index = new();

        public RedDotMgr()
        {
            _index[string.Empty] = _root;
        }

        /// <summary>
        /// 取节点；路径还没建过就按需创建
        /// </summary>
        public RedDotNode GetOrCreate(string path)
        {
            if (string.IsNullOrEmpty(path)) return _root;
            if (_index.TryGetValue(path, out RedDotNode node)) return node;

            RedDotNode parent = GetOrCreate(ParentPath(path));
            var created = new RedDotNode(path, parent);
            parent.AddChild(created);
            _index[path] = created;
            return created;
        }

        /// <summary>
        /// 把某个节点的红点数直接设为 count
        /// </summary>
        public void Set(string path, int count) => GetOrCreate(path).SetCount(count);

        /// <summary>
        /// 某个节点的红点数加 delta
        /// </summary>
        public void Add(string path, int delta) => GetOrCreate(path).AddCount(delta);

        /// <summary>
        /// 清掉某个节点的红点 子树不受影响
        /// </summary>
        public void Clear(string path) => GetOrCreate(path).Clear();

        public void ClearAll() => _root.ClearAll();

        // "Main/Friend/Apply" → "Main/Friend"；顶层节点（"Main"）→ ""
        private static string ParentPath(string path)
        {
            int index = path.LastIndexOf(Separator);
            return index <= 0 ? string.Empty : path.Substring(0, index);
        }
    }
}