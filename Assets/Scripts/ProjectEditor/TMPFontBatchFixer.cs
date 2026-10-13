using TMPro;
using UnityEditor;
using UnityEngine;

namespace ProjectEditor
{
    public static class TMPFontBatchFixer
    {
        // 要处理的文件夹（相对 Assets）
        private const string TargetFolder = "Assets/Res/UI";

        // 目标字体资源路径
        private const string FontAssetPath = "Assets/Resources/AlimamaDongFangDaKai-Regular SDF.asset";

        [MenuItem("Tools/TMP/批量修复缺失字体")]
        public static void FixMissingFonts()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                Debug.LogError($"找不到字体资源：{FontAssetPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(TargetFolder))
            {
                Debug.LogError($"文件夹不存在：{TargetFolder}");
                return;
            }

            // 找出文件夹下所有预制体
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TargetFolder });
            int prefabCount = 0;
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool dirty = false;

                // 包含未激活的子物体
                var tmpComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmpComponents)
                {
                    // 只修复"字体为空"或"字体丢失"的
                    if (tmp.font == null)
                    {
                        tmp.font = fontAsset;
                        dirty = true;
                        fixedCount++;
                    }
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(prefab);
                    prefabCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"TMP 字体修复完成：扫描预制体 {guids.Length} 个，修改 {prefabCount} 个预制体，" +
                      $"修复 {fixedCount} 个 TextMeshProUGUI 组件。");
        }
    }
}