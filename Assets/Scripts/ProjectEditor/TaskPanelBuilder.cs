using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 任务面板：全屏遮罩 + 居中卡片 + 任务列表（taskContent 下运行时实例化 TaskItem）
    public static class TaskPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/TaskPanel.prefab";

        [MenuItem("Tools/UI/小面板/5. 生成 TaskPanel（任务）", false, 64)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("TaskPanel", canvas.transform);

            // TaskPanel.Awake 里 GetComponent<CanvasGroup>()，并靠它做卷轴淡入
            root.AddComponent<CanvasGroup>();
            var panel = root.AddComponent<TaskPanel>();

            // BasePanel 只在根的直接子级找 CloseBtn
            var close = UIBuilderKit.CloseButton(root.transform);
            UIBuilderKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f),
                new Vector2(44f, 44f), new Vector2(470f, 370f));

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(900f, 700f));

            var title = UIBuilderKit.Text("Title", card.transform, "任  务", 40, UIBuilderKit.TextMain);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(500f, 60f), new Vector2(44f, -40f));

            var sub = UIBuilderKit.Text("Sub", card.transform, "TASKS", 16, UIBuilderKit.Accent);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(500f, 24f), new Vector2(46f, -96f));

            var line = UIBuilderKit.Line("Line", card.transform, false, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0f),
                new Vector2(812f, 1f), new Vector2(0f, -124f));

            // ---- 任务列表 ----
            var scrollGo = UIBuilderKit.NewUI("TaskScroll", card.transform);
            var srt = (RectTransform)scrollGo.transform;
            UIBuilderKit.Stretch(srt, 44f, 44f, 40f, 140f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = UIBuilderKit.NewUI("Viewport", scrollGo.transform);
            UIBuilderKit.Stretch((RectTransform)viewport.transform, 0f, 18f, 0f, 0f);
            viewport.AddComponent<RectMask2D>();

            var content = UIBuilderKit.NewUI("Content", viewport.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = crt;

            // 纯色滚动条
            var barGo = UIBuilderKit.NewUI("Scrollbar", scrollGo.transform);
            var brt = (RectTransform)barGo.transform;
            brt.anchorMin = new Vector2(1f, 0f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.sizeDelta = new Vector2(10f, 0f);
            brt.anchoredPosition = Vector2.zero;

            var track = barGo.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.03f);

            var handle = UIBuilderKit.NewUI("Handle", barGo.transform);
            UIBuilderKit.Stretch((RectTransform)handle.transform, 2f, 2f, 2f, 2f);
            var himg = handle.AddComponent<Image>();
            himg.color = new Color(1f, 1f, 1f, 0.16f);

            var scrollbar = barGo.AddComponent<Scrollbar>();
            scrollbar.handleRect = (RectTransform)handle.transform;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = himg;
            scroll.verticalScrollbar = scrollbar;

            panel.taskContent = content.transform;

            UIBuilderKit.Save(root, PrefabPath, "TaskPanel", "TaskPanelBuilder");
        }
    }
}
