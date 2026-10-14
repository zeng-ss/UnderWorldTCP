using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectEditor
{
    // 中文字体显示成「框框」的修复工具。
    //
    // 本项目 AlimamaDongFangDaKai-Regular SDF.asset 是 Dynamic（动态）模式，所以理论上
    // 遇到没烘焙过的汉字会自动往图集里补。但实测图集只有 1024x1024、占用已达 86.7%
    // （Xmax=1022 / Ymax=1023，基本塞满），而「Multi Atlas Textures」是关闭的 ——
    // 塞不下又开不了第二张图集，TMP 就只能渲染缺字方框，于是中文一片框框。
    //
    // 另一处：TMP Settings 的全局回退字体列表为空，凡是直接用 LiberationSans 的
    // TMP 组件（本项目预制体里还有 44 处）里的中文同样全是框。
    public static class TMPFontAtlasFixer
    {
        // 中文字体资源
        private const string FontAssetPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        // TMP 全局设置
        private const string TmpSettingsPath = "Assets/UnityShop/URP/TextMesh Pro/Resources/TMP Settings.asset";

        // 目标图集边长（必须是 2 的幂）
        private const int AtlasSize = 2048;

        [MenuItem("Tools/TMP/字体/一键修复中文框框", false, 1)]
        public static void FixAll()
        {
            TMP_FontAsset font = FixAtlasSettings();
            if (font == null) return;

            AddAsGlobalFallback(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TMPFontAtlasFixer] 完成：{font.name} 已改为 Dynamic + 多图集 + " +
                      $"{AtlasSize}x{AtlasSize}，并已加入 TMP 全局回退字体。");
        }

        [MenuItem("Tools/TMP/字体/1. 修复字体图集（动态 + 多图集 + 放大）", false, 11)]
        public static void FixAtlasSettingsMenu()
        {
            TMP_FontAsset font = FixAtlasSettings();
            if (font == null) return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TMPFontAtlasFixer] {font.name} 图集已清空并按 {AtlasSize}x{AtlasSize} 重建，多图集已开启。");
        }

        [MenuItem("Tools/TMP/字体/2. 设为 TMP 全局回退字体", false, 12)]
        public static void AddGlobalFallbackMenu()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Debug.LogError($"[TMPFontAtlasFixer] 找不到字体资源：{FontAssetPath}");
                return;
            }

            AddAsGlobalFallback(font);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/TMP/字体/3. 预烘焙字符集（从 txt 选择）", false, 13)]
        public static void PrebakeFromFile()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Debug.LogError($"[TMPFontAtlasFixer] 找不到字体资源：{FontAssetPath}");
                return;
            }

            // 任意一份字符集文本（每行一串汉字或整篇文本都行），只取其中出现过的字
            string path = EditorUtility.OpenFilePanelWithFilters("选择字符集文本", Application.dataPath,
                new[] { "文本", "txt" });
            if (string.IsNullOrEmpty(path)) return;

            Prebake(font, File.ReadAllText(path, Encoding.UTF8));
        }

        // 改成动态 + 多图集 + 放大图集，并清空重建
        private static TMP_FontAsset FixAtlasSettings()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Debug.LogError($"[TMPFontAtlasFixer] 找不到字体资源：{FontAssetPath}");
                return null;
            }

            // 1) 动态模式 + 多图集（这两个都有 public setter）
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;

            // 2) 图集尺寸：atlasWidth / atlasHeight 是 internal set，脚本里只能走 SerializedObject
            var so = new SerializedObject(font);
            SerializedProperty w = so.FindProperty("m_AtlasWidth");
            SerializedProperty h = so.FindProperty("m_AtlasHeight");
            if (w == null || h == null)
            {
                Debug.LogError("[TMPFontAtlasFixer] 无法访问 m_AtlasWidth / m_AtlasHeight，TMP 版本可能不匹配。");
                return null;
            }

            w.intValue = AtlasSize;
            h.intValue = AtlasSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 3) 清空字形表与图集。ClearAtlasTextures 内部发现贴图尺寸与 m_AtlasWidth/Height
            //    不一致时会自动 Reinitialize 到新尺寸，等于「改尺寸 + 重建图集」一步到位。
            //    动态模式下被清掉的字符会在用到时自动补回来；静态模式则会把字全丢光，故此处先强制 Dynamic。
            font.ClearFontAssetData(false);

            EditorUtility.SetDirty(font);
            return font;
        }

        // 加入 TMP Settings 的全局回退列表（已在列表里则跳过）
        private static void AddAsGlobalFallback(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"[TMPFontAtlasFixer] 找不到 TMP Settings：{TmpSettingsPath}，跳过全局回退设置。");
                return;
            }

            var so = new SerializedObject(settings);
            SerializedProperty list = so.FindProperty("m_fallbackFontAssets");
            if (list == null)
            {
                Debug.LogWarning("[TMPFontAtlasFixer] TMP Settings 里找不到 m_fallbackFontAssets 字段，跳过。");
                return;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == font)
                {
                    Debug.Log($"[TMPFontAtlasFixer] {font.name} 已在全局回退列表中，无需重复添加。");
                    return;
                }
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            Debug.Log($"[TMPFontAtlasFixer] 已把 {font.name} 加入 TMP 全局回退字体，当前共 {list.arraySize} 个。");
        }

        // 预烘焙：把字符集里的字一次性写进图集，省掉运行时逐字生成的开销
        private static void Prebake(TMP_FontAsset font, string raw)
        {
            var set = new HashSet<char>();
            foreach (char c in raw)
            {
                if (!char.IsControl(c) && !char.IsWhiteSpace(c)) set.Add(c);
            }

            if (set.Count == 0)
            {
                Debug.LogWarning("[TMPFontAtlasFixer] 字符集里没有可用字符。");
                return;
            }

            var sb = new StringBuilder(set.Count);
            foreach (char c in set) sb.Append(c);

            bool all = font.TryAddCharacters(sb.ToString(), out string missing);

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            if (all)
                Debug.Log($"[TMPFontAtlasFixer] 预烘焙完成：{set.Count} 个字符全部写入图集。");
            else
                Debug.LogWarning($"[TMPFontAtlasFixer] 预烘焙未全部成功，缺失 {missing.Length} 个字符" +
                                 "（字体本身没有该字形，或图集容量不足，汉字太多请分批）。");
        }
    }
}
