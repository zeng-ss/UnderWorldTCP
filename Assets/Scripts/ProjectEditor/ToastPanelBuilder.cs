using HotUpdate.UI.UIItem;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // =====================================================================
    // 左下角提示条：ToastPanel（全局常驻，滑入淡入 + 停留 + 淡出 + 对象池）
    // 预制体里放一条隐藏的模板，运行时由 ToastPanel 克隆复用
    // =====================================================================

    public static class ToastPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/ToastPanel.prefab";

        private const float ItemWidth = 360f;
        private const float ItemHeight = 56f;

        private static readonly Color ColItemBg = new Color(0.02f, 0.02f, 0.03f, 0.92f);

        [MenuItem("Tools/UI/过渡面板/2. 生成 ToastPanel（左下角击杀提示）", false, 76)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("ToastTempCanvas");

            // 根节点只挂逻辑：Canvas 在 Awake 里加（预制体根节点上的 Canvas 会把 RectTransform 全存成 0）
            var root = UIBuilderKit.NewUI("ToastPanel", canvas.transform);
            UIBuilderKit.Stretch((RectTransform)root.transform);
            var panel = root.AddComponent<ToastPanel>();

            // 容器锚在屏幕左下角：第一条就落在它的位置上，后面的往上排
            var container = UIBuilderKit.NewUI("Container", root.transform);
            var crt = (RectTransform)container.transform;
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 0f);
            crt.pivot = new Vector2(0f, 0f);
            crt.sizeDelta = new Vector2(ItemWidth, 0f);
            crt.anchoredPosition = new Vector2(48f, 150f);

            // ---- 模板：深色底 + 左侧荧光条 + 一行文字 ----
            var itemGo = UIBuilderKit.NewUI("Item", container.transform);
            var irt = (RectTransform)itemGo.transform;
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(0f, 0f);
            irt.pivot = new Vector2(0f, 0f);
            irt.sizeDelta = new Vector2(ItemWidth, ItemHeight);
            irt.anchoredPosition = Vector2.zero;

            var bg = itemGo.AddComponent<Image>();
            bg.color = ColItemBg;
            bg.raycastTarget = false;

            var group = itemGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var accent = UIBuilderKit.Rect("Accent", itemGo.transform, UIBuilderKit.Accent);
            var art = accent.rectTransform;
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.sizeDelta = new Vector2(4f, 0f);
            art.anchoredPosition = Vector2.zero;

            var label = UIBuilderKit.Text("Label", itemGo.transform, "击杀哥布林，金币×20", 20,
                UIBuilderKit.TextMain);
            UIBuilderKit.Stretch(label.rectTransform, 22f, 16f, 0f, 0f);

            var item = itemGo.AddComponent<ToastItem>();
            var iso = new SerializedObject(item);
            iso.FindProperty("rect").objectReferenceValue = irt;
            iso.FindProperty("group").objectReferenceValue = group;
            iso.FindProperty("label").objectReferenceValue = label;
            iso.ApplyModifiedPropertiesWithoutUndo();

            var pso = new SerializedObject(panel);
            pso.FindProperty("container").objectReferenceValue = crt;
            pso.FindProperty("template").objectReferenceValue = item;
            pso.ApplyModifiedPropertiesWithoutUndo();

            // 模板不进可视区，运行时克隆时才 SetActive(true)
            itemGo.SetActive(false);

            UIBuilderKit.Save(root, PrefabPath, "ToastPanel", "ToastPanelBuilder", "UIPanel");
            Debug.Log("[ToastPanelBuilder] 已生成 ToastPanel（地址 ToastPanel）。改配色改这里重跑即可。");
        }
    }
}
