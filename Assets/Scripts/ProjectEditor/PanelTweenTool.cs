using System.Collections.Generic;
using HotUpdate.UI;
using UnityEditor;
using UnityEngine;

namespace ProjectEditor
{
    // 给面板预制体批量挂 / 卸「打开动画组件」（PanelTween）的工具。
    //
    // 不挂也能跑 —— BasePanel 在 Awake 里会自动给面板根节点补一个 PanelTween，所有面板默认就有打开动画。
    // 这个工具只是把组件「落到预制体上」，方便在 Inspector 里逐个面板调动画参数（类型 / 时长 / 起始缩放）。
    // 注意：代码里的 BasePanel.AnimConfig 一旦重写（非 null），会覆盖预制体上的参数。
    public static class PanelTweenTool
    {
        // 面板预制体所在目录（与 Addressables 的 UIPanel 组一致）
        private const string PanelPrefabDir = "Assets/Res/UI/UIPanel";

        [MenuItem("Tools/UI/面板动画/1. 给选中预制体添加动画组件")]
        private static void AddToSelection()
        {
            Apply(SelectedPrefabPaths(), true);
        }

        [MenuItem("Tools/UI/面板动画/2. 从选中预制体移除动画组件")]
        private static void RemoveFromSelection()
        {
            Apply(SelectedPrefabPaths(), false);
        }

        [MenuItem("Tools/UI/面板动画/3. 一键给所有面板预制体添加动画组件")]
        private static void AddToAllPanels()
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PanelPrefabDir }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            if (paths.Count == 0)
            {
                Debug.LogWarning($"[PanelTweenTool] {PanelPrefabDir} 下没找到面板预制体。");
                return;
            }

            Apply(paths, true);
        }

        // 对一组预制体批量增删组件；isAdd 区分增删，返回真正改动的数量
        private static void Apply(List<string> paths, bool isAdd)
        {
            if (paths.Count == 0)
            {
                Debug.LogWarning("[PanelTweenTool] 没选中任何预制体（请在 Project 窗口里选 .prefab）。");
                return;
            }

            int changed = 0;
            foreach (string path in paths)
            {
                if (SetOnPrefab(path, isAdd)) changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string action = isAdd ? "添加" : "移除";
            Debug.Log($"[PanelTweenTool] 已{action}动画组件：{changed}/{paths.Count} 个预制体（其余本来就{action}过）。");
        }

        // 打开预制体 → 在根节点增删 PanelTween → 存回；返回是否发生改动
        private static bool SetOnPrefab(string path, bool isAdd)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PanelTween existing = root.GetComponent<PanelTween>();

                if (isAdd)
                {
                    if (existing != null) return false;
                    root.AddComponent<PanelTween>();
                }
                else
                {
                    if (existing == null) return false;
                    Object.DestroyImmediate(existing, true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 取当前选中的预制体资源路径
        private static List<string> SelectedPrefabPaths()
        {
            var paths = new List<string>();
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab")) paths.Add(path);
            }

            return paths;
        }
    }
}
