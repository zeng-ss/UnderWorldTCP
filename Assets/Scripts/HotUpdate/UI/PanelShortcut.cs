using System;
using System.Linq;
using HotUpdate.Event;
using HotUpdate.RedDot;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI
{
    /// <summary>能被快捷键 / HUD 快捷栏打开的面板。</summary>
    public enum PanelId
    {
        Chat,
        Depot,
        PlayerData,
        Friend,
        Task,
    }

    /// <summary>
    /// 一条「打开 / 关闭某面板」的快捷键定义：面板 + 按键 + 中文名 + 图标 + 红点 + 开关动作。
    ///
    /// 这是全项目唯一一份面板快捷键总表（<see cref="All"/>）：
    ///   键盘 —— GameController 遍历注册；
    ///   HUD  —— 快捷格摆在预制体上（见 PlayerProPanelBuilder），Awake 时按 <see cref="Get"/> 认领自己的开关动作与红点。
    /// 新增面板只在这里加一条，两处都会跟上。
    /// </summary>
    public class PanelShortcut
    {
        // 这条快捷键对应哪个面板（HUD 快捷格就是按它认领开关动作的）
        public readonly PanelId Id;

        // 触发这个面板的按键
        public readonly KeyCode Key;

        // 快捷栏上显示的中文名（如「背包」）
        public readonly string Label;

        // 图标贴图在 Assets/Resources/SharpUI/Textures 下的名字（不带扩展名）
        public readonly string IconName;

        // HUD 上这一格的红点看哪根节点（路径常量见 RedDotPath）
        public readonly string DotPath;

        // 按下按键 / 点击图标时执行的开关注作
        public readonly Action Toggle;

        private PanelShortcut(PanelId id, KeyCode key, string label, string iconName, string dotPath, Action toggle)
        {
            Id = id;
            Key = key;
            Label = label;
            IconName = iconName;
            DotPath = dotPath;
            Toggle = toggle;
        }

        // 角标上显示的按键文本（Tab / C / V / B / G）
        public string KeyLabel => Key == KeyCode.Tab ? "Tab" : Key.ToString();

        /// <summary>面板快捷键总表。数组顺序即 HUD 快捷栏里的排列顺序。</summary>
        public static readonly PanelShortcut[] All =
        {
            new(PanelId.Chat, KeyCode.C, "聊天", "icon_info", RedDotPath.Chat, () => AppContext.Ui.TogglePanel<ChatPanel>()),
            new(PanelId.Depot, KeyCode.V, "背包", "icon_bottle", RedDotPath.Depot, () => AppContext.Ui.TogglePanel<DepotPanel>()),
            new(PanelId.PlayerData, KeyCode.B, "角色", "icon_warrior", RedDotPath.PlayerData, () => AppContext.Ui.TogglePanel<PlayerDataPanel>()),
            new(PanelId.Friend, KeyCode.G, "好友", "icon_male", RedDotPath.Friend, () => AppContext.Ui.TogglePanel<FriendPanel>()),
            new(PanelId.Task, KeyCode.Tab, "任务", "icon_question", RedDotPath.Task, ToggleTaskPanel),
        };

        // 按面板取快捷键定义
        public static PanelShortcut Get(PanelId id) => All.FirstOrDefault(p => p.Id == id);

        // 任务面板自带卷轴开关动画（走自己的 ClosePanel），不能套用通用的 Hide，单独处理；
        // 同时补上 CursorShow / CursorHide —— 通用 TogglePanel 会发这两个事件，这里要对齐，
        // 否则按 Tab 打开任务面板时光标还是锁死的，点不到「领取」。
        private static void ToggleTaskPanel()
        {
            TaskPanel panel = AppContext.Ui.GetPanel<TaskPanel>();
            if (panel == null || !panel.gameObject.activeInHierarchy)
            {
                AppContext.Ui.OpenPanel<TaskPanel>();
                AppContext.Events.EventTrigger(GameEvent.CursorShow);
            }
            else
            {
                panel.ClosePanel();
                AppContext.Events.EventTrigger(GameEvent.CursorHide);
            }
        }
    }
}