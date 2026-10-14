using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HotUpdate.UI.UIPanel;

namespace ProjectEditor
{
    // 一键重建「开始面板」(StartPanel) 的整份层级。
    //
    // 菜单：Tools/UI/开始面板
    //   1. 在当前场景生成          —— 在临时 Canvas 下搭出整套层级（可直接预览 / 微调）
    //   2. 保存选中对象为 Prefab    —— 把生成的面板存成预制体
    //   3. 一键生成并保存为 Prefab  —— 1 + 2 连做（最常用）
    //   4. 注册到 Addressables     —— 按地址 StartPanel 注册（路径没变的话本来就已经注册过）
    //
    // 版式：全屏背景 + 暗化遮罩 + 居中九宫主面板
    //   左列：头像 + 昵称(playerName) + 副标题
    //   右列：进入游戏(HostBtn) / 私聊(FriendBtn) / 背包(DepotBtn) / 退出游戏(ExitBtn)
    //
    // 约定：
    //   · 视觉全部复用 SharpUI 资源（预制体 RectButton；贴图 background / info_box /
    //     dialog_header_background / rect_button / round_frame）。
    //   · 节点名必须和 StartPanel.BindHierarchy 里的查找名一致，勿随意改。
    //   · 所有文本统一用项目中文 TMP 字体，否则中文会是方块。
    public static class StartPanelBuilder
    {
        // ---------- 字体 ----------
        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        // ---------- SharpUI 贴图 ----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpriteBg = TexDir + "background.png";
        private const string SpritePanelBg = TexDir + "info_box.png";
        private const string SpriteHeader = TexDir + "dialog_header_background.png";
        private const string SpriteButton = TexDir + "rect_button.png";
        private const string SpriteRoundFrame = TexDir + "round_frame.png";

        // ---------- SharpUI 预制体 ----------
        private const string SharpPrefabDir = "Assets/Resources/SharpUI/Prefabs/UI/";
        private const string SharpButtonPath = SharpPrefabDir + "Button/RectButton.prefab";

        // ---------- 项目自带 UIItem 预制体 ----------
        private const string HeadFramePrefabPath = "Assets/Res/UI/UIItem/headFrame.prefab";

        // ---------- 图标 ----------
        private const string IconHost = TexDir + "sword.png";
        private const string IconFriend = TexDir + "icon_male.png";
        private const string IconDepot = TexDir + "icon_bottle.png";
        private const string IconExit = TexDir + "icon_close_light.png";

        // ---------- 输出 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/StartPanel.prefab";

        // ---------- 命名 ----------
        private const string RootName = "StartPanel";
        private const string CanvasName = "StartPanelTempCanvas";

        // ---------- 版式 ----------
        private const float PanelWidth = 1120f;
        private const float PanelHeight = 640f;
        private const float HeaderHeight = 80f;
        private const float LeftColumnX = -300f;
        private const float RightColumnX = 250f;
        private const float ButtonWidth = 380f;
        private const float ButtonHeight = 86f;
        private const float ButtonGap = 22f;

        // ---------- 配色 ----------
        private static readonly Color ColBackdrop = new Color(0.05f, 0.06f, 0.09f, 1f);
        private static readonly Color ColDim = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color ColTitle = new Color(1f, 0.93f, 0.76f, 1f);
        private static readonly Color ColName = new Color(0.96f, 0.97f, 1f, 1f);
        private static readonly Color ColSub = new Color(0.66f, 0.69f, 0.76f, 1f);
        private static readonly Color ColLabel = new Color(0.97f, 0.98f, 1f, 1f);

        private static TMP_FontAsset _font;
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly HashSet<string> _fullRectFixed = new();

        // =====================================================================
        // 菜单入口
        // =====================================================================

        [MenuItem("Tools/UI/开始面板/1. 在当前场景生成", false, 30)]
        public static void BuildInScene()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("[StartPanelBuilder] 已生成开始面板层级，满意后用菜单 " +
                      "「Tools/UI/开始面板/3. 一键生成并保存为 Prefab」存成预制体。");
        }

