using HotUpdate.UI.UIItem;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // =====================================================================
    // 列表项 / 聊天气泡 / 快捷格的纯色重建
    // 菜单：Tools/UI/列表项
    // 不动的三个：RedDot（纯圆点）、DialogueOptionItem（对话框选项）、DepotItem（驱动盘格子）
    // =====================================================================

    public static class UIItemBuilder
    {
        private const string Dir = "Assets/Res/UI/UIItem/";
        private const string Group = "UIItem";

        // 行底：极淡一层白，靠 hover 提亮区分
        private static readonly Color RowBg = new Color(1f, 1f, 1f, 0.035f);
        private static readonly Color RowHover = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color RowLine = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color Chip = new Color(1f, 1f, 1f, 0.06f);

        private static readonly Color Main = UIBuilderKit.TextMain;
        private static readonly Color Dim = UIBuilderKit.TextDim;
        private static readonly Color Accent = UIBuilderKit.Accent;
        private static readonly Color OnAccent = UIBuilderKit.OnAccent;

        [MenuItem("Tools/UI/列表项/1. 全部重建（气泡 / 快捷格 / 任务 / 房间 / 房间玩家）", false, 70)]
        public static void BuildAll()
        {
            UIBuilderKit.KeepPreview = false;
            try
            {
                BuildChatBubbles();
                BuildHudSlot();
                BuildTaskItem();
                BuildRoomItem();
                BuildRoomPlayerItem();
            }
            finally
            {
                UIBuilderKit.KeepPreview = true;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[UIItemBuilder] 列表项已全部重建：ChatRootLeft / ChatRootRight / HUDSlotItem / TaskItem / RoomListItem / RoomPlayerItem");
            }
        }

        // ---------------------------------------------------------------
        // 聊天气泡（左：对方 / 右：自己）
        // 高度靠 HorizontalLayoutGroup + 多行文本自动撑开，父级 MsgContent 的
        // VerticalLayoutGroup(childControlHeight) 直接读它的 preferredHeight
        // ---------------------------------------------------------------

        [MenuItem("Tools/UI/列表项/2. 聊天气泡 ChatRootLeft / ChatRootRight", false, 71)]
        public static void BuildChatBubbles()
        {
            var canvas = UIBuilderKit.TempCanvas("ItemTempCanvas");
            GameObject left = CreateBubble(canvas.transform, false);
            GameObject right = CreateBubble(canvas.transform, true);
            UIBuilderKit.Save(left, Dir + "ChatRootLeft.prefab", "ChatRootLeft", "ChatItem", Group);
            UIBuilderKit.Save(right, Dir + "ChatRootRight.prefab", "ChatRootRight", "ChatItem", Group);
        }

        private static GameObject CreateBubble(Transform parent, bool self)
        {
            var root = UIBuilderKit.NewUI(self ? "ChatRootRight" : "ChatRootLeft", parent);
            ((RectTransform)root.transform).sizeDelta = new Vector2(0f, 76f);

            var row = root.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;
            row.spacing = 10f;
            row.padding = new RectOffset(4, 4, 4, 4);

            // 文字块：flexibleWidth = 1，吃掉头像以外的宽度，长句在气泡里换行
            var block = UIBuilderKit.NewUI("nameAndMes", root.transform);
            block.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var column = block.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = self ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            column.spacing = 4f;

            var name = UIBuilderKit.Text("name", block.transform, "名字", 15, Dim,
                self ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            // 自己：荧光色淡淡一层；对方：白 6%
            var bubble = UIBuilderKit.Rect("mesBg", block.transform,
                self ? new Color(0.85f, 1f, 0.24f, 0.14f) : new Color(1f, 1f, 1f, 0.06f));
            var bubbleLayout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(14, 14, 10, 10);
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;

            var mes = UIBuilderKit.Text("Mes", bubble.transform, "消息内容", 21, Main,
                self ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft, true);

            // 头像固定 56：自己的那条排到最后 → 「文字在左、头像在右」
            var head = UIBuilderKit.Rect("Head", root.transform, Color.white);
            head.preserveAspect = true;
            var headLayout = head.gameObject.AddComponent<LayoutElement>();
            headLayout.preferredWidth = 56f;
            headLayout.preferredHeight = 56f;
            if (self) head.transform.SetAsLastSibling();

            var item = root.AddComponent<ChatItem>();
            item.headImage = head;
            item.nameText = name;
            item.mesText = mes;
            return root;
        }

        // ---------------------------------------------------------------
        // HUD 快捷格 84×108
        // ---------------------------------------------------------------

        [MenuItem("Tools/UI/列表项/3. 快捷格 HUDSlotItem", false, 72)]
        public static void BuildHudSlot()
        {
            var canvas = UIBuilderKit.TempCanvas("ItemTempCanvas");
            GameObject root = CreateHudSlot(canvas.transform);
            UIBuilderKit.Save(root, Dir + "HUDSlotItem.prefab", "HUDSlotItem", "ShortcutButton", Group);
        }

        private static GameObject CreateHudSlot(Transform parent)
        {
            var root = UIBuilderKit.NewUI("HUDSlotItem", parent);
            ((RectTransform)root.transform).sizeDelta = new Vector2(84f, 108f);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);
            bg.raycastTarget = true;

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            // 左下角一条荧光底边，和菜单按钮同一套语言
            var rule = UIBuilderKit.Rect("Rule", root.transform, Accent);
            UIBuilderKit.Place(rule.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(84f, 2f), Vector2.zero);

            var key = UIBuilderKit.Text("Key", root.transform, "E", 13, Accent);
            UIBuilderKit.Place(key.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, 18f), new Vector2(8f, -7f));

            var icon = UIBuilderKit.Rect("Icon", root.transform, Color.white);
            icon.preserveAspect = true;
            UIBuilderKit.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(52f, 52f), new Vector2(0f, -32f));

            var label = UIBuilderKit.Text("Name", root.transform, "背包", 13, Dim, TextAlignmentOptions.Center);
            UIBuilderKit.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(78f, 18f), new Vector2(0f, 10f));

            // 红点用 45° 小菱形，避免再依赖圆点贴图
            var dot = UIBuilderKit.Rect("RedDot", root.transform, new Color(1f, 0.34f, 0.34f, 1f));
            UIBuilderKit.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, 10f), new Vector2(-10f, -10f));
            dot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            dot.gameObject.SetActive(false);

            var tint = root.AddComponent<ButtonHoverTint>();
            tint.SetColors(new Color(0.85f, 1f, 0.24f, 0.92f), OnAccent, 0.1f);
            // 图标是 sprite，不能跟着染成纯色
            tint.SetForegrounds(key, label);

            var view = root.AddComponent<ShortcutButton>();
            Bind(view, "button", btn);
            Bind(view, "redDot", dot.gameObject);
            Bind(view, "icon", icon);
            Bind(view, "keyTxt", key);
            Bind(view, "desTxt", label);
            return root;
        }

        // ---------------------------------------------------------------
        // 任务项 812×112
        // ---------------------------------------------------------------

        [MenuItem("Tools/UI/列表项/4. 任务项 TaskItem", false, 73)]
        public static void BuildTaskItem()
        {
            var canvas = UIBuilderKit.TempCanvas("ItemTempCanvas");
            GameObject root = CreateTaskItem(canvas.transform);
            UIBuilderKit.Save(root, Dir + "TaskItem.prefab", "TaskItem", "TaskItem", Group);
        }

        private static GameObject CreateTaskItem(Transform parent)
        {
            var root = UIBuilderKit.NewUI("TaskItem", parent);
            ((RectTransform)root.transform).sizeDelta = new Vector2(812f, 112f);

            var bg = UIBuilderKit.Rect("Bg", root.transform, RowBg);
            UIBuilderKit.Stretch(bg.rectTransform);

            // TaskItem.Awake 里 GetComponent<CanvasGroup>()，入场动画靠它淡入
            root.AddComponent<CanvasGroup>();

            var desc = UIBuilderKit.Text("des", root.transform, "任务描述", 21, Main);
            TopBand(desc.rectTransform, 22f, 168f, 22f, 30f);

            var progress = UIBuilderKit.Text("num", root.transform, "0/1", 18, Accent,
                TextAlignmentOptions.MidlineRight);
            UIBuilderKit.Place(progress.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(140f, 30f), new Vector2(-22f, -22f));

            // 完成态：荧光色小牌子，和进度文字互斥显示
            var finish = UIBuilderKit.Rect("Finish", root.transform, new Color(0.85f, 1f, 0.24f, 0.16f));
            UIBuilderKit.Place(finish.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(96f, 30f), new Vector2(-22f, -22f));
            var finishTxt = UIBuilderKit.Text("FinishText", finish.transform, "已完成", 15, Accent,
                TextAlignmentOptions.Center);
            UIBuilderKit.Stretch(finishTxt.rectTransform);
            finish.gameObject.SetActive(false);

            var barBg = UIBuilderKit.Rect("BarBg", root.transform, new Color(1f, 1f, 1f, 0.08f));
            BottomBand(barBg.rectTransform, 22f, 22f, 26f, 6f);

            var fill = UIBuilderKit.Rect("fill", barBg.transform, Accent);
            UIBuilderKit.Stretch(fill.rectTransform);
            // 没 sprite 的 Image 用 Filled 不生效，必须挂一张
            fill.sprite = UIBuilderKit.BuiltinSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            var view = root.AddComponent<TaskItem>();
            view.imgFill = fill;
            view.imgFinish = finish;
            view.txtDesc = desc;
            view.txtProgress = progress;
            return root;
        }

        // ---------------------------------------------------------------
        // 房间列表项（高度 76，宽度由 LoopList 拉满）
        // ---------------------------------------------------------------

        [MenuItem("Tools/UI/列表项/5. 房间列表项 RoomListItem", false, 74)]
        public static void BuildRoomItem()
        {
            var canvas = UIBuilderKit.TempCanvas("ItemTempCanvas");
            GameObject root = CreateRoomItem(canvas.transform);
            UIBuilderKit.Save(root, Dir + "RoomListItem.prefab", "RoomListItem", "RoomListItem", Group);
        }

        public static GameObject CreateRoomItem(Transform parent)
        {
            var root = UIBuilderKit.NewUI("RoomListItem", parent);
            ((RectTransform)root.transform).sizeDelta = new Vector2(700f, 76f);

            var bg = root.AddComponent<Image>();
            bg.color = RowBg;
            bg.raycastTarget = true;

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            var tint = root.AddComponent<ButtonHoverTint>();
            tint.SetColors(RowHover, Main, 0.1f);

            var accent = UIBuilderKit.Rect("Accent", root.transform, new Color(0.85f, 1f, 0.24f, 0.85f));
            var art = accent.rectTransform;
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(0f, 1f);
            art.pivot = new Vector2(0f, 0.5f);
            art.sizeDelta = new Vector2(3f, -18f);
            art.anchoredPosition = Vector2.zero;

            var name = UIBuilderKit.Text("Name", root.transform, "房间名", 23, Main);
            TopBand(name.rectTransform, 24f, 150f, 12f, 30f);

            var info = UIBuilderKit.Text("Info", root.transform, "房主：玩家   1/4   等待中", 16, Dim);
            BottomBand(info.rectTransform, 24f, 150f, 12f, 24f);

            var line = UIBuilderKit.Rect("Line", root.transform, RowLine);
            BottomBand(line.rectTransform, 24f, 0f, 0f, 1f);

            var view = root.AddComponent<RoomListItem>();
            Bind(view, "nameText", name);
            Bind(view, "infoText", info);
            return root;
        }

        // ---------------------------------------------------------------
        // 房间内玩家项（高度 64）
        // ---------------------------------------------------------------

        [MenuItem("Tools/UI/列表项/6. 房间玩家项 RoomPlayerItem", false, 75)]
        public static void BuildRoomPlayerItem()
        {
            var canvas = UIBuilderKit.TempCanvas("ItemTempCanvas");
            GameObject root = CreateRoomPlayerItem(canvas.transform);
            UIBuilderKit.Save(root, Dir + "RoomPlayerItem.prefab", "RoomPlayerItem", "RoomPlayerItem", Group);
        }

        public static GameObject CreateRoomPlayerItem(Transform parent)
        {
            var root = UIBuilderKit.NewUI("RoomPlayerItem", parent);
            ((RectTransform)root.transform).sizeDelta = new Vector2(420f, 64f);

            var bg = root.AddComponent<Image>();
            bg.color = RowBg;
            bg.raycastTarget = true;

            var btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;

            var tint = root.AddComponent<ButtonHoverTint>();
            tint.SetColors(RowHover, Main, 0.1f);

            var head = UIBuilderKit.Rect("Head", root.transform, Color.white);
            head.preserveAspect = true;
            UIBuilderKit.Place(head.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(44f, 44f), new Vector2(12f, 0f));

            var name = UIBuilderKit.Text("Name", root.transform, "玩家", 19, Main);
            TopBand(name.rectTransform, 68f, 86f, 12f, 24f);

            var state = UIBuilderKit.Text("State", root.transform, "未准备", 14, Dim);
            BottomBand(state.rectTransform, 68f, 86f, 10f, 20f);

            // 踢人提示（房主把鼠标移到别人身上时出现，RoomPlayerItem 自己开关）
            var kick = UIBuilderKit.Rect("KickTip", root.transform, new Color(1f, 0.34f, 0.34f, 0.16f));
            UIBuilderKit.Place(kick.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(56f, 26f), new Vector2(-12f, 0f));
            var kickTxt = UIBuilderKit.Text("KickText", kick.transform, "踢出", 14,
                new Color(1f, 0.45f, 0.45f, 1f), TextAlignmentOptions.Center);
            UIBuilderKit.Stretch(kickTxt.rectTransform);
            kick.gameObject.SetActive(false);

            var view = root.AddComponent<RoomPlayerItem>();
            Bind(view, "headImage", head);
            Bind(view, "nameText", name);
            Bind(view, "stateText", state);
            Bind(view, "kickTip", kick.gameObject);
            Bind(view, "rowButton", btn);
            return root;
        }

        // =====================================================================
        // 工具
        // =====================================================================

        // 贴顶横条：左右留白 + 距顶距离 + 高度
        private static void TopBand(RectTransform rt, float left, float right, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // 贴底横条
        private static void BottomBand(RectTransform rt, float left, float right, float bottom, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
        }

        // 私有 [SerializeField] 字段用 SerializedObject 写，公开字段直接赋值
        private static void Bind(Component target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[UIItemBuilder] {target.GetType().Name} 上找不到字段 {field}");
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
