using HotUpdate.Core;
using HotUpdate.Data;
using HotUpdate.UI.UIPanel;

namespace HotUpdate.Controller
{
    /// <summary>
    /// 仓库 / 强化 / 角色属性 这一组面板的 Controller。
    /// 装备与升级都是「先请求服务端、服务端落库成功后回包、再落地本地」——
    /// 背包内容（拥有 / 穿戴 / 等级 / 词条 / 材料数量）一律以服务端 role_bag_item 为准。
    /// </summary>
    public class DepotController
    {
        #region View → Controller

        public void Equip(DriverDiskDataRuntime item)
        {
            if (item == null) return;

            var depot = AppContext.Depot;
            if (depot.IsEquipped(item)) return;
            if (!depot.HasFreeEquipSlot)
            {
                ShowTip("装备槽已满");
                return;
            }

            AppContext.Proto.RequestDriverDiskEquip(item.DepotId, true,
                ret => ApplyEquipResult(item, ret, true));
        }

        public void Unequip(DriverDiskDataRuntime item)
        {
            if (item == null) return;

            AppContext.Proto.RequestDriverDiskEquip(item.DepotId, false,
                ret => ApplyEquipResult(item, ret, false));
        }

        private static void ApplyEquipResult(DriverDiskDataRuntime item, DriverDiskEquipRet ret, bool equip)
        {
            if (ret == null || ret.CmdCode != CmdCode.Succeed)
            {
                ShowTip(string.IsNullOrEmpty(ret?.Tip) ? "操作失败" : ret.Tip);
                return;
            }

            if (ret.Equipped) AppContext.Depot.Equip(item);
            else AppContext.Depot.Unequip(item);
        }

        /// <summary>打开强化面板并载入指定驱动盘。面板实例不缓存，用完即弃。</summary>
        public void RequestImprove(DriverDiskDataRuntime item)
        {
            if (item == null) return;

            AppContext.Ui.OpenPanel<ImprovePanel>(panel => panel.UpdateData(item, this));
        }

        /// <summary>
        /// 强化：把「升级哪个驱动盘」交给服务端，服务端校验材料 / 扣除 / 升级词条 / 写 role_bag_item 后回包，
        /// 客户端只用回包覆盖本地实例与材料数量，不做任何本地推算。
        /// </summary>
        public void Upgrade(DriverDiskDataRuntime item)
        {
            if (item == null) return;

            // 本地先粗判一次，省掉一次必然失败的往返；服务端仍会再校验一遍
            if (!item.CheckCanAddExp(out string shortageTip))
            {
                ShowTip($"{shortageTip}不足");
                return;
            }

            AppContext.Proto.RequestDriverDiskUpgrade(item.DepotId, ret => ApplyUpgradeResult(item, ret));
        }

        private static void ApplyUpgradeResult(DriverDiskDataRuntime item, DriverDiskUpgradeRet ret)
        {
            if (ret == null || ret.CmdCode != CmdCode.Succeed)
            {
                ShowTip(string.IsNullOrEmpty(ret?.Tip) ? "升级失败" : ret.Tip);
                return;
            }

            // 升级后等级要先于覆盖前判断：首次升到 2 级（即升过一级）推进对应任务
            int newLevel = ret.DriverDisk != null ? ret.DriverDisk.Level : item.Level;
            bool firstUpgrade = ret.OldLevel < DriverDiskDataRuntime.UpgradedLevel
                                && newLevel >= DriverDiskDataRuntime.UpgradedLevel;

            AppContext.Depot.ApplyDriverDiskData(ret.DriverDisk);
            AppContext.Material.ApplyMaterialCounts(ret.MaterialMap);

            if (firstUpgrade) AppContext.Task.AdvanceByType(TaskType.给每一个驱动盘都升一级);

            // 升级会改变驱动盘数值，属性要跟着重算；订阅者（属性面板 / 强化面板）会自行刷新
            AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
        }

        #endregion

        /// <summary>提示条是全局 UI，直接调 UIManager 弹出</summary>
        private static void ShowTip(string text)
        {
            AppContext.Ui.ShowTip(text);
        }
    }
}
