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
    // 一键生成「房间面板」(RoomPanel) 的完整 UI 层级，省去手动摆放。
    //
    // 菜单：Tools/UI/房间面板
    //   1. 在当前场景生成          —— 在临时 Canvas 下搭出整套层级（可直接预览/微调）
    //   2. 保存选中对象为 Prefab    —— 把生成的面板 + 两个列表项模板存成预制体
    //   3. 一键生成并保存为 Prefab  —— 1 + 2 连做
    //   4. 注册到 Addressables     —— 把 3 个预制体按 RoomPanel / RoomListItem / RoomPlayerItem 注册
    //
    // 面板两种形态（同属一个预制体，运行时由 RoomPanel 切换显隐）：
    //   ListArea —— 房间大厅：房间列表 + 创建 + 加入 + 刷新
    //   RoomArea —— 房间内：玩家列表 + 聊天区 + 准备 / 开始 / 退出
    //
    // 约定：
    //   · 视觉全部复用 SharpUI 资源（预制体 RectButton / IconButton / InputField / ListItemCustom；
    //     贴图 info_box / dialog_header_background / rect_button / rect_input /
    //     list_item_background(+_selected) / icon_close_light / scroll_*）。
    //   · 聊天消息气泡不在这里生成 —— 直接用现成的 ChatRootLeft / ChatRootRight（已在 Addressables 里），
    //     它们自带 LayoutGroup，气泡高度自适应。
    //   · 节点命名必须与 RoomPanel.BindHierarchy 里的查找名一致，勿随意改。
    public static class RoomPanelBuilder
    {
        // ---------- 字体 ----------
        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        // ---------- SharpUI 贴图 ----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpritePanelBg = TexDir + "info_box.png";
        private const string SpriteHeader = TexDir + "dialog_header_background.png";
        private const string SpriteButton = TexDir + "rect_button.png";
        private const string SpriteInput = TexDir + "rect_input.png";
        private const string SpriteListItem = TexDir + "list_item_background.png";
        private const string SpriteListItemSel = TexDir + "list_item_background_selected.png";
        private const string SpriteScrollBg = TexDir + "scroll_background.png";
        private const string SpriteScrollHandle = TexDir + "scroll_handle.png";

        // ---------- SharpUI 预制体 ----------
        private const string SharpPrefabDir = "Assets/Resources/SharpUI/Prefabs/UI/";
        private const string SharpButtonPath = SharpPrefabDir + "Button/RectButton.prefab";
        private const string SharpIconButtonPath = SharpPrefabDir + "Button/IconButton.prefab";
        private const string SharpInputPath = SharpPrefabDir + "Input/InputField.prefab";
        private const string SharpListItemPath = SharpPrefabDir + "List/ListItemCustom.prefab";

        // ---------- 项目自带 UIItem 预制体 ----------
        private const string HeadFramePrefabPath = "Assets/Res/UI/UIItem/headFrame.prefab";

        // ---------- 输出 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/RoomPanel.prefab";
        private const string RoomItemPrefabPath = "Assets/Res/UI/UIItem/RoomListItem.prefab";
        private const string MemberItemPrefabPath = "Assets/Res/UI/UIItem/RoomPlayerItem.prefab";

        // ---------- 命名（与 RoomPanel 绑定逻辑对应）----------
        private const string RootName = "RoomPanel";
        private const string CanvasName = "RoomPanelTempCanvas";
        private const string TemplateHolder = "RoomPanelTemplates";

        // 列表项固定高度（运行时 LoopList 会按自己的 itemHeight 重设 RectTransform，这里只决定模板初始尺寸）
        private const float RoomItemHeight = 76f;
        private const float MemberItemHeight = 64f;

        private const float ScrollbarWidth = 16f;

        // ---------- 配色 ----------
        private static readonly Color ColMask = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color ColName = new Color(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color ColSub = new Color(0.66f, 0.69f, 0.76f, 1f);

        private static TMP_FontAsset _font;
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly HashSet<string> _fullRectFixed = new();

        // =====================================================================
        // 菜单入口
        // =====================================================================

        [MenuItem("Tools/UI/房间面板/1. 在当前场景生成", false, 20)]
        public static void BuildInScene()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);
            BuildTemplates(canvas.transform);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("[RoomPanelBuilder] 已生成房间面板层级，调整满意后用菜单 " +
                      "「Tools/UI/房间面板/2. 保存选中对象为 Prefab」存成预制体。");
        }

        [MenuItem("Tools/UI/房间面板/2. 保存选中对象为 Prefab", false, 21)]
        public static void SaveSelectionAsPrefab()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null || root.transform.Find("Panel") == null)
            {
                EditorUtility.DisplayDialog("保存失败",
                    "请先在层级面板选中生成的房间面板根节点（应包含 Panel 子节点）。", "好");
                return;
            }

            SavePanelAndTemplates(root, null);
        }

        [MenuItem("Tools/UI/房间面板/3. 一键生成并保存为 Prefab", false, 22)]
        public static void BuildAndSave()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);
            GameObject templates = BuildTemplates(canvas.transform);
            SavePanelAndTemplates(root, templates);
        }

        [MenuItem("Tools/UI/房间面板/4. 注册到 Addressables", false, 23)]
        public static void RegisterAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("注册失败",
                    "当前工程还没有 Addressables 设置（Window/Asset Management/Addressables/Groups 先创建）。", "好");
                return;
            }

            RegisterEntry(settings, ResolveGroup(settings, "UIPanel"), PanelPrefabPath, "RoomPanel");
            RegisterEntry(settings, ResolveGroup(settings, "UIItem"), RoomItemPrefabPath, "RoomListItem");
            RegisterEntry(settings, ResolveGroup(settings, "UIItem"), MemberItemPrefabPath, "RoomPlayerItem");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoomPanelBuilder] 已注册到 Addressables：RoomPanel（UIPanel 组）/ " +
                      "RoomListItem、RoomPlayerItem（UIItem 组）。");
        }

        // 优先用名字里带关键字的组，找不到再退回「名字含 UI」的组，最后用默认组
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

        private static void RegisterEntry(AddressableAssetSettings settings, AddressableAssetGroup group,
            string assetPath, string address)
        {
            if (group == null) return;

            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"[RoomPanelBuilder] 未找到 {assetPath}，先执行菜单 3 生成预制体。");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) return;

            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
        }

        // =====================================================================
        // 保存
        // =====================================================================

        private static void SavePanelAndTemplates(GameObject root, GameObject templates)
        {
            EnsureFolder(PanelPrefabPath);
            EnsureFolder(RoomItemPrefabPath);
            EnsureFolder(MemberItemPrefabPath);

            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);

            // 模板容器是隐藏的（SetActive(false)），GameObject.Find 找不到未激活对象，
            // 只能用 Transform.Find（能命中未激活子节点）或直接用传进来的引用。
            GameObject holder = templates;
            if (holder == null && root.transform.parent != null)
                holder = root.transform.parent.Find(TemplateHolder)?.gameObject;

            if (holder != null)
            {
                SaveTemplateChild<RoomListItem>(holder.transform, "RoomListItem", RoomItemPrefabPath);
                SaveTemplateChild<RoomPlayerItem>(holder.transform, "RoomPlayerItem", MemberItemPrefabPath);
            }
            else
            {
                Debug.LogWarning("[RoomPanelBuilder] 未找到模板容器（RoomPanelTemplates），请先用菜单 1 生成层级。");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RoomPanelBuilder] 预制体已保存：\n  {PanelPrefabPath}\n  {RoomItemPrefabPath}\n  {MemberItemPrefabPath}\n" +
                      "接下来执行「Tools/UI/房间面板/4. 注册到 Addressables」。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath));
        }

        // 保存前确保模板自身是激活的，否则预制体根节点会带着 inactive 存下去
        private static void SaveTemplateChild<T>(Transform holder, string childName, string savePath) where T : Component
        {
            var child = holder.Find(childName);
            if (child == null)
            {
                Debug.LogWarning($"[RoomPanelBuilder] 模板容器里没有 {childName}，未生成 {savePath}。");
                return;
            }

            if (child.GetComponent<T>() == null)
                Debug.LogWarning($"[RoomPanelBuilder] {childName} 上缺少 {typeof(T).Name} 组件，生成的列表项会取不到数据。");

            bool wasActive = child.gameObject.activeSelf;
            child.gameObject.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(child.gameObject, savePath);
            child.gameObject.SetActive(wasActive);
        }

        // =====================================================================
        // 构建层级
        // =====================================================================

        private static GameObject BuildHierarchy(Transform canvas)
        {
            _spriteCache.Clear();

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (_font == null)
                Debug.LogWarning($"[RoomPanelBuilder] 未找到字体资源：{FontPath}，文本将回退默认字体（中文可能显示为方块）。");

            var root = NewUI(RootName, canvas);
            Undo.RegisterCreatedObjectUndo(root, "Create RoomPanel");
            Stretch((RectTransform)root.transform);
            root.AddComponent<Image>().color = ColMask;
            root.AddComponent<RoomPanel>();

            var panel = NewUI("Panel", root.transform);
            Center((RectTransform)panel.transform, new Vector2(1400, 800));
            ApplySprite(panel.AddComponent<Image>(), SpritePanelBg, true);

            BuildHeader(panel.transform);
            BuildHomeButton(panel.transform);
            BuildListArea(panel.transform);
            BuildRoomArea(panel.transform);

            return root;
        }

        private static void BuildHeader(Transform parent)
        {
            var header = NewUI("Header", parent);
            var hrt = (RectTransform)header.transform;
            Anchor(hrt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(0, 72);
            hrt.anchoredPosition = Vector2.zero;
            ApplySprite(header.AddComponent<Image>(), SpriteHeader, false);

            var title = NewText("Title", header.transform, "房间", 34, TextAlignmentOptions.Center, ColName);
            Stretch(title.rectTransform);
        }

        // 顶部右上角的「返回主界面」：两种形态共用（房间面板不提供直接关闭，避免玩家卡在房间里）
        private static void BuildHomeButton(Transform parent)
        {
            var homeBtn = NewButton("HomeBtn", parent, "返回主界面");
            var hrt = homeBtn.GetComponent<RectTransform>();
            Anchor(hrt, new Vector2(1f, 1f), new Vector2(1f, 1f));
            hrt.pivot = new Vector2(1f, 1f);
            hrt.sizeDelta = new Vector2(170, 46);
            hrt.anchoredPosition = new Vector2(-20, -13);
        }

        // ---------------- 形态一：房间大厅 ----------------

        private static void BuildListArea(Transform parent)
        {
            var area = NewUI("ListArea", parent);
            Stretch((RectTransform)area.transform); // 铺满 Panel，子节点各自定位

            // 左：房间列表
            var listBg = NewUI("RoomListBg", area.transform);
            var lrt = (RectTransform)listBg.transform;
            Anchor(lrt, new Vector2(0f, 0f), new Vector2(0f, 1f));
            lrt.offsetMin = new Vector2(20, 20);
            lrt.offsetMax = new Vector2(-620, -92);
            ApplySprite(listBg.AddComponent<Image>(), SpritePanelBg, true);

            var title = NewText("RoomListTitle", listBg.transform, "房间列表", 26, TextAlignmentOptions.Center, ColName);
            var trt = title.rectTransform;
            Anchor(trt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0, 48);
            trt.anchoredPosition = Vector2.zero;

            BuildScrollArea("RoomScroll", listBg.transform, 8, 8, 8, 56, true);

            // 右：创建 / 加入 / 刷新
            var opBg = NewUI("RoomOpBg", area.transform);
            var ort = (RectTransform)opBg.transform;
            Anchor(ort, new Vector2(1f, 0f), new Vector2(1f, 1f));
            ort.offsetMin = new Vector2(-580, 20);
            ort.offsetMax = new Vector2(-20, -92);
            ApplySprite(opBg.AddComponent<Image>(), SpritePanelBg, true);

            NewLabel(opBg.transform, "CreateTitle", "创建房间", -24);
            var nameInput = NewInput("RoomNameInput", opBg.transform, "输入房间名称");
            PlaceTopLeftInput((RectTransform)nameInput.transform, -74, 380, 46);
            var createBtn = NewButton("CreateBtn", opBg.transform, "创建");
            PlaceTopLeft((RectTransform)createBtn.transform, new Vector2(416, -74), new Vector2(128, 46));

            NewLabel(opBg.transform, "JoinTitle", "加入房间", -160);
            var idInput = NewInput("RoomIdInput", opBg.transform, "输入房间号");
            PlaceTopLeftInput((RectTransform)idInput.transform, -210, 380, 46);
            var joinBtn = NewButton("JoinBtn", opBg.transform, "加入");
            PlaceTopLeft((RectTransform)joinBtn.transform, new Vector2(416, -210), new Vector2(128, 46));

            var refreshBtn = NewButton("RefreshBtn", opBg.transform, "刷新列表");
            PlaceTopLeft((RectTransform)refreshBtn.transform, new Vector2(24, -292), new Vector2(200, 46));
        }

        // ---------------- 形态二：房间内 ----------------

        private static void BuildRoomArea(Transform parent)
        {
            var area = NewUI("RoomArea", parent);
            Stretch((RectTransform)area.transform);

            // 左：玩家列表 + 底部按钮
            var listBg = NewUI("PlayerListBg", area.transform);
            var lrt = (RectTransform)listBg.transform;
            Anchor(lrt, new Vector2(0f, 0f), new Vector2(0f, 1f));
            lrt.offsetMin = new Vector2(20, 20);
            lrt.offsetMax = new Vector2(-620, -92);
            ApplySprite(listBg.AddComponent<Image>(), SpritePanelBg, true);

            var title = NewText("PlayerTitle", listBg.transform, "房间玩家", 26, TextAlignmentOptions.Center, ColName);
            var trt = title.rectTransform;
            Anchor(trt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0, 48);
            trt.anchoredPosition = Vector2.zero;

            // 底部 76 留给按钮
            BuildScrollArea("PlayerScroll", listBg.transform, 8, 8, 76, 56, true);

            var readyBtn = NewButton("ReadyBtn", listBg.transform, "准备");
            PlaceBottomLeft((RectTransform)readyBtn.transform, new Vector2(20, 20), new Vector2(150, 46));

            var startBtn = NewButton("StartBtn", listBg.transform, "开始游戏");
            PlaceBottomLeft((RectTransform)startBtn.transform, new Vector2(184, 20), new Vector2(170, 46));

            // 房间内不留「关闭」：LeaveBtn 是「返回上一级」（回房间列表），HomeBtn 才是回主界面
            var leaveBtn = NewButton("LeaveBtn", listBg.transform, "返回房间列表");
            PlaceBottomRight((RectTransform)leaveBtn.transform, new Vector2(-20, 20), new Vector2(170, 46));

            // 右：聊天区
            var chatBg = NewUI("ChatBg", area.transform);
            var crt = (RectTransform)chatBg.transform;
            Anchor(crt, new Vector2(1f, 0f), new Vector2(1f, 1f));
            crt.offsetMin = new Vector2(-580, 20);
            crt.offsetMax = new Vector2(-20, -92);
            ApplySprite(chatBg.AddComponent<Image>(), SpritePanelBg, true);

            var chatTitle = NewText("ChatTitle", chatBg.transform, "房间 1", 26, TextAlignmentOptions.Center, ColName);
            var ctrt = chatTitle.rectTransform;
            Anchor(ctrt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            ctrt.pivot = new Vector2(0.5f, 1f);
            ctrt.sizeDelta = new Vector2(0, 48);
            ctrt.anchoredPosition = Vector2.zero;

            // 消息区：直接用现成的 ChatRootLeft / ChatRootRight 气泡（高度自适应）
            BuildScrollArea("MsgScroll", chatBg.transform, 16, 16, 76, 52, false);

            var msgInput = NewInput("MsgInput", chatBg.transform, "输入消息");
            PlaceBottomLeft((RectTransform)msgInput.transform, new Vector2(20, 20), new Vector2(400, 44));

            var sendBtn = NewButton("SendBtn", chatBg.transform, "发送");
            PlaceBottomRight((RectTransform)sendBtn.transform, new Vector2(-20, 20), new Vector2(110, 44));

            area.SetActive(false); // 默认展示大厅形态
        }

        // 生成一个「ScrollRect + Viewport/Content + Vertical Scrollbar」的滚动区
        //   withLoopList = true  —— Content 顶部锚定、挂 LoopList（固定项高，列表用）
        //   withLoopList = false —— Content 挂 VerticalLayoutGroup + ContentSizeFitter（子项自适应高度，聊天用）
        private static void BuildScrollArea(string name, Transform parent,
            float left, float right, float bottom, float top, bool withLoopList)
        {
            var host = NewUI(name, parent);
            var hrt = (RectTransform)host.transform;
            Stretch(hrt, left, right, bottom, top);

            var scroll = host.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = NewUI("Viewport", host.transform);
            var vrt = (RectTransform)viewport.transform;
            Stretch(vrt, 0, ScrollbarWidth + 4, 0, 0);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport.transform);
            if (withLoopList) TopAnchoredContent((RectTransform)content.transform);
            else AutoHeightContent((RectTransform)content.transform);

            scroll.viewport = vrt;
            scroll.content = (RectTransform)content.transform;

            var scrollbar = BuildScrollbar(host.transform);
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0f;

            if (withLoopList) host.AddComponent<LoopList>();
        }

        private static Scrollbar BuildScrollbar(Transform parent)
        {
            var go = NewUI("Scrollbar", parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(ScrollbarWidth, 0);
            rt.anchoredPosition = Vector2.zero;
            ApplySprite(go.AddComponent<Image>(), SpriteScrollBg, true);

            var area = NewUI("Sliding Area", go.transform);
            Stretch((RectTransform)area.transform, 2, 2, 2, 2);

            var handle = NewUI("Handle", area.transform);
            var hrt = (RectTransform)handle.transform;
            Stretch(hrt);
            var himg = handle.AddComponent<Image>();
            ApplySprite(himg, SpriteScrollHandle, true);

            var scrollbar = go.AddComponent<Scrollbar>();
            scrollbar.handleRect = hrt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = himg;
            return scrollbar;
        }

        // =====================================================================
        // 列表项模板
        // =====================================================================

        // 模板容器：隐藏起来只作模板用；返回它是为了让保存流程能直接拿到（隐藏对象 GameObject.Find 找不到）
        private static GameObject BuildTemplates(Transform parent)
        {
            var holder = NewUI(TemplateHolder, parent);
            var hrt = (RectTransform)holder.transform;
            Anchor(hrt, new Vector2(0f, 1f), new Vector2(0f, 1f));
            hrt.sizeDelta = new Vector2(760, 200);
            hrt.anchoredPosition = Vector2.zero;
            holder.SetActive(false);

            BuildRoomItemTemplate(holder.transform);
            BuildMemberItemTemplate(holder.transform);
            return holder;
        }

        // 房间列表项：房间名（左，主文案）+ 房主 / 人数 / 状态（左，副文案），两行排布
        private static void BuildRoomItemTemplate(Transform holder)
        {
            GameObject item = CreateListItemBase("RoomListItem", holder, out Image img, out Button btn);

            // ListItemCustom 自带的左右文本改成我们的两行文案
            RenameIfMissing(item.transform, "TextLeft", "Name");
            RenameIfMissing(item.transform, "TextRight", "Info");

            TMP_Text name = EnsureChildText(item.transform, "Name", "房间名");
            TMP_Text info = EnsureChildText(item.transform, "Info", "房主：玩家   1/4   等待中");

            // 两行都靠左：Name 在上、Info 在下
            LayoutLeftText((RectTransform)name.transform, new Vector2(16, 14), new Vector2(680, 28), 24, ColName);
            LayoutLeftText((RectTransform)info.transform, new Vector2(16, -14), new Vector2(680, 24), 17, ColSub);

            ApplySprite(img, SpriteListItem, true);
            SetupSpriteSwap(btn, img);

            var view = item.GetComponent<RoomListItem>();
            if (view == null) view = item.AddComponent<RoomListItem>();

            var so = new SerializedObject(view);
            SetRef(so, "nameText", name);
            SetRef(so, "infoText", info);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 房间内玩家项：头像 + 昵称（左）+ 房主 / 准备状态（右）
        private static void BuildMemberItemTemplate(Transform holder)
        {
            GameObject item = CreateListItemBase("RoomPlayerItem", holder, out Image img, out Button btn);

            RenameIfMissing(item.transform, "TextLeft", "Name");
            RenameIfMissing(item.transform, "TextRight", "State");

            TMP_Text name = EnsureChildText(item.transform, "Name", "玩家昵称");
            TMP_Text state = EnsureChildText(item.transform, "State", "未准备");

            // 头像复用 headFrame 预制体（自带 Mask）
            Transform head = BuildHead(item.transform);

            LayoutLeftText((RectTransform)name.transform, new Vector2(68, 0), new Vector2(300, 28), 22, ColName);
            LayoutRightText((RectTransform)state.transform, new Vector2(-16, 0), new Vector2(160, 26), 18, ColSub);

            ApplySprite(img, SpriteListItem, true);
            SetupSpriteSwap(btn, img);

            // 房主视角：鼠标移到这一行时盖上「踢出该玩家」蒙层，点整行即踢人。
            // 蒙层 Image 的 raycastTarget 必须为 false，否则会挡住整行点击。
            GameObject kickTip = BuildKickTip(item.transform);

            // 悬停放大（通用组件）
            if (item.GetComponent<HoverScale>() == null) item.AddComponent<HoverScale>();

            var view = item.GetComponent<RoomPlayerItem>();
            if (view == null) view = item.AddComponent<RoomPlayerItem>();

            var so = new SerializedObject(view);
            SetRef(so, "headImage", head != null ? FindDeep(head, "headImage")?.GetComponent<Image>() : null);
            SetRef(so, "nameText", name);
            SetRef(so, "stateText", state);
            SetRef(so, "kickTip", kickTip);
            SetRef(so, "rowButton", btn);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 盖满整行的踢人蒙层：半透明红底 + 居中「踢出该玩家」
        private static GameObject BuildKickTip(Transform parent)
        {
            var tip = NewUI("KickTip", parent);
            Stretch((RectTransform)tip.transform);

            var bg = tip.AddComponent<Image>();
            bg.color = new Color(0.62f, 0.15f, 0.15f, 0.55f);
            bg.raycastTarget = false; // 不能挡住整行点击

            TMP_Text label = NewText("Text", tip.transform, "踢出该玩家", 22, TextAlignmentOptions.Center, Color.white);
            Stretch(label.rectTransform);

            tip.SetActive(false); // 默认隐藏，运行时由 RoomPlayerItem 悬停控制
            return tip;
        }

        // 列表项底座：复用 SharpUI ListItemCustom（Image + Button + 左右文本），解包并清掉缺失脚本
        private static GameObject CreateListItemBase(string name, Transform holder, out Image img, out Button btn)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharpListItemPath);
            GameObject item = null;

            if (prefab != null)
            {
                item = PrefabUtility.InstantiatePrefab(prefab, holder) as GameObject;
                if (item != null)
                {
                    PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                    StripMissingScripts(item);
                    item.name = name;

                    // 列表项自己排位，不需要 LayoutElement
                    var le = item.GetComponent<LayoutElement>();
                    if (le != null) Object.DestroyImmediate(le);
                    ApplyFont(item);
                }
            }

            if (item == null)
            {
                item = NewUI(name, holder);
                ((RectTransform)item.transform).sizeDelta = new Vector2(400, MemberItemHeight);
                item.AddComponent<Image>();
                item.AddComponent<Button>();
                NewText("Name", item.transform, "文本", 22, TextAlignmentOptions.Left, ColName);
            }

            img = item.GetComponent<Image>();
            if (img == null) img = item.AddComponent<Image>();
            img.raycastTarget = true;

            btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            return item;
        }

        private static void SetupSpriteSwap(Button btn, Image img)
        {
            if (btn == null || img == null) return;

            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            Sprite sel = LoadSprite(SpriteListItemSel);
            var state = btn.spriteState;
            state.highlightedSprite = sel;
            state.pressedSprite = sel;
            state.selectedSprite = sel;
            btn.spriteState = state;
        }

        // 头像：复用 headFrame.prefab（含 Mask），缩到 44 贴左侧
        private static Transform BuildHead(Transform parent)
        {
            GameObject head = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeadFramePrefabPath);
            if (prefab != null) head = InstantiateUnpacked(prefab, parent, "Head");

            if (head == null)
            {
                head = NewUI("Head", parent);
                ApplySprite(head.AddComponent<Image>(), TexDir + "round_frame.png", true);
            }

            var rt = (RectTransform)head.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(44, 44);
            rt.anchoredPosition = new Vector2(38, 0);

            var inner = FindDeep(head.transform, "headImage") as RectTransform;
            if (inner != null)
            {
                inner.anchorMin = Vector2.zero;
                inner.anchorMax = Vector2.one;
                inner.offsetMin = new Vector2(5, 5);
                inner.offsetMax = new Vector2(-5, -5);
            }

            return head.transform;
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

        // 小标题（贴在容器左上角，往下第 y 像素）
        private static void NewLabel(Transform parent, string name, string text, float y)
        {
            var label = NewText(name, parent, text, 26, TextAlignmentOptions.MidlineLeft, ColName);
            PlaceTopLeft(label.rectTransform, new Vector2(24, y), new Vector2(400, 40));
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
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharpButtonPath);
            if (prefab != null)
            {
                var inst = InstantiateUnpacked(prefab, parent, name);
                if (inst != null)
                {
                    var btn = inst.GetComponent<Button>();
                    if (btn != null)
                    {
                        ApplyFontAndLabel(inst, label);
                        return btn;
                    }
                    Object.DestroyImmediate(inst);
                }
            }

            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            ApplySprite(img, SpriteButton, true);
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var t = NewText("Label", go.transform, label, 22, TextAlignmentOptions.Center, Color.white);
            Stretch(t.rectTransform);
            return b;
        }

        private static Button NewIconButton(string name, Transform parent, string iconSpritePath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharpIconButtonPath);
            if (prefab != null)
            {
                var inst = InstantiateUnpacked(prefab, parent, name);
                if (inst != null)
                {
                    var icon = FindDeep(inst.transform, "IconImage");
                    var iconImg = icon != null ? icon.GetComponent<Image>() : null;
                    if (iconImg != null) ApplySprite(iconImg, iconSpritePath, false);

                    var btn = inst.GetComponent<Button>();
                    if (btn != null) return btn;
                    Object.DestroyImmediate(inst);
                }
            }

            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            ApplySprite(img, iconSpritePath, false);
            img.preserveAspect = true;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            return b;
        }

        private static TMP_InputField NewInput(string name, Transform parent, string placeholder)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharpInputPath);
            if (prefab != null)
            {
                var inst = InstantiateUnpacked(prefab, parent, name);
                if (inst != null)
                {
                    var input = inst.GetComponent<TMP_InputField>();
                    if (input != null)
                    {
                        RepairInput(input, placeholder);
                        return input;
                    }
                    Object.DestroyImmediate(inst);
                }
            }

            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            ApplySprite(img, SpriteInput, true);

            var inputFb = go.AddComponent<TMP_InputField>();
            inputFb.image = img;

            var textArea = NewUI("Text Area", go.transform);
            Stretch((RectTransform)textArea.transform, 10, 10, 5, 5);

            var text = NewText("Text", textArea.transform, "", 22, TextAlignmentOptions.Left, new Color(0.1f, 0.1f, 0.12f, 1f));
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var ph = NewText("Placeholder", textArea.transform, placeholder, 22, TextAlignmentOptions.Left,
                new Color(0.5f, 0.5f, 0.55f, 1f));
            Stretch(ph.rectTransform);
            ph.textWrappingMode = TextWrappingModes.NoWrap;

            inputFb.textViewport = (RectTransform)textArea.transform;
            inputFb.textComponent = text;
            inputFb.placeholder = ph;
            return inputFb;
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

        private static void StripMissingScripts(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
        }

        private static void ApplyFont(GameObject go)
        {
            if (_font == null) return;
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) t.font = _font;
        }

        private static void ApplyFontAndLabel(GameObject go, string label)
        {
            bool labeled = false;
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                if (_font != null) t.font = _font;
                if (labeled) continue;
                t.text = label;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                labeled = true;
            }
        }

        private static void RepairInput(TMP_InputField input, string placeholder)
        {
            foreach (var t in input.GetComponentsInChildren<TMP_Text>(true))
            {
                if (_font != null) t.font = _font;
            }

            if (input.textComponent == null)
                input.textComponent = FindDeep(input.transform, "Text")?.GetComponent<TMP_Text>();
            if (input.textViewport == null)
            {
                var area = FindDeep(input.transform, "Text Area");
                if (area != null) input.textViewport = (RectTransform)area;
            }
            if (input.placeholder == null)
                input.placeholder = FindDeep(input.transform, "Placeholder")?.GetComponent<TMP_Text>();

            if (input.textComponent != null) input.textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            if (input.placeholder is TMP_Text phText)
            {
                phText.text = placeholder;
                phText.textWrappingMode = TextWrappingModes.NoWrap;
                if (_font != null) phText.font = _font;
            }
        }

        private static void RenameChild(Transform parent, string from, string to)
        {
            var child = FindDeep(parent, from);
            if (child != null) child.name = to;
        }

        private static void RenameIfMissing(Transform item, string fromName, string toName)
        {
            if (FindDeep(item, toName) != null) return;
            RenameChild(item, fromName, toName);
        }

        // 目标文本不存在时补一个（SharpUI 预制体结构变化时的兜底）
        private static TMP_Text EnsureChildText(Transform parent, string nodeName, string fallbackText)
        {
            var existing = FindDeep(parent, nodeName)?.GetComponent<TMP_Text>();
            if (existing != null) return existing;
            return NewText(nodeName, parent, fallbackText, 22, TextAlignmentOptions.MidlineLeft, ColName);
        }

        private static void LayoutLeftText(RectTransform rt, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            if (rt == null) return;

            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            ApplyTextStyle(rt, TextAlignmentOptions.MidlineLeft, fontSize, color);
        }

        private static void LayoutRightText(RectTransform rt, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            if (rt == null) return;

            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            ApplyTextStyle(rt, TextAlignmentOptions.MidlineRight, fontSize, color);
        }

        private static void ApplyTextStyle(RectTransform rt, TextAlignmentOptions align, float fontSize, Color color)
        {
            var text = rt.GetComponent<TMP_Text>();
            if (text == null) return;

            text.alignment = align;
            text.fontSize = fontSize;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (_font != null) text.font = _font;
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
                Debug.LogWarning($"[RoomPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{path}");

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
            Debug.Log($"[RoomPanelBuilder] 已把 {path} 的 Mesh Type 改为 FullRect（九宫拉伸需要）。");
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

        // 贴容器左上角摆放（y 为负数往下）
        private static void PlaceTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void PlaceTopLeftInput(RectTransform rt, float y, float width, float height)
        {
            PlaceTopLeft(rt, new Vector2(24, y), new Vector2(width, height));
        }

        private static void PlaceBottomLeft(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void PlaceBottomRight(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void SetRef(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        private static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void TopAnchoredContent(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 0);
        }

        private static void AutoHeightContent(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 0);

            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);

            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Room Canvas");

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
