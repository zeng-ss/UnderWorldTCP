using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using HotUpdate.Data;
using HotUpdate.UI;
using HotUpdate.UI.UIPanel;

namespace ProjectEditor
{
    // 一键把「游戏内 HUD」(PlayerProPanel) 的骨架摆进预制体。可反复执行（幂等）。
    //
    // 自从快捷格改为「运行时按 ShortcutBtnConfig 生成」后，预制体里不再摆具体格子，
    // 这个生成器只负责四件事：
    //   1. InfoBg            —— 左上角头像/血条后面的半透明底板（静态）。
    //   2. shortcutBtnParent —— 底部快捷栏容器：底图 + HorizontalLayoutGroup + ContentSizeFitter，
    //                           运行时 PlayerProPanel.Awake 按 Config 往里实例化 HUDSlotItem，
    //                           格子数量变了会自动重排、底板自动变宽。
    //   3. 把 shortcutBtnConfig / shortcutBtnParent 两个字段绑到 PlayerProPanel 组件上。
    //   4. 校验 Config：panel 没在 PanelShortcut 注册、或与总表按键不一致时打警告。
    //
    // 快捷格模板是 Assets/Res/UI/UIItem/HUDSlotItem.prefab（外观在模板上，地址已注册进 UIItem 组），
    // 本生成器不动它；想改格子外观直接改那个预制体。
    //
    // 菜单：Tools/UI/游戏内HUD/1. 在预制体里搭好HUD骨架
    public static class PlayerProPanelBuilder
    {
        // ---------- 目标资产 ----------
        private const string PanelPrefabPath = "Assets/Res/UI/UIPanel/PlayerProPanel.prefab";
        private const string ConfigPath = "Assets/Res/Config/ShortcutBtnConfig.asset";
        private const string HeadName = "Head";

        // ---------- 贴图 ----------
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpriteBar = TexDir + "info_box.png";

        // ---------- 命名 ----------
        private const string BarName = "shortcutBtnParent"; // 与 PlayerProPanel 的字段同名，见名知意
        private const string InfoBgName = "InfoBg";
        private const string OldBarName = "ShortcutBar";    // 旧版静态格子栏，重建时清掉

        // ---------- 版式 ----------
        private const float BarBottomOffset = 28f;
        private const float BarPaddingX = 18f;
        private const float BarPaddingY = 12f;
        private const float SlotSpacing = 14f;   // 与模板格 84x108 配合
        private const float InfoBgMarginX = 48f;
        private const float InfoBgMarginY = 44f;

        // ---------- 配色 ----------
        private static readonly Color ColBarBg = new Color(0.05f, 0.06f, 0.10f, 0.78f);
        private static readonly Color ColInfoBg = new Color(0.05f, 0.06f, 0.10f, 0.62f);

        [MenuItem("Tools/UI/游戏内HUD/1. 在预制体里搭好HUD骨架", false, 40)]
        public static void BuildIntoPrefab()
        {
            var config = AssetDatabase.LoadAssetAtPath<ShortcutBtnConfig>(ConfigPath);
            if (config == null)
            {
                EditorUtility.DisplayDialog("生成失败", $"没找到配置：{ConfigPath}", "好");
                return;
            }

            ValidateConfig(config);

            GameObject root = PrefabUtility.LoadPrefabContents(PanelPrefabPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("生成失败", $"没找到预制体：{PanelPrefabPath}", "好");
                return;
            }

            try
            {
                Build(root, config);
                PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlayerProPanelBuilder] HUD 骨架已写入 {PanelPrefabPath}，并绑好 shortcutBtnConfig / shortcutBtnParent。\n" +
                      $"快捷格共 {config.shortcutButtonsConfig.Count} 条，运行时按 Config 自动实例化 HUDSlotItem。");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath));
        }

        // =====================================================================
        // 构建
        // =====================================================================

        private static void Build(GameObject root, ShortcutBtnConfig config)
        {
            BuildInfoBackdrop(root);
            BuildShortcutBar(root);

            // 把容器与配置绑到面板组件上（字段是 private，走 SerializedObject）
            var panel = root.GetComponent<PlayerProPanel>();
            if (panel == null)
            {
                Debug.LogError($"[PlayerProPanelBuilder] 预制体上没有 PlayerProPanel 组件，无法绑定引用。");
                return;
            }

            var so = new SerializedObject(panel);
            so.FindProperty("shortcutBtnConfig").objectReferenceValue = config;
            so.FindProperty("shortcutBtnParent").objectReferenceValue = root.transform.Find(BarName);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 头像 / 血条那块直接怼在 3D 画面上看不清，给它垫一层半透明底板
        private static void BuildInfoBackdrop(GameObject root)
        {
            DestroyChild(root.transform, InfoBgName);

            var head = root.transform.Find(HeadName) as RectTransform;
            if (head == null)
            {
                Debug.LogWarning($"[PlayerProPanelBuilder] 没找到节点 {HeadName}，跳过信息底板。");
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

        // 底部快捷栏容器：底图 + 水平布局，运行时格子进来会自动排开、底板自动变宽
        private static void BuildShortcutBar(GameObject root)
        {
            // 旧版静态格子栏（若还在）与旧容器都按名字清掉
            DestroyChild(root.transform, OldBarName);
            DestroyChild(root.transform, BarName);

            var go = NewUI(BarName, root.transform);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // 底边居中
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, BarBottomOffset);

            var bg = go.AddComponent<Image>();
            ApplySprite(bg, SpriteBar, true);
            bg.color = ColBarBg;
            bg.raycastTarget = false;

            // 格子数量由 Config 决定，所以容器不写死尺寸：
            // HorizontalLayoutGroup 算出首选尺寸，ContentSizeFitter 按它撑开自己
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)BarPaddingX, (int)BarPaddingX, (int)BarPaddingY, (int)BarPaddingY);
            layout.spacing = SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;  // 格子保持模板的 84x108
            layout.childControlHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // =====================================================================
        // 配置校验
        // =====================================================================

        // Config 决定 HUD 长什么样，键盘注册仍在 PanelShortcut 总表 —— 两边按键不一致时提前暴露
        private static void ValidateConfig(ShortcutBtnConfig config)
        {
            foreach (var data in config.shortcutButtonsConfig)
            {
                PanelShortcut shortcut = PanelShortcut.Get(data.panel);
                if (shortcut == null)
                {
                    Debug.LogWarning($"[PlayerProPanelBuilder] Config「{data.name}」的 panel={data.panel} 没有注册 PanelShortcut，运行时点击与红点不会生效。");
                    continue;
                }

                if (shortcut.Key != data.key)
                {
                    Debug.LogWarning($"[PlayerProPanelBuilder] Config「{data.name}」按键={data.key}，与 PanelShortcut 注册的 {shortcut.Key} 不一致（键盘以总表为准）。");
                }
            }
        }

        // =====================================================================
        // 工具
        // =====================================================================

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform old = parent.Find(childName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        private static void ApplySprite(Image img, string spritePath, bool sliced)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[PlayerProPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{spritePath}");
                return;
            }

            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }
    }
}
