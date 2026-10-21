using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HotUpdate.UI.UIItem;
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
    //   5. 补引用绑定（不动层级）    —— 已有预制体只重连字段
    //
    // 版式（1920×1080 全屏，深色 + 荧光黄绿强调）：
    //   左上：头像 + 昵称(playerName) + 在线状态
    //   左中：竖排菜单 —— 进入游戏(HostBtn) / 私聊(FriendBtn) / 背包(DepotBtn) / 退出登录(ExitBtn)
    //   右侧：斜排的 UNDER WORLD 水印字
    //   边角：版本号 / 装饰线
    //
    // 约定：
    //   · 头像是复用 headFrame.prefab（自带 Mask），菜单项自绘（斜切感靠整体 -2.5° 旋转 + 左侧色条）。
    //   · 节点的引用由生成流程直接写进 StartPanel 的序列化字段，运行时不再按名字找。
    //   · 所有中文文本统一用项目中文 TMP 字体，英文装饰字用 Oswald（形更硬朗），否则中文会是方块。
    public static class StartPanelBuilder
    {
        // ---------- 字体 ----------
        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";
        private const string EnFontPath =
            "Assets/UnityShop/URP/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset";

        // ---------- 贴图 ----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpriteBg = TexDir + "background.png";
        private const string SpriteRoundFrame = TexDir + "round_frame.png";

        // ---------- 项目自带 UIItem 预制体 ----------
        private const string HeadFramePrefabPath = "Assets/Res/UI/UIItem/headFrame.prefab";

        // ---------- 输出 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/StartPanel.prefab";

        // ---------- 命名 ----------
        private const string RootName = "StartPanel";
        private const string CanvasName = "StartPanelTempCanvas";

        // ---------- 版式（相对屏幕中心，参考 1920×1080）----------
        private const float RuleX = -900f;          // 左侧竖线
        private const float MenuCenterX = -540f;    // 菜单列中心
        private const float MenuWidth = 460f;
        private const float MenuItemHeight = 84f;
        private const float MenuItemGap = 16f;
        private const float MenuTopY = 180f;        // 第一项中心
        private const float MenuTilt = -2.5f;       // 菜单整体倾斜角（斜切感）

        // ---------- 配色 ----------
        private static readonly Color ColBackdrop = new Color(0.035f, 0.035f, 0.045f, 1f);
        private static readonly Color ColShade = new Color(0.028f, 0.028f, 0.036f, 0.84f);
        private static readonly Color ColAccent = new Color(0.85f, 1f, 0.24f, 1f);      // 荧光黄绿 #D9FF3D
        private static readonly Color ColAccentHover = new Color(0.93f, 1f, 0.45f, 1f); // 主按钮悬停再亮一档
        private static readonly Color ColOnAccent = new Color(0.05f, 0.05f, 0.06f, 1f);
        private static readonly Color ColItemBg = new Color(0.085f, 0.085f, 0.105f, 0.94f);
        private static readonly Color ColText = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ColTextDim = new Color(0.42f, 0.43f, 0.5f, 1f);
        private static readonly Color ColWatermark = new Color(0.085f, 0.085f, 0.105f, 1f);

        private static TMP_FontAsset _font;
        private static TMP_FontAsset _enFont;
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

        // 只补引用、不动层级：改完 StartPanel 的序列化字段后点一下，把已有预制体的引用连上
        [MenuItem("Tools/UI/开始面板/5. 补引用绑定（不动层级）", false, 34)]
        public static void BindRefsOnPrefab()
        {
            if (!File.Exists(PanelPrefabPath))
            {
                Debug.LogWarning($"[StartPanelBuilder] 未找到 {PanelPrefabPath}，先执行菜单 3 生成预制体。");
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            BindRefs(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, PanelPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            AssetDatabase.SaveAssets();
            Debug.Log("[StartPanelBuilder] 已把 playerName / 头像 / 四个按钮的引用连到 StartPanel 上。");
        }

        // 面板上全是序列化字段，运行时不再按名字找 —— 由生成流程 / 菜单 5 一次性写进去
        private static void BindRefs(GameObject root)
        {
            // 头像框复用 headFrame.prefab，头像是里面的 headImage；老预制体上没 Button，缺就补一个
            Transform avatar = FindDeep(root.transform, "Avatar");
            Image head = FindDeep(avatar, "headImage").GetComponent<Image>();
            head.raycastTarget = true;

            var avatarBtn = avatar.GetComponent<Button>();
            if (avatarBtn == null) avatarBtn = avatar.gameObject.AddComponent<Button>();
            avatarBtn.targetGraphic = avatar.GetComponent<Image>();

            // 私聊入口的红点：已经摆过就复用，没有就补一个（右上角小圆点）
            Transform friendBtn = FindDeep(root.transform, "FriendBtn");
            Transform dotNode = FindDeep(friendBtn, "RedDot");
            GameObject dot = dotNode != null ? dotNode.gameObject : CreateRedDot(friendBtn).gameObject;
            dot.SetActive(false);

            var so = new SerializedObject(root.GetComponent<StartPanel>());
            so.FindProperty("playerNameTxt").objectReferenceValue =
                FindDeep(root.transform, "playerName").GetComponent<TMP_Text>();
            so.FindProperty("avatarImage").objectReferenceValue = head;
            so.FindProperty("avatarBtn").objectReferenceValue = avatarBtn;
            so.FindProperty("friendRedDot").objectReferenceValue = dot;
            so.FindProperty("hostBtn").objectReferenceValue = FindDeep(root.transform, "HostBtn").GetComponent<Button>();
            so.FindProperty("friendBtn").objectReferenceValue = FindDeep(root.transform, "FriendBtn").GetComponent<Button>();
            so.FindProperty("depotBtn").objectReferenceValue = FindDeep(root.transform, "DepotBtn").GetComponent<Button>();
            so.FindProperty("exitBtn").objectReferenceValue = FindDeep(root.transform, "ExitBtn").GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 红点：锚在按钮右上角的圆点，显隐由 StartPanel 按红点树控制
        private static Image CreateRedDot(Transform parent)
        {
            GameObject go = NewUI("RedDot", parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(22f, 22f);
            rt.anchoredPosition = new Vector2(-10f, -10f);

            var image = go.AddComponent<Image>();
            ApplySprite(image, TexDir + "circle_mask.png", false);
            image.color = new Color(1f, 0.26f, 0.42f, 1f);
            image.raycastTarget = false;
            return image;
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

            _enFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnFontPath);

            // ---- 根节点：铺满 + 深色底 + 逻辑组件 ----
            var root = NewUI(RootName, canvas);
            Undo.RegisterCreatedObjectUndo(root, "Create StartPanel");
            Stretch((RectTransform)root.transform);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = ColBackdrop;
            root.AddComponent<StartPanel>();

            // ---- 右上角一块倾斜的暗色块：纯色几何，代替原来的背景图 ----
            var slab = NewUI("Slab", root.transform);
            var srt = (RectTransform)slab.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(820f, 520f);
            srt.anchoredPosition = new Vector2(-160f, -150f);
            srt.localRotation = Quaternion.Euler(0f, 0f, -16f);
            var slabImg = slab.AddComponent<Image>();
            slabImg.color = new Color(1f, 1f, 1f, 0.022f);
            slabImg.raycastTarget = false;

            var shade = NewUI("Shade", root.transform);
            Stretch((RectTransform)shade.transform);
            var shadeImg = shade.AddComponent<Image>();
            shadeImg.color = ColShade;
            shadeImg.raycastTarget = false;

            BuildSideRule(root.transform);
            BuildProfile(root.transform);
            BuildMenu(root.transform);
            BuildWatermark(root.transform);
            BuildCorners(root.transform);

            BindRefs(root);
            return root;
        }

        // 左侧竖线 + 端点小方块：整套风格里唯一的"硬"装饰
        private static void BuildSideRule(Transform parent)
        {
            var line = NewUI("SideRule", parent);
            var lineRt = (RectTransform)line.transform;
            lineRt.anchorMin = new Vector2(0f, 0f);
            lineRt.anchorMax = new Vector2(0f, 1f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);
            lineRt.sizeDelta = new Vector2(3f, 0f);
            lineRt.anchoredPosition = new Vector2(RuleX, 0f);

            var lineImg = line.AddComponent<Image>();
            lineImg.color = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.55f);
            lineImg.raycastTarget = false;

            var cap = NewUI("Cap", line.transform);
            var capRt = (RectTransform)cap.transform;
            capRt.anchorMin = capRt.anchorMax = new Vector2(0.5f, 1f);
            capRt.pivot = new Vector2(0.5f, 1f);
            capRt.sizeDelta = new Vector2(18f, 46f);
            capRt.anchoredPosition = new Vector2(0f, -60f);

            var capImg = cap.AddComponent<Image>();
            capImg.color = ColAccent;
            capImg.raycastTarget = false;
        }

        // 左上：头像 + 昵称 + 状态行
        private static void BuildProfile(Transform parent)
        {
            var group = NewUI("Profile", parent);
            var groupRt = (RectTransform)group.transform;
            groupRt.anchorMin = groupRt.anchorMax = new Vector2(0.5f, 0.5f);
            groupRt.pivot = new Vector2(0.5f, 0.5f);
            groupRt.sizeDelta = new Vector2(700f, 180f);
            groupRt.anchoredPosition = new Vector2(-600f, 400f);

            BuildAvatar(group.transform);

            TMP_Text name = NewText("playerName", group.transform, "冒险者", 42f,
                TextAlignmentOptions.MidlineLeft, ColText, _font);
            PlaceCenter(name.rectTransform, new Vector2(150f, 22f), new Vector2(400f, 54f));

            TMP_Text status = NewText("Status", group.transform, "ONLINE", 18f,
                TextAlignmentOptions.MidlineLeft, ColAccent, _enFont);
            PlaceCenter(status.rectTransform, new Vector2(150f, -26f), new Vector2(400f, 26f));

            var underline = NewUI("Underline", group.transform);
            var ulRt = (RectTransform)underline.transform;
            ulRt.anchorMin = ulRt.anchorMax = new Vector2(0.5f, 0.5f);
            ulRt.pivot = new Vector2(0f, 0.5f);
            ulRt.sizeDelta = new Vector2(180f, 2f);
            ulRt.anchoredPosition = new Vector2(-50f, -56f);
            var ulImg = underline.AddComponent<Image>();
            ulImg.color = new Color(1f, 1f, 1f, 0.14f);
            ulImg.raycastTarget = false;
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
            rt.sizeDelta = new Vector2(150f, 150f);
            rt.anchoredPosition = new Vector2(-200f, 0f);

            // headFrame 里的头像是固定尺寸，铺满外框即可
            var inner = FindDeep(head.transform, "headImage") as RectTransform;
            if (inner != null) Stretch(inner, 12f, 12f, 12f, 12f);
        }

        // 左中：竖排菜单，整体轻微倾斜
        private static void BuildMenu(Transform parent)
        {
            var menu = NewUI("Menu", parent);
            var menuRt = (RectTransform)menu.transform;
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0.5f, 0.5f);
            menuRt.pivot = new Vector2(0.5f, 0.5f);
            menuRt.sizeDelta = new Vector2(MenuWidth, 520f);
            menuRt.anchoredPosition = new Vector2(MenuCenterX, -40f);
            menuRt.localRotation = Quaternion.Euler(0f, 0f, MenuTilt);

            float y = MenuTopY;

            // 主入口：荧光底 + 黑字，唯一一个高亮项
            NewMenuItem(menu.transform, "HostBtn", "进入游戏", "01", y, true);
            y -= MenuItemHeight + MenuItemGap;

            NewMenuItem(menu.transform, "FriendBtn", "私聊", "02", y, false);
            y -= MenuItemHeight + MenuItemGap;

            NewMenuItem(menu.transform, "DepotBtn", "背包", "03", y, false);
            y -= MenuItemHeight + MenuItemGap;

            NewMenuItem(menu.transform, "ExitBtn", "退出登录", "04", y, false);
        }

        private static Button NewMenuItem(Transform parent, string name, string label, string index,
            float y, bool primary)
        {
            var go = NewUI(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(MenuWidth, MenuItemHeight);
            rt.anchoredPosition = new Vector2(0f, y);

            var img = go.AddComponent<Image>();
            img.color = primary ? ColAccent : ColItemBg;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // ColorTint 是乘法（底色 × 状态色），黑底乘什么都是黑，所以悬停变色交给 ButtonHoverTint：
            // 次按钮整块变荧光黄、文字和左侧色条转近黑，和主按钮同一套视觉
            btn.transition = Selectable.Transition.None;

            var tint = go.AddComponent<ButtonHoverTint>();
            tint.SetColors(primary ? ColAccentHover : ColAccent, ColOnAccent, 0.1f);

            // 左侧色条：主按钮用黑色，次按钮用荧光色
            var bar = NewUI("Bar", go.transform);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(6f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            var barImg = bar.AddComponent<Image>();
            barImg.color = primary ? ColOnAccent : ColAccent;
            barImg.raycastTarget = false;

            // 序号：英文窄体，弱化
            TMP_Text idx = NewText("Index", go.transform, index, 18f,
                TextAlignmentOptions.MidlineLeft, primary ? new Color(0f, 0f, 0f, 0.45f) : ColTextDim, _enFont);
            PlaceCenter(idx.rectTransform, new Vector2(-160f, 0f), new Vector2(60f, 24f));

            // 文案
            TMP_Text txt = NewText("Label", go.transform, label, 36f,
                TextAlignmentOptions.MidlineLeft, primary ? ColOnAccent : ColText, _font);
            PlaceCenter(txt.rectTransform, new Vector2(44f, 0f), new Vector2(300f, 48f));

            // 悬停放大
            var hover = go.AddComponent<HoverScale>();
            hover.SetScale(primary ? 1.035f : 1.02f, 0.1f);

            return btn;
        }

        // 右下：斜排的水印大字（英文用 Oswald，形比中文楷体硬）
        private static void BuildWatermark(Transform parent)
        {
            TMP_Text mark = NewText("Watermark", parent, "UNDER WORLD", 132f,
                TextAlignmentOptions.Right, ColWatermark, _enFont);
            PlaceCenter(mark.rectTransform, new Vector2(260f, -230f), new Vector2(1100f, 180f));
            mark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5f);
        }

        // 边角小字：右上版本 / 左下项目标识
        private static void BuildCorners(Transform parent)
        {
            TMP_Text brand = NewText("Brand", parent, "BUILD 0.1.0  //  ONLINE", 18f,
                TextAlignmentOptions.Right, ColTextDim, _enFont);
            PlaceCenter(brand.rectTransform, new Vector2(700f, 486f), new Vector2(500f, 26f));

            TMP_Text footer = NewText("Footer", parent, "UNDERWORDYOO  热更客户端", 16f,
                TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.22f), _font);
            PlaceCenter(footer.rectTransform, new Vector2(-540f, -500f), new Vector2(600f, 24f));
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
            TextAlignmentOptions align, Color color, TMP_FontAsset font)
        {
            var go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null) tmp.font = font;
            return tmp;
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
