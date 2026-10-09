using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Data.Config;
using HotUpdate.UI.UIPanel;

namespace HotUpdate.Service
{
    /// <summary>
    /// 驱动盘（仓库）服务。拥有「已拥有」和「已装备」两份列表，并对外广播变化。
    /// 驱动盘的静态模板来自 Luban 导出的 tbdriverdisk.json（客户端不再有本地 SO 配置）。
    /// </summary>
    public class DepotService
    {
        private DepotPanel _depotPanel;

        // 驱动盘模板
        private readonly Dictionary<int, DriverDiskRow> _templates = new();

        private const int MaxEquipSlots = 5;

        /// <summary>已拥有的驱动盘</summary>
        public List<DriverDiskDataRuntime> Owned { get; } = new();

        /// <summary>已装备的驱动盘</summary>
        public List<DriverDiskDataRuntime> Equipped { get; } = new();

        public bool HasFreeEquipSlot => Equipped.Count < MaxEquipSlots;

        /// <summary>重建驱动盘模板表，并清空本地背包</summary>
        public void Init(IReadOnlyList<DriverDiskRow> rows)
        {
            _templates.Clear();
            Owned.Clear();
            Equipped.Clear();

            if (rows == null) return;
            foreach (DriverDiskRow row in rows)
            {
                if (row == null) continue;
                _templates[row.depotId] = row;
            }
        }

        #region 拥有列表

        /// <summary>按 Luban 模板发放驱动盘</summary>
        public void AddByTemplate(DriverDiskRow template, int count = 1)
        {
            if (template == null) return;
            for (int i = 0; i < count; i++)
            {
                Owned.Add(new DriverDiskDataRuntime(template));
            }

            RefreshDepotPanel();
        }

        /// <summary>按配置 id 发放，找不到模板直接返回 0</summary>
        public int AddByDepotId(int depotId, int count = 1)
        {
            var template = FindTemplate(depotId);
            if (template == null) return 0;
            AddByTemplate(template, count);
            return count;
        }

        public bool Remove(DriverDiskDataRuntime item)
        {
            if (item == null) return false;
            Unequip(item);
            if (!Owned.Remove(item)) return false;
            RefreshDepotPanel();
            return true;
        }

        private DriverDiskRow FindTemplate(int depotId) => _templates.GetValueOrDefault(depotId);

        #endregion

        #region 装备 / 卸下

        public bool IsEquipped(DriverDiskDataRuntime item)
        {
            return item != null && Equipped.Contains(item);
        }

        public bool Equip(DriverDiskDataRuntime item)
        {
            if (item == null || IsEquipped(item)) return false;
            if (!Owned.Contains(item) || !HasFreeEquipSlot) return false;

            Equipped.Add(item);
            NotifyEquippedChanged();
            return true;
        }

        public bool Unequip(DriverDiskDataRuntime item)
        {
            if (item == null || !Equipped.Remove(item)) return false;
            NotifyEquippedChanged();
            return true;
        }

        public void UnequipAll()
        {
            if (Equipped.Count == 0) return;
            Equipped.Clear();
            NotifyEquippedChanged();
        }

        #endregion

        // 同模块 data→view：仓库列表变化直接通知仓库面板刷新；面板没开就跳过（下次 OnEnable 会重建）
        private void RefreshDepotPanel()
        {
            _depotPanel = AppContext.Ui.GetPanel<DepotPanel>();
            if (_depotPanel == null) return;
            _depotPanel.Refresh();
        }

        private void NotifyEquippedChanged()
        {
            AppContext.PlayerData.ApplyEquipped(Equipped);
            _depotPanel = AppContext.Ui.GetPanel<DepotPanel>();
            if (_depotPanel == null) return;
            _depotPanel.OnEquippedChanged();
        }
    }
}