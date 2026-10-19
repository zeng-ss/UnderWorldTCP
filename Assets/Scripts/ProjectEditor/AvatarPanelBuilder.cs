using System.Collections.Generic;
using HotUpdate.Data;
using HotUpdate.UI.UIItem;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 一键生成「头像面板」AvatarPanel 的预制体，并注册到 Addressables。
    // 菜单：Tools/UI/头像面板/1. 生成 AvatarPanel 预制体（含注册）
    //
    // 摆的东西（参考分辨率 1920x1080）：
    //   Panel/Header/Title  标题「选择头像」
    //   Panel/Grid/Slot0..N 头像格子（Selected 选中框 + Mask 圆形裁切 + Head 头像图）
    //   Panel/SaveBtn       保存
    //   CloseBtn            关闭（必须是根节点直接子物体，BasePanel 才会自动接线）
    //
    // 格子数量 = AvatarConfig.Count，位置按「每行 5 个」自动排；反复执行幂等。
    public static class AvatarPanelBuilder
    {
        private const string PanelDir = "Assets/Res/UI/UIPanel";
        private const string PrefabPath = PanelDir + "/AvatarPanel.prefab";
        private const string Address = "AvatarPanel"; // 必须与 [PanelPath("AvatarPanel")] 一致
        private const int UiLayer = 5;

        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpritePanelBg = TexDir + "info_box.png";
        private const string SpriteHeader = TexDir + "dialog_header_background.png";
        private const string SpriteButton = TexDir + "rect_button.png";
        private const string SpriteFrame = TexDir + "round_frame.png";
        private const string SpriteCircleMask = TexDir + "circle_mask.png";
        private const string SpriteClose = TexDir + "icon_close_light.png";

        // 版式
        private const float PanelWidth = 1000f;
        private const float PanelHeight = 640f;
        private const float HeaderHeight = 80f;
        private const float SlotSize = 150f;
        private const float SlotGap = 24f;
        private const int ColumnCount = 5;

        private static readonly Color ColDim = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color ColTitle = new Color(1f, 0.93f, 0.76f, 1f);
        private static readonly Color ColHint = new Color(0.66f, 0.69f, 0.76f, 1f);
        private static readonly Color ColSelected = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color ColHover = new Color(1f, 0.85f, 0.35f, 0.18f);

        private static TMP_FontAsset _font;
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly List<AvatarSlot> _slots = new();
        private static Button _saveBtn;

        [MenuItem("Tools/UI/头像面板/1. 生成 AvatarPanel 预制体（含注册）", false, 70)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(PanelDir)) AssetDatabase.CreateFolder("Assets/Res/UI", "UIPanel");

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            _spriteCache.Clear();
            _slots.Clear();

            GameObject root = BuildHierarchy();
            BindRefs(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool registered = RegisterAddressable();

            Debug.Log($"[AvatarPanelBuilder] 已生成 {PrefabPath}（{AvatarConfig.Count} 个头像格子），" +
                      (registered ? $"并注册到 Addressables（地址 {Address}）。" : "但没能注册到 Addressables，请手动注册。") +
                      "\n想换预设头像：改 HotUpdate/Data/AvatarConfig.cs 后在「工具」里加一种贴图，再重跑本菜单。");

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // =====================================================================
        // 搭层级
        // =====================================================================

        private static GameObject BuildHierarchy()
        {
            var root = new GameObject("AvatarPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(AvatarPanel));
            root.layer = UiLayer;
            Stretch(root.GetComponent<RectTransform>());

            // 全屏暗化遮罩（挡射线：面板打开时点不到后面的开始面板）
            Image dim = CreateImage("Dim", root.transform, ColDim);
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            GameObject panel = NewUI("Panel", root.transform);
            Center((RectTransform)panel.transform, new Vector2(PanelWidth, PanelHeight));
            ApplySprite(panel.AddComponent<Image>(), SpritePanelBg, true);

            BuildHeader(panel.transform);
            BuildGrid(panel.transform);

            TMP_Text hint = NewText("Hint", panel.transform, "第 1 个为默认头像，选中后点保存生效",
                20f, TextAlignmentOptions.Center, ColHint);
            PlaceCenter(hint.rectTransform, new Vector2(0f, -180f), new Vector2(700f, 30f));

            Button save = NewButton("SaveBtn", panel.transform, "保存");
            PlaceCenter((RectTransform)save.transform, new Vector2(0f, -250f), new Vector2(260f, 70f));
            _saveBtn = save;

            BuildCloseButton(root.transform);

            return root;
        }

        // 面板 / 格子的引用一次性写好，运行时不再按名字找
        private static void BindRefs(GameObject root)
        {
            var so = new SerializedObject(root.GetComponent<AvatarPanel>());
            so.FindProperty("saveBtn").objectReferenceValue = _saveBtn;

            SerializedProperty slots = so.FindProperty("slots");
            slots.arraySize = _slots.Count;
            for (int i = 0; i < _slots.Count; i++)
                slots.GetArrayElementAtIndex(i).objectReferenceValue = _slots[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHeader(Transform parent)
        {
            GameObject header = NewUI("Header", parent);
            var rect = (RectTransform)header.transform;
            Anchor(rect, new Vector2(0f, 1f), new Vector2(1f, 1f));
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, HeaderHeight);
            rect.anchoredPosition = Vector2.zero;
            ApplySprite(header.AddComponent<Image>(), SpriteHeader, false);

            TMP_Text title = NewText("Title", header.transform, "选择头像", 34f, TextAlignmentOptions.Center, ColTitle);
            Stretch(title.rectTransform);
        }

        private static void BuildGrid(Transform parent)
        {
            GameObject grid = NewUI("Grid", parent);
            var gridRect = (RectTransform)grid.transform;
            int rows = Mathf.CeilToInt(AvatarConfig.Count / (float)ColumnCount);
            Center(gridRect, new Vector2(ColumnCount * SlotSize + (ColumnCount - 1) * SlotGap,
                rows * SlotSize + (rows - 1) * SlotGap));
            gridRect.anchoredPosition = new Vector2(0f, 20f);

            for (int i = 0; i < AvatarConfig.Count; i++)
            {
                int col = i % ColumnCount;
                int row = i / ColumnCount;
                float x = (col - (ColumnCount - 1) * 0.5f) * (SlotSize + SlotGap);
                float y = ((rows - 1) * 0.5f - row) * (SlotSize + SlotGap);
                BuildSlot(grid.transform, i, new Vector2(x, y));
            }
        }

        // 一个头像格子：Hover（悬停底色）→ Selected（金色外框，默认隐藏）→ Mask（圆形裁切）→ Head（头像图）
        private static void BuildSlot(Transform parent, int index, Vector2 position)
        {
            GameObject slot = NewUI($"Slot{index}", parent);
            var rect = (RectTransform)slot.transform;
            rect.sizeDelta = new Vector2(SlotSize, SlotSize);
            rect.anchoredPosition = position;

            var frame = slot.AddComponent<Image>();
            ApplySprite(frame, SpriteFrame, true);

            var button = slot.AddComponent<Button>();
            button.targetGraphic = frame;

            Image hover = CreateImage("Hover", slot.transform, ColHover);
            Stretch(hover.rectTransform);
            ApplySprite(hover, SpriteFrame, true);
            hover.raycastTarget = false;
            hover.enabled = false;

            Image selected = CreateImage("Selected", slot.transform, ColSelected);
            Stretch(selected.rectTransform, -4f, -4f, -4f, -4f);
            ApplySprite(selected, SpriteFrame, true);
            selected.enabled = false;

            GameObject mask = NewUI("Mask", slot.transform);
            var maskRect = (RectTransform)mask.transform;
            Center(maskRect, new Vector2(SlotSize - 26f, SlotSize - 26f));
            var maskImage = mask.AddComponent<Image>();
            ApplySprite(maskImage, SpriteCircleMask, false);
            maskImage.raycastTarget = false;
            var maskComp = mask.AddComponent<Mask>();
            maskComp.showMaskGraphic = false;

            Image head = CreateImage("Head", mask.transform, Color.white);
            Stretch(head.rectTransform);
            head.raycastTarget = false;
            head.sprite = AvatarConfig.Get(index);

            var slotComp = slot.AddComponent<AvatarSlot>();
            var so = new SerializedObject(slotComp);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("head").objectReferenceValue = head;
            so.FindProperty("selected").objectReferenceValue = selected;
            so.FindProperty("hoverBg").objectReferenceValue = hover;
            so.ApplyModifiedPropertiesWithoutUndo();
            _slots.Add(slotComp);
        }

        // 关闭按钮：挂根节点下（BasePanel 只会找 transform.Find("CloseBtn")）
        private static void BuildCloseButton(Transform root)
        {
            GameObject close = NewUI("CloseBtn", root);
            var rect = (RectTransform)close.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(46f, 46f);
            rect.anchoredPosition = new Vector2(PanelWidth * 0.5f - 28f, PanelHeight * 0.5f - 28f);

            var image = close.AddComponent<Image>();
            ApplySprite(image, SpriteClose, false);
            image.preserveAspect = true;

            var button = close.AddComponent<Button>();
            button.targetGraphic = image;
        }

        // =====================================================================
        // 控件工厂
        // =====================================================================

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = NewUI(name, parent);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, float size,
            TextAlignmentOptions align, Color color)
        {
            GameObject go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, string label)
        {
            GameObject go = NewUI(name, parent);
            var image = go.AddComponent<Image>();
            ApplySprite(image, SpriteButton, true);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = NewText("Label", go.transform, label, 26f, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            Center(rect, size);
            rect.anchoredPosition = position;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f,
            float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static void ApplySprite(Image image, string path, bool sliced)
        {
            Sprite sprite = LoadSprite(path);
            if (sprite == null) return;

            image.sprite = sprite;
            if (sliced) image.type = Image.Type.Sliced;
        }

        private static Sprite LoadSprite(string path)
        {
            if (_spriteCache.TryGetValue(path, out Sprite cached)) return cached;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            _spriteCache[path] = sprite;
            return sprite;
        }

        // =====================================================================
        // Addressables
        // =====================================================================

        private static bool RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[AvatarPanelBuilder] 工程没有 Addressables 设置，跳过注册。");
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
            if (string.IsNullOrEmpty(guid)) return false;

            AddressableAssetGroup group = settings.FindGroup("UIPanel") ?? settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = Address;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }
    }
}
