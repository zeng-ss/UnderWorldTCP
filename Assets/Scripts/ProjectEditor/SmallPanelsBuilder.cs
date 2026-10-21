using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // =====================================================================
    // 小型面板的纯色重建：退出确认 / 全局提示 / 登录 / 注册
    // 菜单：Tools/UI/小面板
    // =====================================================================

    public static class SmallPanelsMenu
    {
        [MenuItem("Tools/UI/小面板/0. 全部重新生成（1~9）", false, 59)]
        public static void BuildAll()
        {
            // 批量时不保留预览，否则九个面板会叠在同一个 Canvas 里互相挡
            UIBuilderKit.KeepPreview = false;
            try
            {
                ExitPanelBuilder.Build();
                TipPanelBuilder.Build();
                LoginPanelBuilder.Build();
                RegisterPanelBuilder.Build();
                TaskPanelBuilder.Build();
                DepotPanelBuilder.Build();
                PlayerDataPanelBuilder.Build();
                DialoguePanelBuilder.Build();
                ImprovePanelBuilder.Build();
            }
            finally
            {
                UIBuilderKit.KeepPreview = true;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[小面板] 全部生成完毕：Exit / Tip / Login / Register / Task / Depot / PlayerData / Dialogue / Improve");
            }
        }
    }

    public static class ExitPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/ExitPanel.prefab";

        [MenuItem("Tools/UI/小面板/1. 生成 ExitPanel（退出确认）", false, 60)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("ExitPanel", canvas.transform);
            var panel = root.AddComponent<ExitPanel>();

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(560f, 300f));

            var title = UIBuilderKit.Text("Title", card.transform, "退出房间", 34, UIBuilderKit.TextMain);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(420f, 48f), new Vector2(40f, -34f));

            var desc = UIBuilderKit.Text("Desc", card.transform, "确定要离开当前房间吗？", 22, UIBuilderKit.TextDim);
            UIBuilderKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(480f, 32f), new Vector2(40f, -96f));

            var cancel = UIBuilderKit.Button("CancelBtn", card.transform, "取消");
            UIBuilderKit.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(150f, 52f), new Vector2(-90f, 44f));

            var exit = UIBuilderKit.Button("ExitBtn", card.transform, "确认退出", true);
            UIBuilderKit.Place((RectTransform)exit.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(150f, 52f), new Vector2(90f, 44f));

            panel.exitBtn = exit;
            panel.cancelBtn = cancel;

            UIBuilderKit.Save(root, PrefabPath, "ExitPanel", "ExitPanelBuilder");
        }
    }

    public static class TipPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/TipPanel.prefab";

        [MenuItem("Tools/UI/小面板/2. 生成 TipPanel（全局提示）", false, 61)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");

            // 提示条不吃点击、不铺底色，只在自己那块区域显示
            var root = UIBuilderKit.NewUI("TipPanel", canvas.transform);
            UIBuilderKit.Stretch((RectTransform)root.transform);

            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var panel = root.AddComponent<TipPanel>();

            var bar = UIBuilderKit.Rect("Bar", root.transform, new Color(0.02f, 0.02f, 0.03f, 0.92f));
            UIBuilderKit.Center(bar.rectTransform, new Vector2(660f, 76f), new Vector2(0f, -280f));

            var accent = UIBuilderKit.Rect("Accent", bar.transform, UIBuilderKit.Accent);
            UIBuilderKit.Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(4f, 44f), new Vector2(16f, 0f));

            var label = UIBuilderKit.Text("TipText", root.transform, "提示", 28,
                UIBuilderKit.TextMain, TextAlignmentOptions.Center);
            UIBuilderKit.Center(label.rectTransform, new Vector2(620f, 60f), new Vector2(0f, -280f));

            panel.tipText = label;
            panel.canvasGroup = group;

            UIBuilderKit.Save(root, PrefabPath, "TipPanel", "TipPanelBuilder");
        }
    }

    public static class LoginPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/LoginPanel.prefab";

        [MenuItem("Tools/UI/小面板/3. 生成 LoginPanel（登录）", false, 62)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("LoginPanel", canvas.transform, 0f);
            root.GetComponent<Image>().color = UIBuilderKit.Backdrop;

            var panel = root.AddComponent<LoginPanel>();

            // ---- 左侧：标题区 ----
            var title = UIBuilderKit.Text("Title", root.transform, "UNDER WORLD", 76,
                UIBuilderKit.TextMain, TextAlignmentOptions.MidlineLeft);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(760f, 96f), new Vector2(-780f, 60f));

            var sub = UIBuilderKit.Text("Sub", root.transform, "CO-OP ACTION RPG", 20,
                UIBuilderKit.Accent, TextAlignmentOptions.MidlineLeft);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(760f, 28f), new Vector2(-780f, -16f));

            var rule = UIBuilderKit.Line("Rule", root.transform, false, 2f, UIBuilderKit.LineColor);
            UIBuilderKit.Place(rule.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(360f, 2f), new Vector2(-780f, -56f));

            // ---- 右侧：登录卡 ----
            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(470f, 540f));
            UIBuilderKit.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(470f, 540f), new Vector2(520f, 0f));

            var cardTitle = UIBuilderKit.Text("CardTitle", card.transform, "登  录", 30, UIBuilderKit.TextMain);
            UIBuilderKit.Place(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(300f, 44f), new Vector2(40f, -44f));

            var dd = UIBuilderKit.Dropdown("ServerDropdown", card.transform);
            UIBuilderKit.Place((RectTransform)dd.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 54f), new Vector2(40f, -116f));

            var acc = UIBuilderKit.LegacyInput("AccountInput", card.transform, "账号");
            UIBuilderKit.Place((RectTransform)acc.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 56f), new Vector2(40f, -204f));

            var pwd = UIBuilderKit.LegacyInput("PasswordInput", card.transform, "密码");
            UIBuilderKit.Place((RectTransform)pwd.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 56f), new Vector2(40f, -292f));
            pwd.contentType = InputField.ContentType.Password;

            var state = UIBuilderKit.LegacyText("StateText", card.transform, "服务器连接中…", 20,
                UIBuilderKit.TextDim);
            UIBuilderKit.Place(state.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 28f), new Vector2(40f, -372f));

            var login = UIBuilderKit.Button("LoginBtn", card.transform, "进入游戏", true);
            UIBuilderKit.Place((RectTransform)login.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 58f), new Vector2(40f, -424f));

            var reg = UIBuilderKit.Button("RegisterBtn", card.transform, "注册账号");
            UIBuilderKit.Place((RectTransform)reg.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 52f), new Vector2(40f, -496f));

            panel.stateText = state;
            panel.dropdown = dd;
            panel.loginBtn = login;
            panel.registerBtn = reg;
            //panel.account = acc.textComponent;
            //panel.password = pwd.textComponent;

            UIBuilderKit.Save(root, PrefabPath, "LoginPanel", "LoginPanelBuilder");
        }
    }

    public static class RegisterPanelBuilder
    {
        private const string PrefabPath = "Assets/Res/UI/UIPanel/RegisterPanel.prefab";

        [MenuItem("Tools/UI/小面板/4. 生成 RegisterPanel（注册）", false, 63)]
        public static void Build()
        {
            var canvas = UIBuilderKit.TempCanvas("SmallPanelTempCanvas");
            var root = UIBuilderKit.PanelRoot("RegisterPanel", canvas.transform, 0f);
            root.GetComponent<Image>().color = UIBuilderKit.Backdrop;

            var panel = root.AddComponent<RegisterPanel>();

            var title = UIBuilderKit.Text("Title", root.transform, "创建账号", 64,
                UIBuilderKit.TextMain, TextAlignmentOptions.MidlineLeft);
            UIBuilderKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(700f, 84f), new Vector2(-760f, 40f));

            var sub = UIBuilderKit.Text("Sub", root.transform, "REGISTER", 20,
                UIBuilderKit.Accent, TextAlignmentOptions.MidlineLeft);
            UIBuilderKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(700f, 28f), new Vector2(-760f, -26f));

            var card = UIBuilderKit.Card("Panel", root.transform, new Vector2(470f, 520f));
            UIBuilderKit.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(470f, 520f), new Vector2(520f, 0f));

            var cardTitle = UIBuilderKit.Text("CardTitle", card.transform, "注册信息", 28, UIBuilderKit.TextMain);
            UIBuilderKit.Place(cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(300f, 40f), new Vector2(40f, -40f));

            var acc = UIBuilderKit.LegacyInput("AccountInput", card.transform, "账号");
            UIBuilderKit.Place((RectTransform)acc.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 56f), new Vector2(40f, -120f));

            var pwd = UIBuilderKit.LegacyInput("PasswordInput", card.transform, "密码");
            UIBuilderKit.Place((RectTransform)pwd.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 56f), new Vector2(40f, -208f));
            pwd.contentType = InputField.ContentType.Password;

            var check = UIBuilderKit.LegacyInput("ConfirmInput", card.transform, "确认密码");
            UIBuilderKit.Place((RectTransform)check.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 56f), new Vector2(40f, -296f));
            check.contentType = InputField.ContentType.Password;

            var reg = UIBuilderKit.Button("RegisterBtn", card.transform, "注册", true);
            UIBuilderKit.Place((RectTransform)reg.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 58f), new Vector2(40f, -382f));

            var back = UIBuilderKit.Button("LoginBtn", card.transform, "返回登录");
            UIBuilderKit.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(390f, 52f), new Vector2(40f, -452f));

            //panel.account = acc.textComponent;
            //panel.password = pwd.textComponent;
            //panel.pwsCheck = check.textComponent;
            panel.registerBtn = reg;
            panel.loginBtn = back;

            UIBuilderKit.Save(root, PrefabPath, "RegisterPanel", "RegisterPanelBuilder");
        }
    }
}
