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
    // 一键生成「好友面板」(FriendPanel) 的完整 UI 层级，省去手动摆放。
    //
    // 菜单：Tools/UI/好友面板
    //   1. 在当前场景生成          —— 在临时 Canvas 下搭出整套层级（可直接预览/微调）
    //   2. 保存选中对象为 Prefab    —— 把生成的面板 + 好友项模板存成预制体
    //   3. 一键生成并保存为 Prefab  —— 1 + 2 连做
    //   4. 注册到 Addressables     —— 把 2 个预制体按 FriendPanel / FriendItem 注册
    //
    // 约定：
    //   · 视觉全部复用 SharpUI 资源：
    //       预制体 —— RectButton / IconButton / InputField / ListItemCustom
    //       贴图   —— info_box / dialog_header_background / rect_button / rect_input /
    //                 list_item_background(+_selected) / icon_close_light / scroll_*
    //   · 聊天消息气泡不在这里生成 —— 直接用现成的 ChatRootLeft / ChatRootRight（已在 Addressables 里），
    //     它们自带 LayoutGroup，气泡高度自适应。
    //   · 所有文本统一使用项目中文 TMP 字体（AlimamaDongFangDaKai）。
    //   · 生成的根节点挂 FriendPanel 组件（已继承 BasePanel），由 UIManager 统一打开。
    public static class FriendPanelBuilder
    {
        // ---------- 字体 ----------
        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        // ---------- SharpUI 贴图（全部以 Sprite 形式导入）----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpritePanelBg = TexDir + "info_box.png";                  // 面板 / 区域背景（九宫 13）
        private const string SpriteHeader = TexDir + "dialog_header_background.png";   // 标题栏
        private const string SpriteButton = TexDir + "rect_button.png";                // 矩形按钮（九宫 8）
        private const string SpriteInput = TexDir + "rect_input.png";                  // 输入框
        private const string SpriteListItem = TexDir + "list_item_background.png";     // 好友项常态
        private const string SpriteListItemSel = TexDir + "list_item_background_selected.png"; // 好友项高亮
        private const string SpriteClose = TexDir + "icon_close_light.png";            // 关闭图标
        private const string SpriteScrollBg = TexDir + "scroll_background.png";        // 滚动条底槽（九宫 5）
        private const string SpriteScrollHandle = TexDir + "scroll_handle.png";        // 滚动条把手

        // ---------- SharpUI 预制体 ----------
        private const string SharpPrefabDir = "Assets/Resources/SharpUI/Prefabs/UI/";
        private const string SharpButtonPath = SharpPrefabDir + "Button/RectButton.prefab";
        private const string SharpIconButtonPath = SharpPrefabDir + "Button/IconButton.prefab";
        private const string SharpInputPath = SharpPrefabDir + "Input/InputField.prefab";
        private const string SharpListItemPath = ""; // 纯色自绘，不再解包 SharpUI

        // ---------- 项目自带 UIItem 预制体 ----------
        private const string HeadFramePrefabPath = "Assets/Res/UI/UIItem/headFrame.prefab"; // 头像框（含 Mask，裁切内部头像）

        // ---------- 输出 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/FriendPanel.prefab";
        private const string FriendItemPrefabPath = "Assets/Res/UI/UIItem/FriendItem.prefab";

        // ---------- 命名（与 FriendPanel 绑定逻辑对应，勿随意改）----------
        private const string RootName = "FriendPanel";
        private const string CanvasName = "FriendPanelTempCanvas";
        private const string TemplateHolder = "FriendPanelTemplates";

        // 好友项固定高度（LoopList 虚拟滚动需要固定行高，运行时按预制体实际高度读，见 FriendPanel.LoadFriendItemPrefab）
        private const float FriendItemHeight = 88f;
        private const float FriendItemWidth = 360f;

        private const float ScrollbarWidth = 16f;

        // ---------- 全屏版式（1920×1080 参考）----------
        private const float TopBarHeight = 110f;   // 顶栏（标题 + 加好友 + 关闭）
        private const float SidePadding = 60f;     // 左右留白
        private const float RailWidth = 380f;      // 左侧好友栏宽度
        private const float RailGap = 44f;         // 栏间距（中间放分隔线）
        private const float ChatInputHeight = 72f; // 聊天底部输入区高度

        // ---------- 配色 ----------
        private static readonly Color ColMask = new Color(0f, 0f, 0f, 0.88f);
        private static readonly Color ColName = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color ColStatus = new Color(0.42f, 0.43f, 0.5f, 1f);
        private static readonly Color ColAccent = new Color(0.85f, 1f, 0.24f, 1f);
        private static readonly Color ColLine = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color ColButton = new Color(0.09f, 0.09f, 0.115f, 1f);
        private static readonly Color ColMuted = new Color(0.38f, 0.39f, 0.46f, 1f);

        private static TMP_FontAsset _font;
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly HashSet<string> _fullRectFixed = new();

        // =====================================================================
        // 菜单入口
        // =====================================================================

        /// <summary>在当前场景的临时 Canvas 下生成整套好友面板层级</summary>
        [MenuItem("Tools/UI/好友面板/1. 在当前场景生成", false, 10)]
        public static void BuildInScene()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);
            BuildTemplates(canvas.transform);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log("[FriendPanelBuilder] 已生成好友面板层级，调整满意后用菜单 " +
                      "「Tools/UI/好友面板/2. 保存选中对象为 Prefab」存成预制体。");
        }

        /// <summary>把当前选中的面板保存为预制体（同时保存两个列表项模板）</summary>
        [MenuItem("Tools/UI/好友面板/2. 保存选中对象为 Prefab", false, 11)]
        public static void SaveSelectionAsPrefab()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null || root.transform.Find("Panel") == null)
            {
                EditorUtility.DisplayDialog("保存失败",
                    "请先在层级面板选中生成的好友面板根节点（应包含 Panel 子节点）。", "好");
                return;
            }

            SavePanelAndTemplates(root, null);
        }

        /// <summary>生成并直接保存为预制体</summary>
        [MenuItem("Tools/UI/好友面板/3. 一键生成并保存为 Prefab", false, 12)]
        public static void BuildAndSave()
        {
            GameObject canvas = EnsureCanvas();
            GameObject root = BuildHierarchy(canvas.transform);
            GameObject templates = BuildTemplates(canvas.transform);
            SavePanelAndTemplates(root, templates);
        }

        /// <summary>把生成的预制体按 UIManager 需要的地址注册到 Addressables（可选，但面板靠它加载）</summary>
        [MenuItem("Tools/UI/好友面板/4. 注册到 Addressables", false, 13)]
        public static void RegisterAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("注册失败",
                    "当前工程还没有 Addressables 设置（Window/Asset Management/Addressables/Groups 先创建）。", "好");
                return;
            }

            // 优先塞进名字里带 UI 的组，否则用默认组
            AddressableAssetGroup group = null;
            foreach (var g in settings.groups)
            {
                if (g == null || string.IsNullOrEmpty(g.Name)) continue;
                if (g.Name.ToUpperInvariant().Contains("UI")) { group = g; break; }
            }
            if (group == null) group = settings.DefaultGroup;

            RegisterEntry(settings, group, PanelPrefabPath, "FriendPanel");
            RegisterEntry(settings, group, FriendItemPrefabPath, "FriendItem");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FriendPanelBuilder] 已注册到 Addressables 组「{group.Name}」：FriendPanel / FriendItem" +
                      "（消息气泡的 ChatRootLeft / ChatRootRight 早已在组里，无需重复注册）");
        }

        private static void RegisterEntry(AddressableAssetSettings settings, AddressableAssetGroup group,
            string assetPath, string address)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"[FriendPanelBuilder] 未找到 {assetPath}，先执行菜单 3 生成预制体。");
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
            EnsureFolder(FriendItemPrefabPath);

            // 面板本体（好友项模板挂在 Canvas 下、与面板平级，不会被一起打包）
            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);

            // 好友项模板（消息气泡直接用现成的 ChatRootLeft / ChatRootRight，不需要另存）
            // 注意：模板容器是隐藏的（SetActive(false)），GameObject.Find 找不到未激活对象，
            //       只能用 Transform.Find（能命中未激活子节点）或直接用传进来的引用。
            GameObject holder = templates;
            if (holder == null && root.transform.parent != null)
                holder = root.transform.parent.Find(TemplateHolder)?.gameObject;

            if (holder != null)
            {
                var fi = holder.transform.Find("FriendItem");
                if (fi != null)
                {
                    // 保存前确保模板自身是激活的，否则预制体根节点会带着 inactive 存下去
                    bool wasActive = fi.gameObject.activeSelf;
                    fi.gameObject.SetActive(true);
                    PrefabUtility.SaveAsPrefabAsset(fi.gameObject, FriendItemPrefabPath);
                    fi.gameObject.SetActive(wasActive);
                }
                else
                    Debug.LogWarning("[FriendPanelBuilder] 模板容器里没有 FriendItem，未生成 FriendItem.prefab。");
            }
            else
                Debug.LogWarning("[FriendPanelBuilder] 未找到好友项模板容器（FriendPanelTemplates），" +
                                 "请先用菜单 1 生成层级。");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FriendPanelBuilder] 预制体已保存：\n  {PanelPrefabPath}\n  {FriendItemPrefabPath}\n" +
                      "接下来执行「Tools/UI/好友面板/4. 注册到 Addressables」。");
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
                Debug.LogWarning($"[FriendPanelBuilder] 未找到字体资源：{FontPath}，文本将回退默认字体（中文可能显示为方块）。");

            // ---- 根节点（全屏遮罩 + 逻辑组件）----
            var root = NewUI(RootName, canvas);
            Undo.RegisterCreatedObjectUndo(root, "Create FriendPanel");
            Stretch((RectTransform)root.transform);
            root.AddComponent<Image>().color = ColMask;
            root.AddComponent<FriendPanel>();

            // ---- Panel 直接铺满：不再是居中的卡片，改成全屏分区 ----
            var panel = NewUI("Panel", root.transform);
            Stretch((RectTransform)panel.transform);

            BuildHeader(panel.transform);

            // 主体：顶栏以下，左右留白
            var body = NewUI("Body", panel.transform);
            Stretch((RectTransform)body.transform, SidePadding, SidePadding, 40f, TopBarHeight + 20f);

            BuildFriendList(body.transform);
            BuildChatArea(body.transform);

            // 左右之间的分隔竖线
            var divider = NewUI("Divider", body.transform);
            var drt = (RectTransform)divider.transform;
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(0f, 1f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(1f, 0f);
            drt.anchoredPosition = new Vector2(RailWidth + RailGap * 0.5f, 0f);
            var dimg = divider.AddComponent<Image>();
            dimg.color = ColLine;
            dimg.raycastTarget = false;

            return root;
        }

        // 顶栏：左边「好友 / FRIENDS」大字，右边加好友输入 + 按钮 + 关闭
        private static void BuildHeader(Transform parent)
        {
            var header = NewUI("Header", parent);
            var hrt = (RectTransform)header.transform;
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(0f, TopBarHeight);
            hrt.anchoredPosition = Vector2.zero;

            // 底部一条细线，代替原来那块布质标题栏
            AddLine("Line", header.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), 1f);

            var title = NewText("Title", header.transform, "好友", 54, TextAlignmentOptions.MidlineLeft, ColName);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(300f, 64f), new Vector2(SidePadding, -14f));

            var sub = NewText("Sub", header.transform, "FRIENDS", 16, TextAlignmentOptions.MidlineLeft, ColAccent);
            Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(300f, 22f), new Vector2(SidePadding + 4f, -78f));

            // 加好友：输入框 + 按钮，靠右
            var addInput = NewInput("AddFriendInput", header.transform, "输入对方角色ID");
            Place(addInput.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(300f, 46f), new Vector2(-280f, 0f));

            var addBtn = NewButton("AddFriendBtn", header.transform, "加好友");
            Place(addBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(120f, 46f), new Vector2(-148f, 0f));

            var closeBtn = NewIconButton("CloseBtn", header.transform, SpriteClose);
            Place(closeBtn.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(44f, 44f), new Vector2(-50f, 0f));
        }

        // 左栏：好友列表（无卡片底，直接贴在面板上）
        private static void BuildFriendList(Transform parent)
        {
            var rail = NewUI("FriendListBg", parent);
            var rrt = (RectTransform)rail.transform;
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(0f, 1f);
            rrt.pivot = new Vector2(0f, 0.5f);
            rrt.sizeDelta = new Vector2(RailWidth, 0f);
            rrt.anchoredPosition = Vector2.zero;

            BuildScrollArea("FriendScroll", rail.transform, 0, 0, 0, 0, true);
        }

        // 右区：聊天标题 + 消息列表 + 底部输入
        private static void BuildChatArea(Transform parent)
        {
            var area = NewUI("ChatBg", parent);
            var art = (RectTransform)area.transform;
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(1f, 1f);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.offsetMin = new Vector2(RailWidth + RailGap, 0f);
            art.offsetMax = Vector2.zero;

            var chatTitle = NewText("ChatTitle", area.transform, "选择好友开始聊天", 30,
                TextAlignmentOptions.MidlineLeft, ColName);
            var ctrt = chatTitle.rectTransform;
            ctrt.anchorMin = new Vector2(0f, 1f);
            ctrt.anchorMax = new Vector2(1f, 1f);
            ctrt.pivot = new Vector2(0.5f, 1f);
            ctrt.offsetMin = new Vector2(4f, -56f);
            ctrt.offsetMax = Vector2.zero;

            AddLine("TitleLine", area.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0f), 1f, -58f);

            // 消息区：不生成列表项，直接用现成的 ChatRootLeft / ChatRootRight 气泡（高度自适应）
            BuildScrollArea("MsgScroll", area.transform, 0, 0, ChatInputHeight, 68, false);

            var msgInput = NewInput("MsgInput", area.transform, "输入消息");
            var mirt = msgInput.GetComponent<RectTransform>();
            mirt.anchorMin = new Vector2(0f, 0f);
            mirt.anchorMax = new Vector2(1f, 0f);
            mirt.pivot = new Vector2(0.5f, 0.5f);
            mirt.offsetMin = new Vector2(0f, 10f);
            mirt.offsetMax = new Vector2(-148f, 62f);

            var sendBtn = NewButton("SendBtn", area.transform, "发送");
            Place(sendBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0.5f),
                new Vector2(128f, 52f), new Vector2(0f, 36f));
        }

        // 生成一个「ScrollRect + Viewport/Content + Vertical Scrollbar」的滚动区
        //   withLoopList = true  —— Content 顶部锚定、挂 LoopList（固定项高，好友列表用）
        //   withLoopList = false —— Content 挂 VerticalLayoutGroup + ContentSizeFitter（子项自适应高度，聊天消息用）
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

            var track = go.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.03f);

            var area = NewUI("Sliding Area", go.transform);
            Stretch((RectTransform)area.transform, 3, 3, 3, 3);

            var handle = NewUI("Handle", area.transform);
            var hrt = (RectTransform)handle.transform;
            Stretch(hrt);
            var himg = handle.AddComponent<Image>();
            himg.color = new Color(1f, 1f, 1f, 0.16f);

            var scrollbar = go.AddComponent<Scrollbar>();
            scrollbar.handleRect = hrt;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = himg;
            return scrollbar;
        }

        // 模板容器：隐藏起来只作模板用；返回它是为了让保存流程能直接拿到（隐藏对象 GameObject.Find 找不到）
        private static GameObject BuildTemplates(Transform parent)
        {
            var holder = NewUI(TemplateHolder, parent);
            var hrt = (RectTransform)holder.transform;
            Anchor(hrt, new Vector2(0f, 1f), new Vector2(0f, 1f));
            hrt.sizeDelta = new Vector2(600, 200);
            hrt.anchoredPosition = Vector2.zero;
            holder.SetActive(false); // 仅作模板，不进可视区

            BuildFriendItemTemplate(holder.transform);
            return holder;
        }

        // 好友项模板：复用 SharpUI ListItemCustom（Image + Button + 左右文本），换成好友项贴图
        private static void BuildFriendItemTemplate(Transform holder)
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
                    item.name = "FriendItem";
                    // SharpUI 的 ListItem 原高偏小，这里统一成列表行高（LoopList 按这个高度排位）
                    ((RectTransform)item.transform).sizeDelta = new Vector2(FriendItemWidth, FriendItemHeight);
                    RenameChild(item.transform, "TextLeft", "Name");
                    RenameChild(item.transform, "TextRight", "Status");

                    // 列表项自己排位，不需要 LayoutElement
                    var le = item.GetComponent<LayoutElement>();
                    if (le != null) Object.DestroyImmediate(le);
                    ApplyFont(item);
                }
            }

            if (item == null)
            {
                // 回退：自绘
                item = NewUI("FriendItem", holder);
                var irt = (RectTransform)item.transform;
                irt.sizeDelta = new Vector2(FriendItemWidth, FriendItemHeight);
                item.AddComponent<Image>();
                item.AddComponent<Button>();
                NewText("Name", item.transform, "昵称", 22, TextAlignmentOptions.Left, ColName);
                NewText("Status", item.transform, "离线", 18, TextAlignmentOptions.Left, ColStatus);
            }

            var img = item.GetComponent<Image>();
            if (img == null) img = item.AddComponent<Image>();
            // 纯色行底：极淡的一层白，hover / 选中靠覆盖层和 HoverScale
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.color = new Color(1f, 1f, 1f, 0.035f);
            img.raycastTarget = true;

            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            // 选中覆盖层（“当前聊天对象”高亮）：默认隐藏，由 FriendItem.SetSelected 控制。
            // 放到同级最底层，免得盖住头像与文字。
            var overlay = NewUI("Selected", item.transform);
            Stretch((RectTransform)overlay.transform);
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.sprite = null;
            overlayImg.color = new Color(0.85f, 1f, 0.24f, 0.10f);
            overlayImg.raycastTarget = false;
            overlay.transform.SetSiblingIndex(0);
            overlay.SetActive(false);

            // 头像：复用项目 headFrame 预制体（自带 Mask，能把内部头像裁进框里）
            Transform head = BuildFriendHead(item.transform);

            // 文本排布：头像右侧，昵称在上、状态在下
            LayoutChildText(item.transform, "Name", new Vector2(74, 16), new Vector2(240, 32), 22, ColName);
            LayoutChildText(item.transform, "Status", new Vector2(74, -18), new Vector2(200, 22), 17, ColStatus);

            // 昵称改为左右自适应：左侧开头像、右侧留白，长昵称不会被裁。
            // 申请形态下「同意 / 拒绝」按钮浮在右侧，按钮自带不透明底图，会盖住溢出的尾部。
            var nameRt = FindDeep(item.transform, "Name") as RectTransform;
            if (nameRt != null)
            {
                nameRt.anchorMin = new Vector2(0f, 0.5f);
                nameRt.anchorMax = new Vector2(1f, 0.5f);
                nameRt.pivot = new Vector2(0f, 0.5f);
                nameRt.offsetMin = new Vector2(74f, 2f);
                nameRt.offsetMax = new Vector2(-12f, 32f);
            }

            // 好友申请形态：右侧「同意 / 拒绝」两个按钮（复用 SharpUI RectButton），
            // 默认整组隐藏 —— 由 FriendItem.SetData 按 IsRequest 控制显隐。
            var applyBtns = NewUI("ApplyBtns", item.transform);
            var abrt = (RectTransform)applyBtns.transform;
            abrt.anchorMin = abrt.anchorMax = new Vector2(1f, 0.5f);
            abrt.pivot = new Vector2(1f, 0.5f);
            abrt.sizeDelta = new Vector2(180, 40);
            abrt.anchoredPosition = new Vector2(-10, 0);

            Button agreeBtn = NewButton("AgreeBtn", applyBtns.transform, "同意");
            PlaceRightAligned((RectTransform)agreeBtn.transform, new Vector2(-86, 0), new Vector2(80, 36));
            SetLabelSize(agreeBtn, 18);

            Button rejectBtn = NewButton("RejectBtn", applyBtns.transform, "拒绝");
            PlaceRightAligned((RectTransform)rejectBtn.transform, new Vector2(0, 0), new Vector2(80, 36));
            SetLabelSize(rejectBtn, 18);

            applyBtns.SetActive(false);

            // 挂展示脚本并接好引用。FriendItem 的字段都是 private [SerializeField]，
            // 编辑器脚本只能走 SerializedObject 赋值（字段名要和 FriendItem.cs 完全一致）。
            var view = item.GetComponent<FriendItem>();
            if (view == null) view = item.AddComponent<FriendItem>();

            var so = new SerializedObject(view);
            SetRef(so, "headImage", head != null ? FindDeep(head, "headImage")?.GetComponent<Image>() : null);
            SetRef(so, "nameText", FindDeep(item.transform, "Name")?.GetComponent<TMP_Text>());
            SetRef(so, "statusText", FindDeep(item.transform, "Status")?.GetComponent<TMP_Text>());
            SetRef(so, "selectedMark", overlay);
            SetRef(so, "applyButtons", applyBtns);
            SetRef(so, "agreeBtn", agreeBtn);
            SetRef(so, "rejectBtn", rejectBtn);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 好友头像：优先复用 headFrame.prefab（80x80，内部 headImage 是占位图），缩到 48 贴左侧
        private static Transform BuildFriendHead(Transform parent)
        {
            GameObject head = null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeadFramePrefabPath);
            if (prefab != null) head = InstantiateUnpacked(prefab, parent, "Head");

            if (head == null)
            {
                // 回退：自绘一个圆角框
                head = NewUI("Head", parent);
                ApplySprite(head.AddComponent<Image>(), TexDir + "round_frame.png", true);
            }

            var rt = (RectTransform)head.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(48, 48);
            rt.anchoredPosition = new Vector2(40, 0);

            // headFrame 内部头像节点是固定 50x50，缩到 48 的框里要重新铺一次
            var inner = FindDeep(head.transform, "headImage") as RectTransform;
            if (inner != null)
            {
                inner.anchorMin = Vector2.zero;
                inner.anchorMax = Vector2.one;
                inner.offsetMin = new Vector2(6, 6);
                inner.offsetMax = new Vector2(-6, -6);
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

        // 纯色按钮：深色底 + 左侧 4px 强调条 + 居中文字（不依赖任何贴图）
        private static Button NewButton(string name, Transform parent, string label)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = ColButton;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // ColorTint 是乘法（底色 × 状态色），深色底乘什么都是深色 → 用 ButtonHoverTint 直接改颜色：
            // 悬停时整块变荧光黄、文字与左侧色条转近黑
            btn.transition = Selectable.Transition.None;

            var tint = go.AddComponent<ButtonHoverTint>();
            tint.SetColors(UIBuilderKit.Accent, UIBuilderKit.OnAccent, 0.1f);

            var bar = NewUI("Bar", go.transform);
            var brt = (RectTransform)bar.transform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0f, 0.5f);
            brt.sizeDelta = new Vector2(4f, 0f);
            brt.anchoredPosition = Vector2.zero;
            var barImg = bar.AddComponent<Image>();
            barImg.color = ColAccent;
            barImg.raycastTarget = false;

            var t = NewText("Label", go.transform, label, 22, TextAlignmentOptions.Center, ColName);
            Stretch(t.rectTransform, 10f, 8f, 0f, 0f);
            return btn;
        }

        // 纯色图标按钮：半透明方块 + 一个文字符号（×），不用图标贴图
        private static Button NewIconButton(string name, Transform parent, string iconSpritePath)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.07f);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // ColorTint 是乘法（底色 × 状态色），深色底乘什么都是深色 → 用 ButtonHoverTint 直接改颜色：
            // 悬停时整块变荧光黄、文字与左侧色条转近黑
            btn.transition = Selectable.Transition.None;

            var tint = go.AddComponent<ButtonHoverTint>();
            tint.SetColors(UIBuilderKit.Accent, UIBuilderKit.OnAccent, 0.1f);

            var t = NewText("Icon", go.transform, "×", 26, TextAlignmentOptions.Center, ColName);
            Stretch(t.rectTransform);
            return btn;
        }

        // 纯色输入框：半透明底 + 底部一条 2px 强调线
        private static TMP_InputField NewInput(string name, Transform parent, string placeholder)
        {
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.045f);

            var input = go.AddComponent<TMP_InputField>();
            input.image = img;
            input.targetGraphic = img;

            var textArea = NewUI("Text Area", go.transform);
            Stretch((RectTransform)textArea.transform, 12f, 12f, 4f, 6f);

            var text = NewText("Text", textArea.transform, "", 22, TextAlignmentOptions.Left, ColName);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var ph = NewText("Placeholder", textArea.transform, placeholder, 22, TextAlignmentOptions.Left, ColMuted);
            Stretch(ph.rectTransform);
            ph.textWrappingMode = TextWrappingModes.NoWrap;

            input.textViewport = (RectTransform)textArea.transform;
            input.textComponent = text;
            input.placeholder = ph;

            var line = NewUI("Underline", go.transform);
            var lrt = (RectTransform)line.transform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.sizeDelta = new Vector2(0f, 2f);
            lrt.anchoredPosition = Vector2.zero;
            var limg = line.AddComponent<Image>();
            limg.color = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.45f);
            limg.raycastTarget = false;

            return input;
        }

        // =====================================================================
        // 工具
        // =====================================================================

        // 实例化 SharpUI 预制体 → 解包 → 清理缺失脚本 → 改名
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

        private static void ApplyFont(GameObject go)
        {
            if (_font == null) return;
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true)) t.font = _font;
        }

        // 把对象里的 TMP 文本统一成本项目字体，并把第一个文本设为按钮文案
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

        // 修复从预制体实例化出来的输入框：统一字体、补全 textViewport/textComponent、设置占位文本
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

        private static void LayoutChildText(Transform parent, string childName, Vector2 pos, Vector2 size,
            float fontSize, Color color)
        {
            var rt = FindDeep(parent, childName) as RectTransform;
            if (rt == null) return;

            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var text = rt.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.fontSize = fontSize;
                text.color = color;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                if (_font != null) text.font = _font;
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
                Debug.LogWarning($"[FriendPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{path}");

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

            // 九宫渲染要求 Sprite 的 mesh 为 FullRect（SharpUI 贴图默认是 Tight，会报警且九宫失效）
            if (sliced) EnsureFullRect(spritePath);

            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            img.color = Color.white;
        }

        // 把以 Single Sprite 导入、但 mesh 为 Tight 的贴图改成 FullRect（只处理一次）
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
            Debug.Log($"[FriendPanelBuilder] 已把 {path} 的 Mesh Type 改为 FullRect（九宫拉伸需要）。");
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

        // 贴右对齐摆放（列表项右侧的小按钮用）
        private static void PlaceRightAligned(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // 给 private [SerializeField] 字段赋引用（编辑器脚本访问不到私有字段）
        private static void SetRef(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        // 调整按钮文字大小（RectButton 默认字号偏大，塞进小按钮会溢出）
        private static void SetLabelSize(Button btn, float size)
        {
            if (btn == null) return;
            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true)) t.fontSize = size;
        }

        private static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        // 按锚点 + 轴心 + 尺寸 + 位置摆放（比 Anchor 之后手写 anchoredPosition 少出错）
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // 一条 1px 装饰线（不用九宫图，避免被拉成一条缝）
        private static void AddLine(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, float thickness, float y = 0f)
        {
            var go = NewUI(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(0f, thickness);
            rt.anchoredPosition = new Vector2(0f, y);

            var img = go.AddComponent<Image>();
            img.color = ColLine;
            img.raycastTarget = false;
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

        // 聊天消息的 Content：竖直排布 + 高度随子项自适应（ChatRootLeft / ChatRootRight 自带 LayoutElement）
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
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Friend Canvas");

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
