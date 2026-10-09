using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Data.Config;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Service
{
    /// <summary>
    /// 材料数据服务。
    /// 静态定义来自 Luban 导出的 tbmaterialdata.json，持有数量以服务端 role_bag_item 为准：
    /// 进游戏下发、升级扣料回包、领奖后回拉，客户端只负责显示，不再自己增减数量。
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
        /// 与服务端 LoginModle.GetUpgradeCost 保持一致：1 个金币抵 10 点，其余驱动材料 1 个抵 1 点。
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

        #region 服务端数据落地

        /// <summary>用服务端下发的全量背包重建材料数量（进游戏 / 领奖后回拉时调用）</summary>
        public void ApplyServerBag(IEnumerable<MaterialInfo> materials)
        {
            // 先把已知材料清零：服务端没下发的即为 0
            foreach (int materialId in new List<int>(_counts.Keys)) _counts[materialId] = 0;

            if (materials != null)
            {
                foreach (MaterialInfo info in materials)
                {
                    if (info == null) continue;
                    _counts[info.MaterialId] = Mathf.Max(0, info.MaterialCount);
                }
            }

            RefreshImprovePanel();
        }

        /// <summary>应用服务端回包的「材料 → 剩余数量」增量（升级扣料后用）</summary>
        public void ApplyMaterialCounts(IEnumerable<KeyValuePair<int, int>> counts)
        {
            if (counts == null) return;

            foreach (var kv in counts) _counts[kv.Key] = Mathf.Max(0, kv.Value);
            RefreshImprovePanel();
        }

        #endregion

        public int GetCount(int materialId) => _counts.GetValueOrDefault(materialId, 0);

        /// <summary>数量是否够一次升级消耗。只用于本地粗判，真正的扣除在服务端</summary>
        public bool HasEnough(int materialId, int need = -1)
        {
            int cost = need < 0 ? GetUpgradeCost(materialId) : need;
            return GetCount(materialId) >= cost;
        }

        public MaterialDataRuntime GetRuntime(int materialId) => _runtime.GetValueOrDefault(materialId);

        // 同模块 data→view：直接让强化面板刷新数量；面板没开则跳过
        private void RefreshImprovePanel()
        {
            var panel = AppContext.Ui.GetPanel<ImprovePanel>();
            if (panel != null) panel.UpdateMaterialNum();
        }
    }
}