        [MenuItem("Tools/UI/开始面板/2. 保存选中对象为 Prefab", false, 31)]
        public static void SaveSelectionAsPrefab()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null || root.name != RootName)
            {
                EditorUtility.DisplayDialog("保存失败",
                    "请先在层级面板选中生成的开始面板根节点（名字应为 StartPanel）。", "好");
                return;
            }

            SavePanel(root);
        }

        [MenuItem("Tools/UI/开始面板/3. 一键生成并保存为 Prefab", false, 32)]
        public static void BuildAndSave()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);
            SavePanel(root);
        }

        [MenuItem("Tools/UI/开始面板/4. 注册到 Addressables", false, 33)]
        public static void RegisterAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("注册失败",
                    "当前工程还没有 Addressables 设置（Window/Asset Management/Addressables/Groups 先创建）。", "好");
                return;
            }

            if (!File.Exists(PanelPrefabPath))
            {
                Debug.LogWarning($"[StartPanelBuilder] 未找到 {PanelPrefabPath}，先执行菜单 3 生成预制体。");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(PanelPrefabPath);
            if (string.IsNullOrEmpty(guid)) return;

            var entry = settings.CreateOrMoveEntry(guid, ResolveGroup(settings, "UIPanel"), false, false);
            entry.address = "StartPanel";

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StartPanelBuilder] 已按地址 StartPanel 注册到 Addressables。");
        }

        // 优先用名字完全等于关键字的组，找不到再退回「名字含 UI」的组，最后用默认组
        private static AddressableAssetGroup ResolveGroup(AddressableAssetSettings settings, string keyword)
        {
            foreach (var g in settings.groups)
            {
                if (g != null && !string.IsNullOrEmpty(g.Name) && g.Name == keyword) return g;
            }

            AddressableAssetGroup fallback = null;
            foreach (var g in settings.groups)
            {
                if (g == null || string.IsNullOrEmpty(g.Name)) continue;
                if (g.Name.ToUpperInvariant().Contains("UI")) { fallback = g; break; }
            }

            return fallback ?? settings.DefaultGroup;
        }

        private static void SavePanel(GameObject root)
        {
            EnsureFolder(PanelPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StartPanelBuilder] 预制体已保存：{PanelPrefabPath}\n" +
                      "面板地址没变（仍是 StartPanel），Addressables 里已有的注册项继续有效，不必重新注册。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath));
        }

        // =====================================================================
        // 构建层级
        // =====================================================================

        private static GameObject BuildHierarchy(Transform canvas)
        {
            _spriteCache.Clear();

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null)
                Debug.LogWarning($"[StartPanelBuilder] 未找到字体资源：{FontPath}，文本将回退默认字体（中文可能显示为方块）。");

            // ---- 根节点：铺满 + 深色底 + 逻辑组件 ----
            var root = NewUI(RootName, canvas);
            Undo.RegisterCreatedObjectUndo(root, "Create StartPanel");
            Stretch((RectTransform)root.transform);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = ColBackdrop;
            root.AddComponent<StartPanel>();

            // ---- 背景图（16:9，直接拉满不跑偏）----
            var bg = NewUI("Bg", root.transform);
            Stretch((RectTransform)bg.transform);
            var bgImg = bg.AddComponent<Image>();
            ApplySprite(bgImg, SpriteBg, false);
            bgImg.raycastTarget = false;

            // ---- 暗化遮罩，压住背景让面板与文字跳出来 ----
            var dim = NewUI("Dim", root.transform);
            Stretch((RectTransform)dim.transform);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = ColDim;
            dimImg.raycastTarget = false;

            // ---- 主面板 ----
            var panel = NewUI("Panel", root.transform);
            Center((RectTransform)panel.transform, new Vector2(PanelWidth, PanelHeight));
            ApplySprite(panel.AddComponent<Image>(), SpritePanelBg, true);

            BuildHeader(panel.transform);
            BuildLeftColumn(panel.transform);
            BuildRightColumn(panel.transform);
            BuildFooter(root.transform);

            return root;
        }

        private static void BuildHeader(Transform parent)
        {
            var header = NewUI("Header", parent);
            var hrt = (RectTransform)header.transform;
            Anchor(hrt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(0f, HeaderHeight);
            hrt.anchoredPosition = Vector2.zero;
            ApplySprite(header.AddComponent<Image>(), SpriteHeader, false);

            TMP_Text title = NewText("Title", header.transform, "开始游戏", 34f, TextAlignmentOptions.Center, ColTitle);
            Stretch(title.rectTransform);
        }

        // 左列：头像 + 昵称 + 副标题
        private static void BuildLeftColumn(Transform parent)
        {
            BuildAvatar(parent);

            TMP_Text name = NewText("playerName", parent, "欢迎你：冒险者", 40f, TextAlignmentOptions.Center, ColName);
            PlaceCenter(name.rectTransform, new Vector2(LeftColumnX, -90f), new Vector2(470f, 60f));

            TMP_Text sub = NewText("SubTitle", parent, "祝你好运，冒险者", 20f, TextAlignmentOptions.Center, ColSub);
            PlaceCenter(sub.rectTransform, new Vector2(LeftColumnX, -160f), new Vector2(470f, 34f));
        }

        // 头像：复用 headFrame.prefab（自带 Mask，能把内部头像裁进框里）
        private static void BuildAvatar(Transform parent)
        {
            GameObject head = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeadFramePrefabPath);
            if (prefab != null) head = InstantiateUnpacked(prefab, parent, "Avatar");

            if (head == null)
            {
                head = NewUI("Avatar", parent);
                ApplySprite(head.AddComponent<Image>(), SpriteRoundFrame, false);
            }

            var rt = (RectTransform)head.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(170f, 170f);
            rt.anchoredPosition = new Vector2(LeftColumnX, 70f);

            // headFrame 里的头像是固定尺寸，铺满外框即可
            var inner = FindDeep(head.transform, "headImage") as RectTransform;
            if (inner != null)
            {
                Stretch(inner, 12f, 12f, 12f, 12f);
            }
        }

        // 右列：进入游戏 / 私聊 / 背包 / 退出游戏
        private static void BuildRightColumn(Transform parent)
        {
            float top = PanelHeight * 0.5f - HeaderHeight - 30f;
            float y = top - ButtonHeight * 0.5f;

            // 进入游戏：比其它按钮更宽 + 暖色调，视觉上作为主按钮
            Button host = NewButton("HostBtn", parent, "进入游戏", IconHost);
            PlaceCenter((RectTransform)host.transform, new Vector2(RightColumnX, y),
                new Vector2(ButtonWidth + 24f, ButtonHeight + 12f));
            TintButton(host, new Color(1f, 0.96f, 0.84f, 1f));
            SetButtonLabelSize(host, 32f);

            y -= ButtonHeight + ButtonGap;

            Button friend = NewButton("FriendBtn", parent, "私聊", IconFriend);
            PlaceCenter((RectTransform)friend.transform, new Vector2(RightColumnX, y),
                new Vector2(ButtonWidth, ButtonHeight));

            y -= ButtonHeight + ButtonGap;

            Button depot = NewButton("DepotBtn", parent, "背包", IconDepot);
            PlaceCenter((RectTransform)depot.transform, new Vector2(RightColumnX, y),
                new Vector2(ButtonWidth, ButtonHeight));

            y -= ButtonHeight + ButtonGap;

            Button exit = NewButton("ExitBtn", parent, "退出游戏", IconExit);
            PlaceCenter((RectTransform)exit.transform, new Vector2(RightColumnX, y),
                new Vector2(ButtonWidth, ButtonHeight));
        }

        private static void BuildFooter(Transform parent)
        {
            TMP_Text version = NewText("Footer", parent, "UnderWordYoo  ·  热更客户端", 20f,
                TextAlignmentOptions.Center, ColSub);
            PlaceCenter(version.rectTransform, new Vector2(0f, -500f), new Vector2(800f, 32f));
        }

        // =====================================================================
        // 控件工厂
        // =====================================================================

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
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

        // 复用 SharpUI RectButton（自带 rect_button 贴图），再补一个左侧图标
        private static Button NewButton(string name, Transform parent, string label, string iconPath)
        {
            Button btn = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharpButtonPath);
            if (prefab != null)
            {
                var inst = InstantiateUnpacked(prefab, parent, name);
                if (inst != null)
                {
                    btn = inst.GetComponent<Button>();
                    if (btn == null) Object.DestroyImmediate(inst);
                }
            }

            if (btn == null)
            {
                // 回退：自绘
                var go = NewUI(name, parent);
                var img = go.AddComponent<Image>();
                ApplySprite(img, SpriteButton, true);
                btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                TMP_Text fb = NewText("Label", go.transform, label, 28f, TextAlignmentOptions.Center, ColLabel);
                Stretch(fb.rectTransform);
                return btn;
            }

            StyleButton(btn, label, iconPath);
            return btn;
        }

        // 统一按钮文案左对齐 + 左侧图标，保持四个按钮视觉一致
        private static void StyleButton(Button btn, string label, string iconPath)
        {
            if (btn == null) return;

            bool labeled = false;
            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true))
            {
                if (_font != null) t.font = _font;
                if (labeled)
                {
                    t.enabled = false;
                    continue;
                }

                t.text = label;
                t.fontSize = 28f;
                t.fontStyle = FontStyles.Normal;
                t.color = ColLabel;
                t.alignment = TextAlignmentOptions.MidlineLeft;
                t.textWrappingMode = TextWrappingModes.NoWrap;

                var trt = t.rectTransform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(104f, 0f);
                trt.offsetMax = new Vector2(-28f, 0f);
                labeled = true;
            }

            if (string.IsNullOrEmpty(iconPath)) return;

            var iconGo = NewUI("Icon", btn.transform);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(52f, 52f);
            iconRt.anchoredPosition = new Vector2(34f, 0f);

            var iconImg = iconGo.AddComponent<Image>();
            ApplySprite(iconImg, iconPath, false);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        private static void TintButton(Button btn, Color color)
        {
            var img = btn != null ? btn.GetComponent<Image>() : null;
            if (img != null) img.color = color;
        }

        private static void SetButtonLabelSize(Button btn, float size)
        {
            if (btn == null) return;
            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true)) t.fontSize = size;
        }

        // =====================================================================
        // 工具
        // =====================================================================

        private static GameObject InstantiateUnpacked(GameObject prefab, Transform parent, string newName)
        {
            var inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (inst == null) return null;

            if (PrefabUtility.IsPartOfPrefabInstance(inst))
            {
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            StripMissingScripts(inst);
            inst.name = newName;
            return inst;
        }

        // SharpUI 预制体里带了一些本工程没有的示例脚本（显示为 Missing Script），统一清掉
        private static void StripMissingScripts(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        private static Transform FindDeep(Transform root, string nodeName)
        {
            if (root == null) return null;
            if (root.name == nodeName) return root;

            int count = root.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform found = FindDeep(root.GetChild(i), nodeName);
                if (found != null) return found;
            }

            return null;
        }

        // ---------- Sprite ----------

        private static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (_spriteCache.TryGetValue(path, out var cached)) return cached;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[StartPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{path}");

            _spriteCache[path] = sprite;
            return sprite;
        }

        private static void ApplySprite(Image img, string spritePath, bool sliced)
        {
            if (img == null) return;
            if (string.IsNullOrEmpty(spritePath))
            {
                img.color = Color.white;
                return;
            }

            // 九宫渲染要求 Sprite 的 mesh 为 FullRect（SharpUI 贴图默认 Tight，九宫会失效）
            if (sliced) EnsureFullRect(spritePath);

            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.color = Color.white;
        }

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
            Debug.Log($"[StartPanelBuilder] 已把 {path} 的 Mesh Type 改为 FullRect（九宫拉伸需要）。");
        }

        // ---------- RectTransform ----------

        private static void Stretch(RectTransform rt, float left = 0, float right = 0, float bottom = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
        }

        private static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        // 相对父节点中心摆放（父节点 pivot 为 (0.5,0.5) 时即「容器内坐标」）
        private static void PlaceCenter(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static GameObject EnsureCanvas()
        {
            var existing = GameObject.Find(CanvasName);
            if (existing != null) return existing;

            var canvasGo = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Start Canvas");

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }
            return canvasGo;
        }

        private static void EnsureFolder(string assetPath)
        {
            string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(dir)) return;

            string[] parts = dir.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
