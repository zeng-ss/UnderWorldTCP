using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using HotUpdate.UI;
using HotUpdate.UI.UIItem;

namespace ProjectEditor
{
    // 一键把「游戏内 HUD」(PlayerProPanel) 的快捷栏与信息底板摆进预制体。
    //
    // 菜单：Tools/UI/游戏内HUD
    //   1. 在预制体里搭好快捷栏   —— 直接改 Assets/Res/UI/UIPanel/PlayerProPanel.prefab（可反复执行）
    //   2. 在当前场景生成一份预览 —— 把上面的预制体实例化到临时 Canvas，看一眼效果
    //
    // 版式：底板 InfoBg（垫在左上角头像/血条后面）+ 底边居中的 ShortcutBar。
    //   ShortcutBar 的格子按 PanelShortcut.All 生成：方块底图 + 图标 + 中文名 + 右下角按键角标；
    //   每格挂 ShortcutButton 并把 panel 字段填好 —— 运行时由它自己接上点击，面板代码里不再生成任何 UI。
    //
    // 约定：
    //   · 视觉全部复用 SharpUI 资源（info_box / square_button_neutral / square_button_selected / rect_button）。
    //   · 节点名 ShortcutBar / InfoBg 在重建时按名字整棵删掉重来，所以别往里面塞手工改动。
    //   · 所有文本统一用项目中文 TMP 字体，否则中文是方块。
    public static class PlayerProPanelBuilder
    {
        // ---------- 目标预制体 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/PlayerProPanel.prefab";
        private const string RootName = "PlayerProPanel";
        private const string HeadName = "Head";

        // ---------- 字体 ----------
        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        // ---------- SharpUI 贴图 ----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpriteBar = TexDir + "info_box.png";
        private const string SpriteTile = TexDir + "square_button_neutral.png";
        private const string SpriteTileSelected = TexDir + "square_button_selected.png";
        private const string SpriteBadge = TexDir + "rect_button.png";

        // ---------- 命名 ----------
        private const string BarName = "ShortcutBar";
        private const string InfoBgName = "InfoBg";
        private const string PreviewCanvasName = "HUDPreviewCanvas";

        // ---------- 版式 ----------
        private const float SlotWidth = 84f;
        private const float SlotHeight = 108f;
        private const float SlotSpacing = 14f;
        private const float TileSize = 84f;
        private const float IconSize = 50f;
        private const float BarPaddingX = 18f;
        private const float BarPaddingY = 12f;
        private const float BarBottomOffset = 28f;

        // 信息底板比头像区（Head）四周各外扩一点
        private const float InfoBgMarginX = 48f;
        private const float InfoBgMarginY = 44f;

        // ---------- 配色 ----------
        private static readonly Color ColBarBg = new Color(0.05f, 0.06f, 0.10f, 0.78f);
        private static readonly Color ColInfoBg = new Color(0.05f, 0.06f, 0.10f, 0.62f);
        private static readonly Color ColName = new Color(0.93f, 0.94f, 0.97f, 1f);
        private static readonly Color ColBadgeBg = new Color(0.08f, 0.09f, 0.13f, 0.95f);
        private static readonly Color ColKey = new Color(0.98f, 0.85f, 0.45f, 1f);

        private static TMP_FontAsset _font;
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly HashSet<string> _fullRectFixed = new();

        // =====================================================================
        // 菜单入口
        // =====================================================================

        [MenuItem("Tools/UI/游戏内HUD/1. 在预制体里搭好快捷栏", false, 40)]
        public static void BuildIntoPrefab()
        {
            // 九宫拉伸要求 Sprite 的 mesh 是 FullRect，先改导入设置再动预制体，避免中途刷库
            EnsureFullRect(SpriteBar);
            EnsureFullRect(SpriteTile);
            EnsureFullRect(SpriteTileSelected);
            EnsureFullRect(SpriteBadge);

            GameObject root = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("生成失败", $"没找到预制体：{PanelPrefabPath}", "好");
                return;
            }

