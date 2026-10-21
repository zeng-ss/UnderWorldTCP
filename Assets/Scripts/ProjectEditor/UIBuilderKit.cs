using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    /// <summary>
    /// 纯色 UI 搭建工具：所有小面板的 Builder 共用。
    ///
    /// 约定（和 StartPanel / FriendPanel 的视觉一致）：
    ///   · 面板底 #0E0E13、卡片 #12121A、按钮 #17171D、强调色 #D9FF3D
    ///   · 不用任何九宫图，全部由 Image 纯色块 + 1px/2px 线条 + 文字拼
    ///   · 引用在生成时直接写进面板的序列化字段，运行时不再按名字找
    /// </summary>
    public static class UIBuilderKit
    {
        public const string TmpFontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";
        public const string LegacyFontPath = "Assets/Resources/Animator&Material/AlimamaDongFangDaKai-Regular.ttf";

        public const int UiLayer = 5;

        public static readonly Color Backdrop = new Color(0.04f, 0.04f, 0.05f, 1f);
        public static readonly Color Panel = new Color(0.055f, 0.055f, 0.07f, 1f);
        public static readonly Color CardColor = new Color(0.075f, 0.075f, 0.095f, 1f);
        public static readonly Color ButtonBg = new Color(0.09f, 0.09f, 0.115f, 1f);
        public static readonly Color Accent = new Color(0.85f, 1f, 0.24f, 1f);
        public static readonly Color AccentHover = new Color(0.93f, 1f, 0.45f, 1f); // 主按钮悬停再亮一档
        public static readonly Color OnAccent = new Color(0.05f, 0.05f, 0.06f, 1f);  // 压在荧光色上的字
        public static readonly Color TextMain = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color TextDim = new Color(0.42f, 0.43f, 0.5f, 1f);
        public static readonly Color LineColor = new Color(1f, 1f, 1f, 0.08f);

        private static TMP_FontAsset _tmpFont;
        private static Font _legacyFont;

        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null) _tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
                return _tmpFont;
            }
        }

        public static Font LegacyFont
        {
            get
            {
                if (_legacyFont == null) _legacyFont = AssetDatabase.LoadAssetAtPath<Font>(LegacyFontPath);
                if (_legacyFont == null) _legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _legacyFont;
            }
        }

        // =====================================================================
        // 基础控件
        // =====================================================================

        public static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image Rect(string name, Transform parent, Color color)
        {
            var img = NewUI(name, parent).AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static TMP_Text Text(string name, Transform parent, string content, float size,
            Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool wrap = false)
        {
            var tmp = NewUI(name, parent).AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            if (TmpFont != null) tmp.font = TmpFont;
            return tmp;
        }

        /// <summary>内置 UISprite：Image 在没有 sprite 时 Filled 类型不生效，进度条必须给一张</summary>
        public static Sprite BuiltinSprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        /// <summary>进度条：返回填充图（type = Filled，从左往右）</summary>
        public static Image Bar(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pos,
            Color bgColor, Color fillColor)
        {
            var bg = Rect(name + "Bg", parent, bgColor);
            Place(bg.rectTransform, anchor, new Vector2(0.5f, 0.5f), size, pos);

            var fill = Rect(name, bg.transform, fillColor);
            Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            fill.sprite = BuiltinSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            return fill;
        }

        // 纯色按钮：深色底 + 左侧 4px 强调条 + 居中文字
        public static Button Button(string name, Transform parent, string label, bool primary = false)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = primary ? Accent : ButtonBg;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // ColorTint 是乘法，黑底乘不出荧光黄 → 悬停变色交给 ButtonHoverTint
            btn.transition = Selectable.Transition.None;

            var tint = go.AddComponent<HotUpdate.UI.UIItem.ButtonHoverTint>();
            tint.SetColors(primary ? AccentHover : Accent, OnAccent, 0.1f);

            if (!primary)
            {
                var bar = NewUI("Bar", go.transform);
                var rt = (RectTransform)bar.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(4f, 0f);
                rt.anchoredPosition = Vector2.zero;
                var barImg = bar.AddComponent<Image>();
                barImg.color = Accent;
                barImg.raycastTarget = false;
            }

            var txt = Text("Label", go.transform, label, 22,
                primary ? new Color(0.05f, 0.05f, 0.06f, 1f) : TextMain, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, 8f, 8f, 0f, 0f);
            return btn;
        }

        // UGUI 文本（LoginPanel / RegisterPanel 的字段是 UnityEngine.UI.Text）
        public static Text LegacyText(string name, Transform parent, string content, int size,
            Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var txt = NewUI(name, parent).AddComponent<Text>();
            txt.text = content;
            txt.font = LegacyFont;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            txt.supportRichText = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        // UGUI 输入框（老面板的字段是 UnityEngine.UI.Text，这里必须用旧版）
        public static InputField LegacyInput(string name, Transform parent, string placeholder)
        {
            var go = NewUI(name, parent);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.045f);

            var input = go.AddComponent<InputField>();
            input.targetGraphic = bg;

            var textGo = NewUI("Text", go.transform);
            var text = textGo.AddComponent<Text>();
            text.font = LegacyFont;
            text.fontSize = 24;
            text.color = TextMain;
            text.supportRichText = false;
            text.alignment = TextAnchor.MiddleLeft;
            Stretch(text.rectTransform, 14f, 14f, 4f, 6f);

            var phGo = NewUI("Placeholder", go.transform);
            var ph = phGo.AddComponent<Text>();
            ph.font = LegacyFont;
            ph.fontSize = 24;
            ph.color = TextDim;
            ph.text = placeholder;
            ph.alignment = TextAnchor.MiddleLeft;
            Stretch(ph.rectTransform, 14f, 14f, 4f, 6f);

            input.textComponent = text;
            input.placeholder = ph;

            AddUnderline(go.transform);
            return input;
        }

        // 纯色 TMP 输入框
        public static TMP_InputField TmpInput(string name, Transform parent, string placeholder)
        {
            var go = NewUI(name, parent);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.045f);

            var input = go.AddComponent<TMP_InputField>();
            input.image = bg;
            input.targetGraphic = bg;

            var area = NewUI("Text Area", go.transform);
            Stretch((RectTransform)area.transform, 14f, 14f, 4f, 6f);

            var text = Text("Text", area.transform, "", 24, TextMain);
            Stretch(text.rectTransform);

            var ph = Text("Placeholder", area.transform, placeholder, 24, TextDim);
            Stretch(ph.rectTransform);

            input.textViewport = (RectTransform)area.transform;
            input.textComponent = text;
            input.placeholder = ph;

            AddUnderline(go.transform);
            return input;
        }

        private static void AddUnderline(Transform parent)
        {
            var line = NewUI("Underline", parent);
            var rt = (RectTransform)line.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 2f);
            rt.anchoredPosition = Vector2.zero;
            var img = line.AddComponent<Image>();
            img.color = new Color(Accent.r, Accent.g, Accent.b, 0.45f);
            img.raycastTarget = false;
        }

        // UGUI 下拉框（LoginPanel 的字段是 Dropdown）
        public static Dropdown Dropdown(string name, Transform parent)
        {
            var go = NewUI(name, parent);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.045f);

            var caption = NewUI("Label", go.transform).AddComponent<Text>();
            caption.font = LegacyFont;
            caption.fontSize = 24;
            caption.color = TextMain;
            caption.alignment = TextAnchor.MiddleLeft;
            Stretch(caption.rectTransform, 14f, 34f, 4f, 6f);

            var arrow = Rect("Arrow", go.transform, Accent);
            var art = arrow.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(1f, 0.5f);
            art.pivot = new Vector2(1f, 0.5f);
            art.sizeDelta = new Vector2(12f, 12f);
            art.anchoredPosition = new Vector2(-14f, 0f);

            // Template：Content + Item(Toggle + Label)，Dropdown 运行时自己实例化
            var template = NewUI("Template", go.transform);
            var trt = (RectTransform)template.transform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0f, 220f);
            trt.anchoredPosition = new Vector2(0f, 2f);
            template.AddComponent<Image>().color = CardColor;

            var viewport = NewUI("Viewport", template.transform);
            Stretch((RectTransform)viewport.transform, 2f, 2f, 2f, 2f);
            viewport.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport.transform);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 40f);

            var item = NewUI("Item", content.transform);
            var irt = (RectTransform)item.transform;
            irt.anchorMin = new Vector2(0f, 1f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(0f, 40f);

            var itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(1f, 1f, 1f, 0.04f);

            var toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemBg;

            var itemLabel = NewUI("Item Label", item.transform).AddComponent<Text>();
            itemLabel.font = LegacyFont;
            itemLabel.fontSize = 22;
            itemLabel.color = TextMain;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            Stretch(itemLabel.rectTransform, 14f, 12f, 0f, 0f);

            var scroll = template.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = crt;

            template.SetActive(false);

            var dd = go.AddComponent<Dropdown>();
            dd.targetGraphic = bg;
            dd.captionText = caption;
            dd.template = trt;
            dd.itemText = itemLabel;
            return dd;
        }

        // =====================================================================
        // 布局
        // =====================================================================

        public static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        public static void Center(RectTransform rt, Vector2 size, Vector2 pos = default)
        {
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, pos);
        }

        public static Image Line(string name, Transform parent, bool vertical, float thickness, Color color)
        {
            var img = Rect(name, parent, color);
            var rt = img.rectTransform;
            if (vertical)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(thickness, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(0f, thickness);
            }

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            return img;
        }

        // =====================================================================
        // 面板根
        // =====================================================================

        /// <summary>全屏深色底 + 可选遮罩（弹出式面板用 0.88 的黑）</summary>
        public static GameObject PanelRoot(string name, Transform parent, float dim = 0.88f)
        {
            var root = NewUI(name, parent);
            Stretch((RectTransform)root.transform);
            var bg = root.AddComponent<Image>();
            bg.color = dim > 0f ? new Color(0f, 0f, 0f, dim) : Backdrop;
            bg.raycastTarget = true;
            return root;
        }

        /// <summary>居中的卡片：深色底 + 顶部一条 2px 强调线</summary>
        public static GameObject Card(string name, Transform parent, Vector2 size)
        {
            var go = NewUI(name, parent);
            var rt = (RectTransform)go.transform;
            Center(rt, size);

            var img = go.AddComponent<Image>();
            img.color = CardColor;
            img.raycastTarget = true;

            var accent = Rect("AccentLine", go.transform, Accent);
            var art = accent.rectTransform;
            art.anchorMin = new Vector2(0f, 1f);
            art.anchorMax = new Vector2(1f, 1f);
            art.pivot = new Vector2(0.5f, 1f);
            art.sizeDelta = new Vector2(0f, 2f);
            art.anchoredPosition = Vector2.zero;

            return go;
        }

        /// <summary>关闭按钮：右上角半透明方块 + ×</summary>
        public static Button CloseButton(Transform parent, string name = "CloseBtn")
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.07f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txt = Text("Icon", go.transform, "×", 26, TextMain, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform);

            Place((RectTransform)go.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(44f, 44f), new Vector2(-20f, -20f));
            return btn;
        }

        // =====================================================================
        // 输出
        // =====================================================================

        /// <summary>
        /// 生成后是否把实例留在场景里做预览。单个菜单 true（方便肉眼验收），
        /// 批量生成时由「0. 全部重新生成」临时设为 false，避免七个面板叠在一起。
        /// </summary>
        public static bool KeepPreview = true;

        /// <summary>保存预制体并注册到 Addressables（地址 = 面板名）</summary>
        public static void Save(GameObject root, string prefabPath, string address, string menuHint,
            string groupName = "UIPanel")
        {
            string dir = System.IO.Path.GetDirectoryName(prefabPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string[] parts = dir.Split('/');
                string cur = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = cur + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterAddressable(prefabPath, address, groupName);

            if (!KeepPreview)
            {
                Object.DestroyImmediate(root);
                Debug.Log($"[{menuHint}] 已生成 {prefabPath}（地址 {address}）。");
                return;
            }

            // 保留场景实例供预览：重跑同一面板时先清掉上一次的（不含本次这颗）
            ClearSiblings(root);
            root.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log($"[{menuHint}] 已生成 {prefabPath}（地址 {address}），场景里保留了预览实例（不会存进场景文件）。");
        }

        // 删掉同父级下上一次生成、同名的预览实例
        private static void ClearSiblings(GameObject root)
        {
            Transform parent = root.transform.parent;
            if (parent == null) return;

            var olds = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != root.transform && child.name == root.name) olds.Add(child.gameObject);
            }

            foreach (GameObject old in olds) Object.DestroyImmediate(old);
        }

        private static void RegisterAddressable(string prefabPath, string address, string groupName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            if (string.IsNullOrEmpty(guid)) return;

            AddressableAssetGroup group = settings.FindGroup(groupName) ?? settings.FindGroup("UIPanel") ?? settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>生成用的临时 Canvas（1920×1080 参考分辨率，不参与场景保存）</summary>
        public static GameObject TempCanvas(string canvasName)
        {
            var existing = GameObject.Find(canvasName);
            if (existing != null)
            {
                existing.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                return existing;
            }

            var go = new GameObject(canvasName, typeof(RectTransform));
            go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            go.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }
            return go;
        }
    }
}
