using System.Collections.Generic;

namespace HotUpdate.Service
{
    /// <summary>
    /// 材料数据服务
    /// 数量变化时广播 GameEvent.MaterialNumChanged，View 自行刷新。
    /// </summary>
    public class MaterialService
    {
        private readonly Dictionary<int, MaterialDataRuntime> _runtime = new();
        private readonly Dictionary<int, int> _counts = new();

        /// <summary>材料静态配置（id → 名称 / 图标 / 数值）</summary>
        public IReadOnlyDictionary<int, MaterialDataRuntime> RuntimeData => _runtime;

        /// <summary>材料持有数量（id → 数量）</summary>
        public IReadOnlyDictionary<int, int> Counts => _counts;

        /// <summary>
        /// 升级一个驱动盘时消耗的材料数量。
        /// 原先这个魔法判断散落在 ImprovePanel 和 DriverDiskDataRuntime 两处，这里统一。
        /// </summary>
        private int GetUpgradeCost(int materialId) => materialId == 1 ? 10 : 1;

        public void Init(MaterialDataSo config)
        {
            _runtime.Clear();
            _counts.Clear();
            if (config == null) return;

            foreach (var material in config.materials)
            {
                if (material == null) continue;
                var runtime = new MaterialDataRuntime(material);
                _runtime[runtime.ID] = runtime;
                _counts[runtime.ID] = 0;
            }
        }

        public int GetCount(int materialId) => _counts.GetValueOrDefault(materialId, 0);

        public bool HasEnough(int materialId, int need = -1)
        {
            int cost = need < 0 ? GetUpgradeCost(materialId) : need;
            return GetCount(materialId) >= cost;
        }

        public void Add(int materialId, int delta)
        {
            SetCount(materialId, GetCount(materialId) + delta);
        }

        private void SetCount(int materialId, int total)
        {
            if (total < 0) total = 0;
            _counts[materialId] = total;
            AppContext.Events.EventTrigger(GameEvent.MaterialNumChanged, new MaterialNumChangedArgs(materialId, total));
        }

        /// <summary>扣除升级消耗；不足时返回 false 且不改变数量</summary>
        public bool TryConsume(int materialId, int need = -1)
        {
            int cost = need < 0 ? GetUpgradeCost(materialId) : need;
            if (!HasEnough(materialId, cost)) return false;
            SetCount(materialId, GetCount(materialId) - cost);
            return true;
        }

        public MaterialDataRuntime GetRuntime(int materialId) => _runtime.GetValueOrDefault(materialId);
    }
}