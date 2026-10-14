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
        private const string SharpListItemPath = SharpPrefabDir + "List/ListItemCustom.prefab";

        // ---------- 项目自带 UIItem 预制体 ----------
        private const string HeadFramePrefabPath = "Assets/Res/UI/UIItem/headFrame.prefab"; // 头像框（含 Mask，裁切内部头像）

        // ---------- 输出 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/FriendPanel.prefab";
        private const string FriendItemPrefabPath = "Assets/Res/UI/UIItem/FriendItem.prefab";

        // ---------- 命名（与 FriendPanel 绑定逻辑对应，勿随意改）----------
        private const string RootName = "FriendPanel";
        private const string CanvasName = "FriendPanelTempCanvas";
        private const string TemplateHolder = "FriendPanelTemplates";

        // 好友项固定高度（与 FriendPanel 里 SetItemHeight 保持一致）
        private const float FriendItemHeight = 64f;

        private const float ScrollbarWidth = 16f;

        // ---------- 配色（仅用于遮罩，其余走贴图）----------
        private static readonly Color ColMask = new Color(0f, 0f, 0f, 0.62f);
        private static readonly Color ColName = new Color(0.92f, 0.94f, 0.98f, 1f);
        private static readonly Color ColStatus = new Color(0.6f, 0.62f, 0.68f, 1f);

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
            root.AddComponent<Image>().color = ColMask; // 遮罩保持纯色（半透明黑）
            root.AddComponent<FriendPanel>();

            // ---- 主面板容器 ----
            var panel = NewUI("Panel", root.transform);
            var prt = (RectTransform)panel.transform;
            Center(prt, new Vector2(1400, 800));
            ApplySprite(panel.AddComponent<Image>(), SpritePanelBg, true);

            BuildHeader(panel.transform);

            // 关闭按钮（复用 SharpUI IconButton + 关闭图标）
            var closeBtn = NewIconButton("CloseBtn", panel.transform, SpriteClose);
            var crt = closeBtn.GetComponent<RectTransform>();
            Anchor(crt, new Vector2(1f, 1f), new Vector2(1f, 1f));
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(46, 46);
            crt.anchoredPosition = new Vector2(-42, -38);

            // 添加好友：输入框 + 按钮（复用 SharpUI InputField / RectButton）
            var addInput = NewInput("AddFriendInput", panel.transform, "输入对方角色ID");
            var airt = addInput.GetComponent<RectTransform>();
            Anchor(airt, new Vector2(0f, 1f), new Vector2(0f, 1f));
            airt.pivot = new Vector2(0f, 0.5f);
            airt.sizeDelta = new Vector2(300, 44);
            airt.anchoredPosition = new Vector2(24, -108);

            var addBtn = NewButton("AddFriendBtn", panel.transform, "加好友");
            var abrt = addBtn.GetComponent<RectTransform>();
            Anchor(abrt, new Vector2(0f, 1f), new Vector2(0f, 1f));
            abrt.pivot = new Vector2(0f, 0.5f);
            abrt.sizeDelta = new Vector2(112, 44);
            abrt.anchoredPosition = new Vector2(336, -108);

            // 左侧好友列表
            BuildFriendList(panel.transform);

            // 右侧聊天区
            BuildChatArea(panel.transform);

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

            var title = NewText("Title", header.transform, "好友", 34, TextAlignmentOptions.Center, ColName);
            Stretch(title.rectTransform);
        }

        private static void BuildFriendList(Transform parent)
        {
            var bg = NewUI("FriendListBg", parent);
            var brt = (RectTransform)bg.transform;
            Anchor(brt, new Vector2(0f, 0f), new Vector2(0f, 1f));
            brt.offsetMin = new Vector2(20, 20);
            brt.offsetMax = new Vector2(-1000, -152);
            ApplySprite(bg.AddComponent<Image>(), SpritePanelBg, true);

            // ScrollRect + LoopList 直接被 FriendPanel 按节点名 "FriendScroll" 找到，挂在这个节点上
            BuildScrollArea("FriendScroll", bg.transform, 8, 8, 8, 8, true);
        }

        private static void BuildChatArea(Transform parent)
        {
            var bg = NewUI("ChatBg", parent);
            var brt = (RectTransform)bg.transform;
            Anchor(brt, new Vector2(1f, 0f), new Vector2(1f, 1f));
            brt.offsetMin = new Vector2(-980, 20);
            brt.offsetMax = new Vector2(-20, -152);
            ApplySprite(bg.AddComponent<Image>(), SpritePanelBg, true);

            var chatTitle = NewText("ChatTitle", bg.transform, "选择好友开始聊天", 28, TextAlignmentOptions.Center, ColName);
            var ctrt = chatTitle.rectTransform;
            Anchor(ctrt, new Vector2(0f, 1f), new Vector2(1f, 1f));
            ctrt.pivot = new Vector2(0.5f, 1f);
            ctrt.sizeDelta = new Vector2(0, 48);
            ctrt.anchoredPosition = Vector2.zero;

            // 消息区：不生成列表项，直接用现成的 ChatRootLeft / ChatRootRight 气泡（高度自适应）
            BuildScrollArea("MsgScroll", bg.transform, 16, 16, 76, 52, false);

            var msgInput = NewInput("MsgInput", bg.transform, "输入消息");
            var mirt = msgInput.GetComponent<RectTransform>();
            Anchor(mirt, new Vector2(0f, 0f), new Vector2(0f, 0f));
            mirt.pivot = new Vector2(0f, 0.5f);
            mirt.sizeDelta = new Vector2(760, 48);
            mirt.anchoredPosition = new Vector2(20, 40);

            var sendBtn = NewButton("SendBtn", bg.transform, "发送");
            var sbrt = sendBtn.GetComponent<RectTransform>();
            Anchor(sbrt, new Vector2(1f, 0f), new Vector2(1f, 0f));
            sbrt.pivot = new Vector2(1f, 0.5f);
            sbrt.sizeDelta = new Vector2(112, 48);
            sbrt.anchoredPosition = new Vector2(-20, 40);
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
                irt.sizeDelta = new Vector2(400, FriendItemHeight);
                item.AddComponent<Image>();
                item.AddComponent<Button>();
                NewText("Name", item.transform, "昵称", 22, TextAlignmentOptions.Left, ColName);
                NewText("Status", item.transform, "离线", 18, TextAlignmentOptions.Left, ColStatus);
            }

            var img = item.GetComponent<Image>();
            if (img == null) img = item.AddComponent<Image>();
            ApplySprite(img, SpriteListItem, true);
            img.raycastTarget = true;

            // 按钮：常态 / 高亮 用两张列表项贴图做 SpriteSwap
            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            Sprite sel = LoadSprite(SpriteListItemSel);
            var state = btn.spriteState;
            state.highlightedSprite = sel;
            state.pressedSprite = sel;
            state.selectedSprite = sel;
            btn.spriteState = state;

            // 选中覆盖层（“当前聊天对象”高亮）：默认隐藏，由 FriendItem.SetSelected 控制。
            // 放到同级最底层，免得盖住头像与文字。
            var overlay = NewUI("Selected", item.transform);
            Stretch((RectTransform)overlay.transform);
            var overlayImg = overlay.AddComponent<Image>();
            ApplySprite(overlayImg, SpriteListItemSel, true);
            overlayImg.raycastTarget = false;
            overlay.transform.SetSiblingIndex(0);
            overlay.SetActive(false);

            // 头像：复用项目 headFrame 预制体（自带 Mask，能把内部头像裁进框里）
            Transform head = BuildFriendHead(item.transform);

            // 文本排布：头像右侧，昵称在上、状态在下
            LayoutChildText(item.transform, "Name", new Vector2(74, 12), new Vector2(240, 30), 22, ColName);
            LayoutChildText(item.transform, "Status", new Vector2(74, -14), new Vector2(200, 22), 17, ColStatus);

            // 昵称改为左右自适应：左侧开头像、右侧留白，长昵称不会被裁。
            // 申请形态下「同意 / 拒绝」按钮浮在右侧，按钮自带不透明底图，会盖住溢出的尾部。
            var nameRt = FindDeep(item.transform, "Name") as RectTransform;
            if (nameRt != null)
            {
                nameRt.anchorMin = new Vector2(0f, 0.5f);
                nameRt.anchorMax = new Vector2(1f, 0.5f);
                nameRt.pivot = new Vector2(0f, 0.5f);
                nameRt.offsetMin = new Vector2(74f, -3f);
                nameRt.offsetMax = new Vector2(-12f, 27f);
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

        // 复用 SharpUI RectButton.prefab（自带 rect_button 贴图）
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

            // 回退：自绘（用按钮贴图）
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            ApplySprite(img, SpriteButton, true);
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var t = NewText("Label", go.transform, label, 22, TextAlignmentOptions.Center, Color.white);
            Stretch(t.rectTransform);
            return b;
        }

        // 复用 SharpUI IconButton.prefab（自带方形按钮贴图），图标换成指定贴图
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

            // 回退：自绘
            var go = NewUI(name, parent);
            var img = go.AddComponent<Image>();
            ApplySprite(img, iconSpritePath, false);
            img.preserveAspect = true;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            return b;
        }

        // 复用 SharpUI InputField.prefab（自带 rect_input 贴图）
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

            // 回退：自绘（用输入框贴图）
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
