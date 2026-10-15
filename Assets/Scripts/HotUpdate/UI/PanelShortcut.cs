using System;
using System.Linq;
using HotUpdate.Event;
using HotUpdate.RedDot;
using HotUpdate.UI.UIPanel;
using UnityEngine;
using AppContext = HotUpdate.Core.AppContext;

namespace HotUpdate.UI
{
    // 能被快捷键 / HUD 快捷栏打开的面板
    public enum PanelId
    {
        Chat,
        Depot,
        PlayerData,
        Friend,
        Task,
    }

    public class PanelShortcut
    {
        public readonly PanelId Id;
        public readonly KeyCode Key;
        // HUD 上这一格的红点看哪根节点 => RedDotPath
        public readonly string DotPath;
        public readonly Action Toggle;

        private PanelShortcut(PanelId id, KeyCode key, string dotPath, Action toggle)
        {
            Id = id;
            Key = key;
            DotPath = dotPath;
            Toggle = toggle;
        }

        public static readonly PanelShortcut[] All =
        {
            new(PanelId.Chat, KeyCode.C, RedDotPath.Chat, () => AppContext.Ui.TogglePanel<ChatPanel>()),
            new(PanelId.Depot, KeyCode.V, RedDotPath.Depot, () => AppContext.Ui.TogglePanel<DepotPanel>()),
            new(PanelId.PlayerData, KeyCode.B, RedDotPath.PlayerData, () => AppContext.Ui.TogglePanel<PlayerDataPanel>()),
            new(PanelId.Friend, KeyCode.G, RedDotPath.Friend, () => AppContext.Ui.TogglePanel<FriendPanel>()),
            new(PanelId.Task, KeyCode.Tab, RedDotPath.Task, ToggleTaskPanel),
        };

        // 按面板取快捷键定义
        public static PanelShortcut Get(PanelId id) => All.FirstOrDefault(p => p.Id == id);

        // 任务面板自带卷轴开关动画，不能套用通用的 Hide，单独处理；
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