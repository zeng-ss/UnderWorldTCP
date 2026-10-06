using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 连招配置迁移工具：把原先硬编码在 PlayerAttackState 里的连招规则，
/// 按旧行为回填到 AttackData 的配置字段上。
///
/// 原硬编码 → 新字段的对应关系：
///   GetCurrentEndAni 的 switch（Attack01→Attack01End ...）      → endAnimationName
///   左键连招里 Attack04 特判「跳过重击回第一段」                → nextAttackIndex = 0
///   可被移动打断的 AttackBranch01 / AttackB01Per（0.5 秒后）    → canBeInterruptedByMove / interruptibleTime
///   中键重击入口 currentAttackAnim == "Attack03"                → isHeavyEntry
///   冲刺杀 skillConfigs[0].attackAnimationName == "AttackRush"  → isRushAttack
///
/// 回填按「动画名规则覆盖」，可重复执行（幂等）。
/// </summary>
public static class SkillConfigFieldMigrator
{
    private const string ConfigDir = "Assets/Res/Config";

    /// <summary>旧 GetCurrentEndAni 的硬编码映射</summary>
    private static readonly Dictionary<string, string> EndAniMap = new()
    {
        { "Attack01", "Attack01End" },
        { "Attack02", "Attack02End" },
        { "Attack03", "Attack03End" },
        { "Attack04", "Attack04End" },
        { "Attack04Perfect", "Attack04PerfectEnd" },
        { "AttackRush", "AttackRushEnd" },
        { "AttackB04Per", "AttackB04PerEnd" },
        { "AttackBranch04", "AttackBranch04End" },
    };

    /// <summary>旧代码里「有移动输入就能提前打断收招」的段</summary>
    private static readonly HashSet<string> MoveInterruptible = new() { "AttackBranch01", "AttackB01Per" };

    private const string HeavyEntryAni = "Attack03"; // 旧：普攻表里按中键跳最后一段的起手段
    private const string RushAni = "AttackRush"; // 旧：冲刺杀

    /// <summary>左键连招里需要「跳过后续段、直接回第一段」的段</summary>
    private const string LoopBackAni = "Attack04";

    private const float InterruptibleTime = 0.5f; // 旧代码里写死的 0.5f

    [MenuItem("Tools/连招配置/回填衔接字段（按旧规则）")]
    private static void Migrate()
    {
        List<string> paths = FindSkillConfigPaths();
        if (paths.Count == 0)
        {
            Debug.LogError($"[连招配置] 在 {ConfigDir} 下没找到 SkillConfig 资产");
            return;
        }

        if (!EditorUtility.DisplayDialog("连招配置回填",
                $"按旧硬编码规则回填以下 {paths.Count} 张配置表：\n\n{string.Join("\n", paths)}\n\n" +
                "已填过的同名字段会被覆盖（按动画名规则，可重复执行）。确定继续？",
                "回填", "取消"))
            return;

        int assetCount = 0;
        int segmentCount = 0;

        foreach (string path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            var so = new SerializedObject(asset);
            SerializedProperty list = so.FindProperty("skillConfigs");
            if (list == null || !list.isArray)
            {
                Debug.LogWarning($"[连招配置] {path} 里找不到 skillConfigs 字段，已跳过");
                continue;
            }

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty seg = list.GetArrayElementAtIndex(i);
                string ani = seg.FindPropertyRelative("attackAnimationName")?.stringValue ?? string.Empty;

                string endAni = EndAniMap.TryGetValue(ani, out string mapped) ? mapped : string.Empty;
                bool interruptible = MoveInterruptible.Contains(ani);

                SetString(seg, "endAnimationName", endAni);
                SetInt(seg, "nextAttackIndex", ani == LoopBackAni ? 0 : -1);
                SetBool(seg, "canBeInterruptedByMove", interruptible);
                SetFloat(seg, "interruptibleTime", InterruptibleTime);
                SetBool(seg, "isHeavyEntry", ani == HeavyEntryAni);
                SetBool(seg, "isRushAttack", ani == RushAni);

                segmentCount++;
                Debug.Log($"[连招配置] {Path.GetFileName(path)} 段{i}「{ani}」→ " +
                          $"收招={(string.IsNullOrEmpty(endAni) ? "(无)" : endAni)}, " +
                          $"next段={(ani == LoopBackAni ? 0 : "自增")}, " +
                          $"可被移动打断={interruptible}, 重击入口={ani == HeavyEntryAni}, 冲刺杀={ani == RushAni}");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            assetCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✓ 连招配置回填完成：{assetCount} 张表 / {segmentCount} 段。请打开配置表核对一遍再提交。");
    }

    private static List<string> FindSkillConfigPaths()
    {
        var paths = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ConfigDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path).StartsWith("SkillConfig")) paths.Add(path);
        }

        return paths;
    }

    #region SerializedProperty 辅助（字段缺失时给出提示，而不是静默失败）

    private static void SetString(SerializedProperty seg, string field, string value)
    {
        SerializedProperty p = seg.FindPropertyRelative(field);
        if (p != null) p.stringValue = value;
        else WarnMissing(field);
    }

    private static void SetInt(SerializedProperty seg, string field, int value)
    {
        SerializedProperty p = seg.FindPropertyRelative(field);
        if (p != null) p.intValue = value;
        else WarnMissing(field);
    }

    private static void SetFloat(SerializedProperty seg, string field, float value)
    {
        SerializedProperty p = seg.FindPropertyRelative(field);
        if (p != null) p.floatValue = value;
        else WarnMissing(field);
    }

    private static void SetBool(SerializedProperty seg, string field, bool value)
    {
        SerializedProperty p = seg.FindPropertyRelative(field);
        if (p != null) p.boolValue = value;
        else WarnMissing(field);
    }

    private static void WarnMissing(string field) =>
        Debug.LogWarning($"[连招配置] AttackData 上找不到字段「{field}」，请确认 GameData.cs 已改名 / 已编译");

    #endregion
}
