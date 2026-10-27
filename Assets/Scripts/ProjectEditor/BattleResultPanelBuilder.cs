using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 战斗结算面板：居中卡片 + 胜/负标题 + 用时 / 击杀 / 获得 + 返回大厅
    public static class BattleResultPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/BattleResultPanel.prefab";

        [MenuItem("Tools/UI/小面板/10. 生成 BattleResultPanel（战斗结算）", false, 69)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("BattleResultPanel", canvas.transform);
            var panel = root.AddComponent<BattleResultPanel>();

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(720f, 460f));

            // 顶部一条强调色，胜负运行时换色（胜利荧光 / 失败红）
            var bar = UIBuilderKit.Rect("TitleBar", card.transform, UIBuilderKit.Accent);
            UIBuilderKit.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(720f, 4f), Vector2.zero);

            var title = UIBuilderKit.Text("Title", card.transform, "战斗胜利", 42, UIBuilderKit.Accent,
                TextAlignmentOptions.Center);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(600f, 60f), new Vector2(0f, -54f));

            var sub = UIBuilderKit.Text("Sub", card.transform, "BATTLE RESULT", 14, UIBuilderKit.TextDim,
                TextAlignmentOptions.Center);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(600f, 22f), new Vector2(0f, -102f));

            var divider = UIBuilderKit.Rect("Divider", card.transform, UIBuilderKit.LineColor);
            UIBuilderKit.Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(600f, 1f), new Vector2(0f, -132f));

            var duration = AddRow(card.transform, "用时", -168f);
            var kills = AddRow(card.transform, "击杀", -222f);
            var reward = AddRow(card.transform, "获得", -276f);

            var back = UIBuilderKit.Button("BackBtn", card.transform, "返回大厅", true);
            UIBuilderKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(280f, 58f), new Vector2(0f, 44f));

            var so = new SerializedObject(panel);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("titleBar").objectReferenceValue = bar;
            so.FindProperty("durationText").objectReferenceValue = duration;
            so.FindProperty("killText").objectReferenceValue = kills;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.FindProperty("backBtn").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();

            UIBuilderKit.Save(root, PrefabPath, "BattleResultPanel", "BattleResultPanelBuilder");
            Debug.Log("[BattleResultPanelBuilder] 已生成 BattleResultPanel（地址 BattleResultPanel）。");
        }

        // 一行：左标签 + 右数值 + 底部分隔线，返回数值文本给面板刷新
        private static TMP_Text AddRow(Transform parent, string label, float y)
        {
            var name = UIBuilderKit.Text(label + "Label", parent, label, 22, UIBuilderKit.TextDim);
            UIBuilderKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(200f, 30f), new Vector2(80f, y));

            var value = UIBuilderKit.Text(label + "Value", parent, "—", 24, UIBuilderKit.TextMain,
                TextAlignmentOptions.MidlineRight);
            UIBuilderKit.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(440f, 30f), new Vector2(-80f, y));

            var line = UIBuilderKit.Rect(label + "Line", parent, new Color(1f, 1f, 1f, 0.06f));
            UIBuilderKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(560f, 1f), new Vector2(0f, y - 32f));

            return value;
        }
    }
}
