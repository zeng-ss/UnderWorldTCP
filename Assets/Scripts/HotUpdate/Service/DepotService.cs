using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Event;
using HotUpdate.UI.UIPanel;

namespace HotUpdate.Service
{
    /// <summary>
    /// 驱动盘（仓库）服务。拥有「已拥有」和「已装备」两份列表，并对外广播变化。
    /// </summary>
    public class DepotService
    {
        private DepotPanel _depotPanel;
        /// <summary>装备槽数量，对应 DepotPanel 上 contentList 的格子数</summary>
        private const int MaxEquipSlots = 5;

        /// <summary>已拥有的驱动盘</summary>
        public List<DriverDiskDataRuntime> Owned { get; } = new();

        /// <summary>已装备的驱动盘</summary>
        public List<DriverDiskDataRuntime> Equipped { get; } = new();

        /// <summary>静态配置引用，发放奖励时需要按 id 找到模板</summary>
        public DepotConfig Config { get; private set; }

        public bool HasFreeEquipSlot => Equipped.Count < MaxEquipSlots;

        public void Init(DepotConfig config)
        {
            Config = config;
            Owned.Clear();
            Equipped.Clear();
        }

        #region 拥有列表

        /// <summary>按配置模板发放驱动盘</summary>
        public void AddByTemplate(DriverDiskData template, int count = 1)
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

        private DriverDiskData FindTemplate(int depotId)
        {
            if (Config == null || Config.depots == null) return null;
            foreach (var depot in Config.depots)
            {
                if (depot != null && depot.depotId == depotId) return depot;
            }

            return null;
        }

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
            _depotPanel ??= AppContext.Ui.GetPanel<DepotPanel>();
            _depotPanel.Refresh();
        }

        private void NotifyEquippedChanged()
        {
            AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
            _depotPanel ??= AppContext.Ui.GetPanel<DepotPanel>();
            _depotPanel.OnEquippedChanged();
        }
    }
}