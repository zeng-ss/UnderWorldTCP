using HotUpdate.Controller;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // =====================================================================
    // 对话面板 / 驱动盘强化面板（纯色重建）
    // 菜单：Tools/UI/小面板/8、9
    // =====================================================================

    public static class DialoguePanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/DialoguePanel.prefab";

        private const float SideWidth = 1000f;
        private const float SideHeight = 360f;

        [MenuItem("Tools/UI/小面板/8. 生成 DialoguePanel（对话）", false, 67)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");

            // 对话是叠在游戏画面上的，根节点不铺底、不吃点击
            var root = UIBuilderKit.NewUI("DialoguePanel", canvas.transform);
            UIBuilderKit.Stretch((RectTransform)root.transform);

            var panel = root.AddComponent<DialoguePanel>();
            var ctrl = root.AddComponent<DialogueController>();

            var left = BuildSide(root.transform, "Left", true, out var leftName, out var leftText,
                out var leftPortrait, out var leftOptions);
            var right = BuildSide(root.transform, "Right", false, out var rightName, out var rightText,
                out var rightPortrait, out var rightOptions);

            // 底部提示（“点击空格继续”）
            var tip = UIBuilderKit.Text("Tip", root.transform, "", 20, UIBuilderKit.TextDim,
                TextAlignmentOptions.Center);
            UIBuilderKit.Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(700f, 30f), new Vector2(0f, 26f));

            ctrl.leftSide = new DialogueController.DialogueSide
            {
                rootObject = left,
                portraitArea = leftPortrait.rectTransform,
                canvasGroup = left.GetComponent<CanvasGroup>()
            };
            ctrl.rightSide = new DialogueController.DialogueSide
            {
                rootObject = right,
                portraitArea = rightPortrait.rectTransform,
                canvasGroup = right.GetComponent<CanvasGroup>()
            };

            panel.dialogueController = ctrl;
            panel.speakerNameTextL = leftName;
            panel.dialogueTextL = leftText;
            panel.speakerPortraitImageL = leftPortrait;
            panel.optionsPanelL = leftOptions;
            panel.speakerNameTextR = rightName;
            panel.dialogueTextR = rightText;
            panel.speakerPortraitImageR = rightPortrait;
            panel.optionsPanelR = rightOptions;
            panel.tipText = tip;

            UIBuilderKit.Save(root, PrefabPath, "DialoguePanel", "DialoguePanelBuilder");
        }

        // 一侧：底带 + 强调条 + 立绘 + 名字 + 正文 + 选项容器
        private static GameObject BuildSide(Transform parent, string name, bool left,
            out TMP_Text nameText, out TMP_Text bodyText, out Image portrait, out Transform options)
        {
            var side = UIBuilderKit.NewUI(name, parent);
            var srt = (RectTransform)side.transform;
            UIBuilderKit.Place(srt,
                new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(SideWidth, SideHeight), Vector2.zero);

            side.AddComponent<CanvasGroup>().alpha = 1f;

            var bg = UIBuilderKit.Rect("Bg", side.transform, new Color(0.02f, 0.02f, 0.03f, 0.9f));
            UIBuilderKit.Stretch(bg.rectTransform);

            // 强调条贴在外侧边缘
            var bar = UIBuilderKit.Rect("Accent", side.transform, UIBuilderKit.Accent);
            UIBuilderKit.Place(bar.rectTransform, new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(4f, SideHeight - 40f), Vector2.zero);

            // 立绘：靠外侧
            var head = UIBuilderKit.Rect("Portrait", side.transform, new Color(1f, 1f, 1f, 0.04f));
            head.preserveAspect = true;
            UIBuilderKit.Place(head.rectTransform, new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 0f : 1f, 0.5f),
                new Vector2(240f, 300f), new Vector2(left ? 34f : -34f, 0f));

            // 文字块：立绘内侧
            Vector2 anchor = new Vector2(left ? 0f : 1f, 1f);
            Vector2 pivot = new Vector2(left ? 0f : 1f, 1f);
            float inset = left ? 306f : -306f;

            nameText = UIBuilderKit.Text("Name", side.transform, "", 32, UIBuilderKit.Accent);
            UIBuilderKit.Place(nameText.rectTransform, anchor, pivot,
                new Vector2(560f, 46f), new Vector2(inset, -22f));

            bodyText = UIBuilderKit.Text("Content", side.transform, "", 24, UIBuilderKit.TextMain,
                TextAlignmentOptions.TopLeft, true);
            UIBuilderKit.Place(bodyText.rectTransform, anchor, pivot,
                new Vector2(620f, 166f), new Vector2(inset, -78f));

            var opt = UIBuilderKit.NewUI("Options", side.transform);
            var ort = (RectTransform)opt.transform;
            UIBuilderKit.Place(ort, anchor, pivot, new Vector2(500f, 86f), new Vector2(inset, -258f));

            var layout = opt.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;

            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            portrait = head;
            options = opt.transform;

            // 右侧的名字/正文跟着镜像，但仍左对齐，避免和立绘挤在一起
            return side;
        }
    }

    public static class ImprovePanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/ImprovePanel.prefab";

        [MenuItem("Tools/UI/小面板/9. 生成 ImprovePanel（驱动盘强化）", false, 68)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("ImprovePanel", canvas.transform);
            var panel = root.AddComponent<ImprovePanel>();

            var close = UIBuilderKit.CloseButton(root.transform);
            UIBuilderKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f),
                new Vector2(44f, 44f), new Vector2(410f, 430f));

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(780f, 820f));

            // ---- 图标 ----
            var iconBg = UIBuilderKit.Rect("IconBg", card.transform, new Color(1f, 1f, 1f, 0.045f));
            UIBuilderKit.Place(iconBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(180f, 180f), new Vector2(0f, -36f));

            var icon = UIBuilderKit.Rect("DepotIcon", iconBg.transform, Color.clear);
            UIBuilderKit.Stretch(icon.rectTransform, 10f, 10f, 10f, 10f);
            icon.preserveAspect = true;

            // ---- 名称 / 类型 / 等级 ----
            var name = UIBuilderKit.Text("DepotName", card.transform, "未选择驱动盘", 32,
                UIBuilderKit.TextMain, TextAlignmentOptions.Center);
            UIBuilderKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(620f, 44f), new Vector2(0f, -228f));

            var baseType = UIBuilderKit.Text("BaseTypeText", card.transform, "-", 18,
                UIBuilderKit.Accent, TextAlignmentOptions.Center);
            UIBuilderKit.Place(baseType.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(620f, 26f), new Vector2(0f, -276f));

            var level = UIBuilderKit.Text("LevelText", card.transform, "Lv.0", 18,
                UIBuilderKit.TextDim, TextAlignmentOptions.Center);
            UIBuilderKit.Place(level.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(620f, 26f), new Vector2(0f, -306f));

            var line = UIBuilderKit.Line("Line", card.transform, false, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                new Vector2(660f, 1f), new Vector2(0f, -344f));

            // ---- 五条属性 ----
            var statBase = AddStat(card.transform, "BaseText", -376f);
            var statAtk = AddStat(card.transform, "AttackText", -418f);
            var statHp = AddStat(card.transform, "HealthText", -460f);
            var statDef = AddStat(card.transform, "DefenseText", -502f);
            var statCrit = AddStat(card.transform, "BaoJiText", -544f);

            // ---- 经验条 ----
            var expText = UIBuilderKit.Text("FillText", card.transform, "0/0", 18,
                UIBuilderKit.TextDim, TextAlignmentOptions.Center);
            UIBuilderKit.Place(expText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(620f, 24f), new Vector2(0f, -580f));

            var fill = UIBuilderKit.Bar("ExpBar", card.transform, new Vector2(660f, 16f),
                new Vector2(0.5f, 1f), new Vector2(0f, -612f),
                new Color(1f, 1f, 1f, 0.08f), UIBuilderKit.Accent);

            // ---- 材料图标行（运行时由 ImprovePanel 往里塞 Image） ----
            var icons = UIBuilderKit.NewUI("Materials", card.transform);
            var irt = (RectTransform)icons.transform;
            UIBuilderKit.Place(irt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(660f, 64f), new Vector2(0f, -660f));

            var row = icons.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            row.spacing = 10f;

            // ---- 强化按钮 ----
            var addExp = UIBuilderKit.Button("AddExpBtn", card.transform, "强化", true);
            UIBuilderKit.Place((RectTransform)addExp.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(300f, 56f), new Vector2(0f, 34f));

            panel.depotIcon = icon;
            panel.depotName = name;
            panel.depotLevelText = level;
            panel.depotBaseTypeText = baseType;
            panel.depotBaseText = statBase;
            panel.depotAttackText = statAtk;
            panel.depotHealthText = statHp;
            panel.depotDefenseText = statDef;
            panel.depotBaoJiText = statCrit;
            panel.fillImage = fill;
            panel.fillText = expText;
            panel.iconsContainer = icons;
            panel.addExpBtn = addExp;

            UIBuilderKit.Save(root, PrefabPath, "ImprovePanel", "ImprovePanelBuilder");
        }

        private static TMP_Text AddStat(Transform parent, string name, float y)
        {
            var txt = UIBuilderKit.Text(name, parent, "—", 20, UIBuilderKit.TextDim);
            UIBuilderKit.Place(txt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(620f, 32f), new Vector2(0f, y));
            return txt;
        }
    }
}