            try
            {
                Build(root);
                PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlayerProPanelBuilder] 快捷栏与信息底板已写入 {PanelPrefabPath}。\n" +
                      "面板地址没变（仍是 PlayerProPanel），Addressables 里已有的注册项继续有效，不必重新注册。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath));
        }

        [MenuItem("Tools/UI/游戏内HUD/2. 在当前场景生成一份预览", false, 41)]
        public static void PreviewInScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("预览失败", $"没找到预制体：{PanelPrefabPath}", "好");
                return;
            }

            Transform canvas = EnsureCanvas();
            var inst = PrefabUtility.InstantiatePrefab(prefab, canvas) as GameObject;
            if (inst == null) return;

            Selection.activeGameObject = inst;
            EditorGUIUtility.PingObject(inst);
            Debug.Log("[PlayerProPanelBuilder] 已把 HUD 实例化到场景里预览（不需要时直接删掉这个实例）。");
        }

        // =====================================================================
        // 构建层级
        // =====================================================================

        private static void Build(GameObject root)
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null)
                Debug.LogWarning($"[PlayerProPanelBuilder] 未找到字体资源：{FontPath}，文本将回退默认字体（中文可能显示为方块）。");

            BuildInfoBackdrop(root);
            BuildShortcutBar(root);
        }

        // 头像 / 血条那块直接怼在 3D 画面上看不清，给它垫一层半透明底板
        private static void BuildInfoBackdrop(GameObject root)
        {
            DestroyChild(root.transform, InfoBgName);

            var head = root.transform.Find(HeadName) as RectTransform;
            if (head == null)
            {
                Debug.LogWarning($"[PlayerProPanelBuilder] 没找到节点 {RootName}/{HeadName}，跳过信息底板。");
                return;
            }

            var go = NewUI(InfoBgName, root.transform);
            var rt = (RectTransform)go.transform;
            rt.SetAsFirstSibling(); // 垫到最底下，别盖住头像与血条

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = head.sizeDelta + new Vector2(InfoBgMarginX, InfoBgMarginY);
            rt.anchoredPosition = head.anchoredPosition + new Vector2(-6f, 0f);

            var img = go.AddComponent<Image>();
            ApplySprite(img, SpriteBar, true);
            img.color = ColInfoBg;
            img.raycastTarget = false; // 空档不挡点击
        }

        private static void BuildShortcutBar(GameObject root)
        {
            DestroyChild(root.transform, BarName);

            int count = PanelShortcut.All.Length;
            float width = count * SlotWidth + Mathf.Max(0, count - 1) * SlotSpacing + BarPaddingX * 2f;
            float height = SlotHeight + BarPaddingY * 2f;

            var go = NewUI(BarName, root.transform);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, BarBottomOffset);

            var bg = go.AddComponent<Image>();
            ApplySprite(bg, SpriteBar, true);
            bg.color = ColBarBg;
            bg.raycastTarget = false;

            for (int i = 0; i < count; i++)
            {
                float x = BarPaddingX + i * (SlotWidth + SlotSpacing);
                CreateSlot(rt, PanelShortcut.All[i], x);
            }
        }

        private static void CreateSlot(Transform bar, PanelShortcut shortcut, float x)
        {
            var slot = NewUI("Slot_" + shortcut.Label, bar);
            var slotRt = (RectTransform)slot.transform;
            slotRt.anchorMin = slotRt.anchorMax = new Vector2(0f, 0.5f);
            slotRt.pivot = new Vector2(0f, 0.5f);
            slotRt.sizeDelta = new Vector2(SlotWidth, SlotHeight);
            slotRt.anchoredPosition = new Vector2(x, 0f);

            // 方块底图（同时也是这一格的点击区）
            var tile = NewUI("Tile", slotRt);
            var tileRt = (RectTransform)tile.transform;
            tileRt.anchorMin = tileRt.anchorMax = new Vector2(0.5f, 1f);
            tileRt.pivot = new Vector2(0.5f, 1f);
            tileRt.sizeDelta = new Vector2(TileSize, TileSize);
            tileRt.anchoredPosition = Vector2.zero;

            var tileImg = tile.AddComponent<Image>();
            ApplySprite(tileImg, SpriteTile, true);

            var btn = tile.AddComponent<Button>();
            SetupSpriteSwap(btn, tileImg);

            // 图标（方块正中偏上，给右下角角标腾位置）
            var icon = NewUI("Icon", tileRt);
            var iconRt = (RectTransform)icon.transform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(IconSize, IconSize);
            iconRt.anchoredPosition = new Vector2(0f, 4f);
            var iconImg = icon.AddComponent<Image>();
            ApplySprite(iconImg, IconPath(shortcut.IconName), false);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // 右下角按键角标（骑在方块右下的角上）
            var badge = NewUI("KeyBadge", tileRt);
            var badgeRt = (RectTransform)badge.transform;
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.sizeDelta = new Vector2(26f, 20f);
            badgeRt.anchoredPosition = new Vector2(0f, 4f);
            var badgeImg = badge.AddComponent<Image>();
            ApplySprite(badgeImg, SpriteBadge, true);
            badgeImg.color = ColBadgeBg;
            badgeImg.raycastTarget = false;

            TMP_Text key = NewText("Key", badgeRt, shortcut.KeyLabel, 15f, TextAlignmentOptions.Center, ColKey);
            
            Stretch(key.rectTransform);
            key.fontStyle = FontStyles.Bold;

            // 中文名（方块正下方）
            TMP_Text name = NewText("Name", slotRt, shortcut.Label, 16f, TextAlignmentOptions.Center, ColName);
            var nameRt = name.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.sizeDelta = new Vector2(SlotWidth, 22f);
            nameRt.anchoredPosition = Vector2.zero;

            // 悬停放大挂在这一格上：鼠标压在方块上时事件会冒泡到这一层
            var view = slot.AddComponent<ShortcutButton>();
            view.panel = shortcut.Id;
            view.button = btn;
        }

        // =====================================================================
        // 控件工厂
        // =====================================================================

        private static string IconPath(string iconName) => TexDir + iconName + ".png";

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, float size,
            TextAlignmentOptions align, Color color)
        {
            var go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        private static void SetupSpriteSwap(Button btn, Image target)
        {
            Sprite selected = LoadSprite(SpriteTileSelected);
            if (selected != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                var state = btn.spriteState;
                state.highlightedSprite = selected;
                state.pressedSprite = selected;
                state.selectedSprite = selected;
                btn.spriteState = state;
            }

            btn.targetGraphic = target;
        }

        // =====================================================================
        // 工具
        // =====================================================================

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform old = parent.Find(childName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------- Sprite ----------

        private static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_spriteCache.TryGetValue(path, out var cached)) return cached;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[PlayerProPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{path}");

            _spriteCache[path] = sprite;
            return sprite;
        }

        private static void ApplySprite(Image img, string spritePath, bool sliced)
        {
            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

        // 九宫渲染要求 Sprite 的 mesh 为 FullRect（SharpUI 贴图默认 Tight，九宫会失效）
        private static void EnsureFullRect(string path)
        {
            if (!_fullRectFixed.Add(path)) return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType == SpriteMeshType.FullRect) return;

            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            _spriteCache.Remove(path);
            Debug.Log($"[PlayerProPanelBuilder] 已把 {path} 的 Mesh Type 改为 FullRect（九宫拉伸需要）。");
        }

        private static Transform EnsureCanvas()
        {
            var existing = GameObject.Find(PreviewCanvasName);
            if (existing != null) return existing.transform;

            var canvasGo = new GameObject(PreviewCanvasName, typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUD Preview Canvas");

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            return canvasGo.transform;
        }
    }
}
