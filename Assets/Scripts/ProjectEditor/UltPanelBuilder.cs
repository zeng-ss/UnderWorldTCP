using HotUpdate.UI.UIPanel;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 一键生成「大招过场」面板 UltPanel 的预制体，并注册到 Addressables。
    // 菜单：Tools/UI/大招过场/1. 生成 UltPanel 预制体（含注册）
    //
    // 摆的东西（1920x1080 参考分辨率；所有 Graphic 都是 raycastTarget = false，不挡点击）：
    //   screen    全屏 RawImage —— 运行时被塞进 PlayerCameraCtrl 的 RenderTexture
    //   topBar    顶部黑边（内侧一条金色亮线）—— 过场时从屏幕外「缓慢滑入」
    //   bottomBar 底部黑边（内侧一条金色亮线）
    //   flash     铺满全屏的白闪层 —— 开场闪一下就淡掉
    //
    // 反复执行是幂等的：每次都整个重建预制体（黑边松手位置 = 预制体里的位置，Play 时挪到屏幕外）。
    // 外观想改直接改这个预制体就行，运行时的动画参数在 UltPanel 组件上。
    public static class UltPanelBuilder
    {
        private const string PanelDir = "Assets/Res/UI/UIPanel";
        private const string PrefabPath = PanelDir + "/UltPanel.prefab";
        private const string Address = "UltPanel"; // 必须与 [PanelPath("UltPanel")] 一致
        private const int UiLayer = 5;

        private const float BarHeight = 170f;
        private const float AccentThickness = 3f;

        private static readonly Color BarColor = new Color(0.03f, 0.03f, 0.05f, 1f);
        private static readonly Color AccentColor = new Color(0.91f, 0.78f, 0.48f, 0.9f);

        [MenuItem("Tools/UI/大招过场/1. 生成 UltPanel 预制体（含注册）", false, 60)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(PanelDir)) AssetDatabase.CreateFolder("Assets/Res/UI", "UIPanel");

            GameObject root = BuildHierarchy();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool registered = RegisterAddressable();

            Debug.Log($"[UltPanelBuilder] 已生成 {PrefabPath}（全屏 RawImage + 上下黑边 + 白闪），" +
                      (registered ? $"并注册到 Addressables（地址 {Address}）。" : "但没能注册到 Addressables，请手动注册。") +
                      "\n运行时由 PlayerCameraCtrl 往 screen 里塞 RenderTexture；黑边动画参数在 UltPanel 组件上。");

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // =====================================================================
        // 搭层级
        // =====================================================================

        private static GameObject BuildHierarchy()
        {
            var root = new GameObject("UltPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(UltPanel));
            root.layer = UiLayer;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            var panel = root.GetComponent<UltPanel>();

            // ① 过场画面：全屏铺满，运行时挂 RenderTexture
            RawImage screen = CreateRawImage("screen", root.transform);
            Stretch(screen.rectTransform);
            screen.color = new Color(1f, 1f, 1f, 1f);
            panel.screen = screen;

            // ② 上下黑边：从屏幕外滑进来的字幕条
            RectTransform topBar = CreateImage("topBar", root.transform, BarColor).rectTransform;
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, BarHeight);
            topBar.anchoredPosition = Vector2.zero;
            panel.topBar = topBar;
            CreateAccentLine(topBar, true);

            RectTransform bottomBar = CreateImage("bottomBar", root.transform, BarColor).rectTransform;
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.sizeDelta = new Vector2(0f, BarHeight);
            bottomBar.anchoredPosition = Vector2.zero;
            panel.bottomBar = bottomBar;
            CreateAccentLine(bottomBar, false);

            // ③ 白闪：放在最后 = 盖在黑边之上，开场闪满屏
            Image flash = CreateImage("flash", root.transform, new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform);
            panel.flash = flash;

            return root;
        }

        // 黑边内侧的亮线：top 的贴在底边，bottom 的贴在顶边
        private static void CreateAccentLine(RectTransform bar, bool atBottomEdge)
        {
            Image line = CreateImage("Accent", bar, AccentColor);
            RectTransform rect = line.rectTransform;

            rect.anchorMin = new Vector2(0f, atBottomEdge ? 0f : 1f);
            rect.anchorMax = new Vector2(1f, atBottomEdge ? 0f : 1f);
            rect.pivot = new Vector2(0.5f, atBottomEdge ? 0f : 1f);
            rect.sizeDelta = new Vector2(0f, AccentThickness);
            rect.anchoredPosition = new Vector2(0f, atBottomEdge ? 1f : -1f);
        }

        // =====================================================================
        // 基础件
        // =====================================================================

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false; // 过场是纯展示层，一律不挡射线
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);

            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            return raw;
        }

        // 四边贴满父节点
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        // =====================================================================
        // Addressables
        // =====================================================================

        private static bool RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[UltPanelBuilder] 工程没有 Addressables 设置，跳过注册。");
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
