/// <summary>
/// 仓库 / 强化 / 角色属性 这一组面板的 Controller。
///
/// 这三个面板在业务上强耦合（装备驱动盘 → 属性重算 → 强化消耗材料 → 再重算），
/// 所以放在同一个 Controller 里编排。
///
/// 依赖方向：
///   View（DepotPanel / ImprovePanel）→ Controller（本类）→ Model（各种 Service）
///   Controller → View 不保留引用，数据变化走事件总线（DepotChanged / EquippedChanged /
///   PlayerDataChanged / MaterialNumChanged），面板自己订阅并刷新；
///   一次性提示直接调 UIManager.ShowTip。
///
/// 本类是普通类，由 AppContext 统一创建，不是 MonoBehaviour、不是单例。
/// </summary>
public class DepotController
{
    #region View → Controller

    public void Equip(DriverDiskDataRuntime item)
    {
        if (item == null) return;

        var depot = AppContext.Depot;
        if (depot.Equip(item)) return;

        if (!depot.HasFreeEquipSlot) ShowTip("装备槽已满");
        else if (depot.IsEquipped(item)) ShowTip("该驱动盘已装备");
        else ShowTip("无法装备该驱动盘");
    }

    public void Unequip(DriverDiskDataRuntime item)
    {
        AppContext.Depot.Unequip(item);
    }

    /// <summary>打开强化面板并载入指定驱动盘。面板实例不缓存，用完即弃。</summary>
    public void RequestImprove(DriverDiskDataRuntime item)
    {
        if (item == null) return;

        UIManager.Instance.OpenPanel<ImprovePanel>(panel => panel.UpdateData(item));
    }

    /// <summary>强化：校验材料 → 扣除 → 加经验 → 触发属性重算</summary>
    public void Upgrade(DriverDiskDataRuntime item)
    {
        if (item == null) return;

        if (!item.CheckCanAddExp(out string shortageTip))
        {
            ShowTip($"{shortageTip}不足");
            return;
        }

        foreach (var materialId in item.materialsId)
        {
            AppContext.Material.TryConsume(materialId);
        }

        item.AddExp(200);

        // 升级会改变驱动盘数值，属性要跟着重算；事件会通知属性面板和仓库面板自行刷新
        AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
    }

    /// <summary>调试用：直接加经验，不消耗材料（对应 GameController 里的 Alpha5）</summary>
    public void DebugFill(float amount)
    {
        var item = UIManager.Instance.GetPanel<ImprovePanel>()?.CurrentData;
        if (item == null) return;

        item.AddExp(amount);
        AppContext.PlayerData.ApplyEquipped(AppContext.Depot.Equipped);
    }

    #endregion

    /// <summary>提示条是全局 UI，直接调 UIManager 弹出</summary>
    private void ShowTip(string text)
    {
        UIManager.Instance.ShowTip(text);
    }
}
