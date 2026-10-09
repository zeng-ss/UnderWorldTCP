using System.Collections.Generic;
using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.Data.Config;
using HotUpdate.UI.UIPanel;
using UnityEngine;

namespace HotUpdate.Service
{
    /// <summary>
    /// 驱动盘（仓库）服务。
    /// 「已拥有 / 已装备 / 等级 / 词条」全部来自服务端 role_bag_item（进游戏下发、升级回包、领奖后回拉），
    /// 客户端只保留 Luban 模板用于显示与展示材料配方，不再自己发放或改动驱动盘。
    /// </summary>
    public class DepotService
    {
        private DepotPanel _depotPanel;

        // 驱动盘模板（名称 / 图标 / 升级配方 / 类型 / 初始数值），只读
        private readonly Dictionary<int, DriverDiskRow> _templates = new();

        private const int MaxEquipSlots = 5;

        /// <summary>已拥有的驱动盘（每个模板一条，数量与穿戴状态同样来自服务端）</summary>
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

        #region 服务端数据落地

        /// <summary>
        /// 用服务端下发的全量背包重建「已拥有 / 已装备」。
        /// 进游戏（StartGame）与领奖后（BagInfo）都走这里 —— 背包内容以服务端为权威。
        /// </summary>
        public void ApplyServerBag(IEnumerable<DriverDiskInfo> disks)
        {
            Owned.Clear();
            Equipped.Clear();

            if (disks != null)
            {
                foreach (DriverDiskInfo info in disks)
                {
                    if (info == null) continue;

                    var template = FindTemplate(info.DriverDiskId);
                    if (template == null)
                    {
                        Debug.LogWarning($"DepotService: 服务端下发了配置表里不存在的驱动盘 {info.DriverDiskId}，已跳过");
                        continue;
                    }

                    var runtime = new DriverDiskDataRuntime(template, info);
                    Owned.Add(runtime);
                    if (info.IsEquipped && Equipped.Count < MaxEquipSlots) Equipped.Add(runtime);
                }
            }

            RefreshDepotPanel();
            AppContext.PlayerData.ApplyEquipped(Equipped);
        }

        /// <summary>用服务端回包覆盖某个已拥有驱动盘的数据（升级后调用），并刷新仓库面板</summary>
        public void ApplyDriverDiskData(DriverDiskInfo info)
        {
            if (info == null) return;

            var runtime = FindOwned(info.DriverDiskId);
            if (runtime == null) return;

            runtime.ApplyFrom(info);
            RefreshDepotPanel();
        }

        /// <summary>按驱动盘模板 id 找已拥有的实例，没有返回 null</summary>
        public DriverDiskDataRuntime FindOwned(int depotId) => Owned.Find(v => v.DepotId == depotId);

        private DriverDiskRow FindTemplate(int depotId) => _templates.GetValueOrDefault(depotId);

        #endregion

        #region 装备 / 卸下

        public bool IsEquipped(DriverDiskDataRuntime item)
        {
            return item != null && Equipped.Contains(item);
        }

        /// <summary>落地「已装备」状态（服务端确认成功后才调用）</summary>
        public bool Equip(DriverDiskDataRuntime item)
        {
            if (item == null || IsEquipped(item)) return false;
            if (!Owned.Contains(item) || !HasFreeEquipSlot) return false;

            Equipped.Add(item);
            NotifyEquippedChanged();
            return true;
        }

        /// <summary>落地「卸下」状态（服务端确认成功后才调用）</summary>
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