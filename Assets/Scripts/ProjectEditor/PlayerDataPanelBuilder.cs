using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 角色属性面板：居中卡片 + 五行数值
    public static class PlayerDataPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/PlayerDataPanel.prefab";

        [MenuItem("Tools/UI/小面板/7. 生成 PlayerDataPanel（角色属性）", false, 66)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("PlayerDataPanel", canvas.transform);
            var panel = root.AddComponent<PlayerDataPanel>();

            var close = UIBuilderKit.CloseButton(root.transform);
            UIBuilderKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(1f, 1f),
                new Vector2(44f, 44f), new Vector2(370f, 330f));

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(740f, 660f));

            var title = UIBuilderKit.Text("Title", card.transform, "角色属性", 38, UIBuilderKit.TextMain);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(400f, 56f), new Vector2(44f, -38f));

            var sub = UIBuilderKit.Text("Sub", card.transform, "STATUS", 16, UIBuilderKit.Accent);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(400f, 24f), new Vector2(46f, -90f));

            var line = UIBuilderKit.Line("Line", card.transform, false, 1f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0f),
                new Vector2(652f, 1f), new Vector2(370f, -118f));

            // 五行属性：左边标签 + 右边数值，中间留空
            var attack = AddRow(card.transform, "AttackText", "攻击力", 110f);
            var health = AddRow(card.transform, "HealthText", "生命值", 30f);
            var defense = AddRow(card.transform, "DefenseText", "防御力", -50f);
            var crit = AddRow(card.transform, "BaoJiText", "暴击率", -130f);
            var ex = AddRow(card.transform, "ExAttackText", "EX 攻击", -210f);

            panel.attackText = attack;
            panel.healthText = health;
            panel.defenseText = defense;
            panel.baoJiText = crit;
            panel.exAttackText = ex;

            UIBuilderKit.Save(root, PrefabPath, "PlayerDataPanel", "PlayerDataPanelBuilder");
        }

        // 一行属性：标签 + 一条虚线分隔 + 数值
        private static TMP_Text AddRow(Transform parent, string valueName, string label, float y)
        {
            var labelText = UIBuilderKit.Text(label + "Label", parent, label, 24, UIBuilderKit.TextDim);
            UIBuilderKit.Place(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(240f, 34f), new Vector2(44f, y));

            var value = UIBuilderKit.Text(valueName, parent, "—", 28, UIBuilderKit.TextMain,
                TextAlignmentOptions.MidlineRight);
            UIBuilderKit.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(240f, 34f), new Vector2(-44f, y));

            var divider = UIBuilderKit.Rect(valueName + "Line", parent, new Color(1f, 1f, 1f, 0.05f));
            UIBuilderKit.Place(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(652f, 1f), new Vector2(0f, y - 30f));

            return value;
        }
    }
}
