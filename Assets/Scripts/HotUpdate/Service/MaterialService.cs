using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Data.Config;
using HotUpdate.UI.UIPanel;

namespace HotUpdate.Service
{
    /// <summary>
    /// 材料数据服务。
    /// 静态定义来自 Luban 导出的 tbmaterialdata.json（客户端不再有本地 SO 配置），持有数量属于玩家状态。
    /// 数量变化时直接通知强化面板刷新（同模块 data→view），不再走全局事件。
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
        /// 1 个金币抵 10 点，其余驱动材料 1 个抵 1 点。
        /// </summary>
        private int GetUpgradeCost(int materialId) => materialId == ItemCatalog.Gold ? 10 : 1;

        /// <summary>从 materialData 配置行重建静态配置，并清零本地持有数量</summary>
        public void Init(IReadOnlyList<MaterialDataRow> rows)
        {
            _runtime.Clear();
            _counts.Clear();
            if (rows == null) return;

            foreach (MaterialDataRow row in rows)
            {
                if (row == null) continue;
                var runtime = new MaterialDataRuntime(row);
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
            // 同模块 data→view：直接让强化面板刷新数量；面板没开则跳过
            var panel = AppContext.Ui.GetPanel<ImprovePanel>();
            if (panel != null) panel.UpdateMaterialNum();
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
