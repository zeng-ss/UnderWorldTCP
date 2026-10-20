using System.Collections.Generic;
using HotUpdate.UI.UIPanel;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectEditor
{
    // 一键生成「过渡面板」TransitionPanel 的预制体，并注册到 Addressables。
    // 菜单：Tools/UI/过渡面板/1. 生成 TransitionPanel 预制体（含注册）
    //
    // 它是常驻在 Canvas 最上层的一层遮罩（子 Canvas overrideSorting），
    // 由 UIManager.Transition(...) 驱动：淡入 → 执行切换 → 停留 → 淡出。
    //
    // 摆的东西（参考分辨率 1920x1080）：
    //   Root/Bg      全屏深色底
    //   Root/Ring    中心光环（循环旋转）
    //   Root/Title   「正在载入」文字（呼吸）
    //   Root/Dots    三个圆点（依次跳动）
    public static class TransitionPanelBuilder
    {
        private const string PanelDir = "Assets/Res/UI/UIPanel";
        private const string PrefabPath = PanelDir + "/TransitionPanel.prefab";
        private const string Address = "TransitionPanel";
        private const int UiLayer = 5;

        private const string FontPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";
        private const string TexDir = "Assets/Resources/SharpUI/Textures/";
        private const string SpriteRing = TexDir + "round_frame.png";
        private const string SpriteDot = TexDir + "circle_mask.png";
        private const string SpriteScroll = TexDir + "ability_slash.png";

        // 流光层的加色滚动材质（URP 版，见 Resources/Shaders/UIScrollAdditive.shader）
        private const string ScrollShader = "UnderWord/UI/ScrollAdditive";
        private const string MaterialDir = "Assets/Resources/Materials";
        private const string MaterialPath = MaterialDir + "/TransitionScroll.mat";

        private static readonly Color ColBg = new Color(0.035f, 0.045f, 0.07f, 1f);
        private static readonly Color ColScroll = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color ColTitle = new Color(1f, 0.93f, 0.76f, 1f);
        private static readonly Color ColDot = new Color(1f, 0.85f, 0.35f, 1f);

        [MenuItem("Tools/UI/过渡面板/1. 生成 TransitionPanel 预制体（含注册）", false, 75)]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(PanelDir)) AssetDatabase.CreateFolder("Assets/Res/UI", "UIPanel");

            GameObject root = BuildHierarchy();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool registered = RegisterAddressable();
            Debug.Log($"[TransitionPanelBuilder] 已生成 {PrefabPath}，" +
                      (registered ? $"并注册到 Addressables（地址 {Address}）。" : "但没能注册到 Addressables，请手动注册。"));
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // =====================================================================
        // 搭层级
        // =====================================================================

        private static GameObject BuildHierarchy()
        {
            // 根节点故意不挂 Canvas：预制体根节点上的 Canvas 会被 Unity 当成「根 Overlay Canvas」，
            // 把 RectTransform 的 localScale / anchor / sizeDelta 全部序列化成 0，
            // 实例化到父 Canvas 下就整块 scale 0、宽高 0，什么都看不见。
            // Canvas + GraphicRaycaster 在 TransitionPanel.Awake 里动态加（那时已经挂在父 Canvas 下了）。
            var root = new GameObject("TransitionPanel", typeof(RectTransform), typeof(TransitionPanel));
            root.layer = UiLayer;
            Stretch(root.GetComponent<RectTransform>());

            // 所有视觉挂在 Root 下，由它的 CanvasGroup 统一控制透明度 / 点击拦截
            GameObject content = NewUI("Root", root.transform);
            Stretch((RectTransform)content.transform);
            var group = content.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            Image bg = NewUI("Bg", content.transform).AddComponent<Image>();
            Stretch(bg.rectTransform);
            bg.color = ColBg;
            bg.raycastTarget = true;

            // 流光层：RawImage + uvRect 无限滚动（不依赖 shader 的 UV 变换，换什么材质都能滚）
            RawImage scroll = NewUI("Scroll", content.transform).AddComponent<RawImage>();
            Stretch(scroll.rectTransform);
            EnsureRepeatTexture(SpriteScroll);
            scroll.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpriteScroll);
            // uvRect 固定 0~1：平铺次数写在材质的 _MainTex_ST 上，滚动只改材质 UV 偏移，
            // 这样每帧不会 SetVerticesDirty 重建整块全屏网格（那是过渡期间掉帧的主因）
            scroll.uvRect = new Rect(0f, 0f, 1f, 1f);
            scroll.color = ColScroll;
            scroll.raycastTarget = false;
            Material scrollMat = EnsureMaterial();
            if (scrollMat != null) scroll.material = scrollMat;

            Image ring = NewUI("Ring", content.transform).AddComponent<Image>();
            Center(ring.rectTransform, new Vector2(180f, 180f));
            ring.rectTransform.anchoredPosition = new Vector2(0f, 110f);
            ApplySprite(ring, SpriteRing, false);

            TMP_Text title = NewText("Title", content.transform, "正在载入…", 34f);
            Center(title.rectTransform, new Vector2(600f, 50f));
            title.rectTransform.anchoredPosition = new Vector2(0f, -40f);

            GameObject dots = NewUI("Dots", content.transform);
            Center((RectTransform)dots.transform, new Vector2(200f, 30f));
            ((RectTransform)dots.transform).anchoredPosition = new Vector2(0f, -120f);

            var dotRects = new List<RectTransform>();
            for (int i = 0; i < 3; i++)
            {
                Image dot = NewUI($"Dot{i}", dots.transform).AddComponent<Image>();
                Center(dot.rectTransform, new Vector2(16f, 16f));
                dot.rectTransform.anchoredPosition = new Vector2((i - 1) * 34f, 0f);
                ApplySprite(dot, SpriteDot, false);
                dot.color = ColDot;
                dot.raycastTarget = false;
                dotRects.Add(dot.rectTransform);
            }

            BindRefs(root.GetComponent<TransitionPanel>(), group, scroll, ring.rectTransform, title, dotRects);
            return root;
        }

        // 引用一次性写进序列化字段，运行时不再按名字找
        private static void BindRefs(TransitionPanel panel, CanvasGroup group, RawImage scroll, RectTransform ring,
            TMP_Text title, List<RectTransform> dots)
        {
            var so = new SerializedObject(panel);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("scrollLayer").objectReferenceValue = scroll;
            so.FindProperty("ring").objectReferenceValue = ring;
            so.FindProperty("title").objectReferenceValue = title;

            SerializedProperty array = so.FindProperty("dots");
            array.arraySize = dots.Count;
            for (int i = 0; i < dots.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = dots[i];

            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static TMP_Text NewText(string name, Transform parent, string text, float size)
        {
            GameObject go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ColTitle;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) tmp.font = font;
            return tmp;
        }

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

        private static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void ApplySprite(Image image, string path, bool sliced)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[TransitionPanelBuilder] 未找到贴图（需以 Sprite 类型导入）：{path}");
                return;
            }

            image.sprite = sprite;
            if (sliced) image.type = Image.Type.Sliced;
        }

        // 流光材质：第一次生成时创建资源，之后复用（改了 shader 也能自动同步）
        private static Material EnsureMaterial()
        {
            Shader shader = Shader.Find(ScrollShader);
            if (shader == null)
            {
                Debug.LogError($"[TransitionPanelBuilder] 找不到 Shader {ScrollShader}，" +
                               "流光层会退回默认 UI 材质（滚动失效）。请确认 UIScrollAdditive.shader 已导入。");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(MaterialDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }

        // RawImage 的 uvRect 会超出 0~1，纹理 wrap 必须是 Repeat（默认 Clamp 会在边界拉出一条线）
        private static void EnsureRepeatTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.wrapMode == TextureWrapMode.Repeat) return;

            settings.wrapMode = TextureWrapMode.Repeat;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Debug.Log($"[TransitionPanelBuilder] 已把 {path} 的 Wrap Mode 改为 Repeat（流光平铺需要）。");
        }

        // =====================================================================
        // Addressables
        // =====================================================================

        private static bool RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[TransitionPanelBuilder] 工程没有 Addressables 设置，跳过注册。");
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
