using System.Collections.Generic;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 仓库面板：左边驱动盘列表（Grid），右边详情 + 装备槽
    public static class DepotPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/DepotPanel.prefab";

        [MenuItem("Tools/UI/小面板/6. 生成 DepotPanel（背包）", false, 65)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("DepotPanel", canvas.transform);
            var panel = root.AddComponent<DepotPanel>();

            var close = UIBuilderKit.CloseButton(root.transform);
            UIBuilderKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f),
                new Vector2(44f, 44f), new Vector2(820f, 450f));

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(1600f, 860f));

            // ---- 标题 ----
            var title = UIBuilderKit.Text("Title", card.transform, "驱动盘", 40, UIBuilderKit.TextMain);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(500f, 60f), new Vector2(48f, -40f));

            var sub = UIBuilderKit.Text("Sub", card.transform, "DRIVE DISCS", 16, UIBuilderKit.Accent);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(500f, 24f), new Vector2(50f, -96f));

            var topLine = UIBuilderKit.Line("TopLine", card.transform, false, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(topLine.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0f),
                new Vector2(1504f, 1f), new Vector2(0f, -124f));

            // ---- 左：仓库列表 ----
            var listGo = UIBuilderKit.NewUI("DepotScroll", card.transform);
            UIBuilderKit.Place((RectTransform)listGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(760f, 660f), new Vector2(-760f, -40f));

            var scroll = listGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = UIBuilderKit.NewUI("Viewport", listGo.transform);
            UIBuilderKit.Stretch((RectTransform)viewport.transform, 0f, 16f, 0f, 0f);
            viewport.AddComponent<RectMask2D>();

            var content = UIBuilderKit.NewUI("Content", viewport.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(170f, 170f);
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = crt;

            var barGo = UIBuilderKit.NewUI("Scrollbar", listGo.transform);
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

            // ---- 右：详情 ----
            var divider = UIBuilderKit.Line("Divider", card.transform, true, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1f, 660f), new Vector2(-336f, -40f));

            var iconBg = UIBuilderKit.Rect("IconBg", card.transform, new Color(1f, 1f, 1f, 0.045f));
            UIBuilderKit.Place(iconBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(220f, 220f), new Vector2(60f, 200f));

            var icon = UIBuilderKit.Rect("DepotIcon", iconBg.transform, Color.clear);
            UIBuilderKit.Stretch(icon.rectTransform, 12f, 12f, 12f, 12f);
            icon.preserveAspect = true;

            var name = UIBuilderKit.Text("DepotName", card.transform, "未选择驱动盘", 34, UIBuilderKit.TextMain);
            UIBuilderKit.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 46f), new Vector2(220f, 268f));

            var baseType = UIBuilderKit.Text("BaseTypeText", card.transform, "-", 16, UIBuilderKit.Accent);
            UIBuilderKit.Place(baseType.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 24f), new Vector2(220f, 228f));

            var level = UIBuilderKit.Text("LevelText", card.transform, "Lv.0", 18, UIBuilderKit.TextDim);
            UIBuilderKit.Place(level.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 26f), new Vector2(220f, 190f));

            var line = UIBuilderKit.Line("DetailLine", card.transform, false, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(520f, 1f), new Vector2(150f, 160f));

            // 五行属性
            var statBase = UIBuilderKit.Text("BaseText", card.transform, "基础攻击  —", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(statBase.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 30f), new Vector2(220f, 118f));

            var statAtk = UIBuilderKit.Text("AttackText", card.transform, "攻击力  —", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(statAtk.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 30f), new Vector2(220f, 80f));

            var statHp = UIBuilderKit.Text("HealthText", card.transform, "生命值  —", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(statHp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 30f), new Vector2(220f, 42f));

            var statDef = UIBuilderKit.Text("DefenseText", card.transform, "防御力  —", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(statDef.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 30f), new Vector2(220f, 4f));

            var statCrit = UIBuilderKit.Text("BaoJiText", card.transform, "暴击率  —", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(statCrit.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(520f, 30f), new Vector2(220f, -34f));

            // ---- 装备槽：5 个纯色方格 ----
            var slotLabel = UIBuilderKit.Text("SlotLabel", card.transform, "已装备", 18, UIBuilderKit.TextDim);
            UIBuilderKit.Place(slotLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(300f, 26f), new Vector2(220f, -100f));

            var slots = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                var slot = UIBuilderKit.Rect($"Slot{i}", card.transform, new Color(1f, 1f, 1f, 0.045f));
                UIBuilderKit.Place(slot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(86f, 86f), new Vector2(220f + i * 100f, -166f));
                slots.Add(slot.gameObject);
            }

            panel.depotIcon = icon;
            panel.depotName = name;
            panel.depotLevelText = level;
            panel.depotBaseTypeText = baseType;
            panel.depotBaseText = statBase;
            panel.depotAttackText = statAtk;
            panel.depotHealthText = statHp;
            panel.depotDefenseText = statDef;
            panel.depotBaoJiText = statCrit;
            panel.content = content;
            panel.contentList = slots;

            UIBuilderKit.Save(root, PrefabPath, "DepotPanel", "DepotPanelBuilder");
        }
    }
}
