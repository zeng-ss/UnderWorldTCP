using UnityEditor;
using UnityEngine;
using HotUpdate.UI.UIItem;

namespace ProjectEditor
{
    // 给任意 UI 预制体挂 / 卸「悬停放大组件」（HoverScale）的工具。
    //
    // 组件在运行时也会自动补挂（ShortcutButton 会给自己补一个），所以不挂也能跑；
    // 这个工具只是把组件「落到预制体上」，方便在 Inspector 里调参数（倍数 / 时长 / 缓动）。
    //
    // 菜单：Tools/UI/悬停动画
    //   1. 给当前选中的对象添加     —— 在预制体模式里选中任意节点
    //   2. 从当前选中的对象移除
    //   3. 一键给 HUD 快捷格模板添加 —— Res/UI/UIItem/HUDSlotItem.prefab
    public static class HoverScaleTool
    {
        private const string HudSlotPrefabPath = "Assets/Res/UI/UIItem/HUDSlotItem.prefab";

        [MenuItem("Tools/UI/悬停动画/1. 给当前选中的对象添加", false, 60)]
        private static void AddToSelection()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go.GetComponent<HoverScale>() != null) continue;
                Undo.AddComponent<HoverScale>(go);
            }
        }

        [MenuItem("Tools/UI/悬停动画/2. 从当前选中的对象移除", false, 61)]
        private static void RemoveFromSelection()
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                var comp = go.GetComponent<HoverScale>();
                if (comp != null) Undo.DestroyObjectImmediate(comp);
            }
        }

        [MenuItem("Tools/UI/悬停动画/3. 一键给 HUD 快捷格模板添加", false, 62)]
        private static void AddToHudSlot()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudSlotPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("添加失败", $"没找到预制体：{HudSlotPrefabPath}", "好");
                return;
            }

            // 直接改磁盘上的预制体资产：加载 → 加组件 → 存回
            GameObject root = PrefabUtility.LoadPrefabContents(HudSlotPrefabPath);
            if (root == null) return;

            try
            {
                if (root.GetComponent<HoverScale>() == null) root.AddComponent<HoverScale>();
                PrefabUtility.SaveAsPrefabAsset(root, HudSlotPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[HoverScaleTool] 已给 {HudSlotPrefabPath} 挂上 HoverScale。");
        }
    }
}
