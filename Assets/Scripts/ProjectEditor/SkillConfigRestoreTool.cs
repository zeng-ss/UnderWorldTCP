using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// 连招配置回填工具：从对照工程（UnderWord -HotUpdate）读取攻击数据与多段伤害特效数据，
// 回填到当前工程的 SkillConfig01~Ex 配置表。
//
// 背景：SkillConfig 里的 HitData / VFXDataList / VFXClip 字段曾被改名为小写
//（hitData / vfxDataList / vfxClip），导致 Unity 序列化时原有的攻击数据与特效数据全部丢失
//（表现为 hitClip=空、hitPrefabs=空、vfxDataList=空，进而连招打不出伤害/特效、无敌人时也无法旋转）。
// 本工具按「动画名」对齐两个工程的攻击段，把对照工程的 HitData 块与 VFXDataList 块回填进来，
// 只做字段名大小写映射，其余内容原样保留（两个工程资源 guid 同源，可直接引用）。
//
// 回填只覆盖 hitData / vfxDataList 两块，当前工程已有的衔接字段
//（endAnimationName / nextAttackIndex / canBeInterruptedByMove 等）保持不变。
public static class SkillConfigRestoreTool
{
    private const string ConfigDir = "Assets/Res/Config";
    private static readonly string[] FileNames = { "SkillConfig01", "SkillConfig02", "SkillConfig03", "SkillConfigEx" };

    // 对照工程路径（可在菜单里改，改完记住到 EditorPrefs）
    private const string SrcRootKey = "SkillConfigRestoreTool.SrcRoot";
    private const string DefaultSrcRoot = @"D:\Unity\UnderWord -HotUpdate";

    // 字段名映射：对照工程 -> 当前工程
    private static readonly (string from, string to)[] FieldMap =
    {
        ("HitData", "hitData"),
        ("VFXDataList", "vfxDataList"),
        ("VFXClip", "vfxClip"),
    };

    [MenuItem("Tools/连招配置/回填攻击与特效数据（从对照工程）")]
    private static void Restore()
    {
        string srcRoot = EditorPrefs.GetString(SrcRootKey, DefaultSrcRoot);
        string srcConfigDir = Path.Combine(srcRoot, "Assets", "Res", "Config");

        if (!Directory.Exists(srcConfigDir))
        {
            if (!EditorUtility.DisplayDialog("找不到对照工程",
                    $"对照工程的连招配置目录不存在：\n{srcConfigDir}\n\n是否手动选择对照工程根目录？", "选择目录", "取消"))
                return;

            string picked = EditorUtility.OpenFolderPanel("选择对照工程根目录（UnderWord -HotUpdate）", srcRoot, "");
            if (string.IsNullOrEmpty(picked)) return;
            srcRoot = picked;
            srcConfigDir = Path.Combine(srcRoot, "Assets", "Res", "Config");
            EditorPrefs.SetString(SrcRootKey, srcRoot);
        }

        if (!Directory.Exists(srcConfigDir))
        {
            Debug.LogError($"[连招回填] 对照工程配置目录仍不存在：{srcConfigDir}");
            return;
        }

        // 先做一次可行性核对：列出每张表「对照段 / 当前段」与缺失情况
        var report = new List<string>();
        int restoredFile = 0;
        int restoredSegment = 0;

        // 项目根目录：Application.dataPath 是 <项目>/Assets，去掉尾部 Assets 得到项目根
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        foreach (string fileName in FileNames)
        {
            string srcPath = Path.Combine(srcConfigDir, fileName + ".asset");
            string dstPath = Path.Combine(ConfigDir, fileName + ".asset");
            string dstFull = Path.Combine(projectRoot, dstPath);

            if (!File.Exists(srcPath))
            {
                report.Add($"[跳过] {fileName}：对照工程无此文件");
                continue;
            }

            if (!File.Exists(dstFull))
            {
                report.Add($"[跳过] {fileName}：当前工程无此文件");
                continue;
            }

            string srcText = File.ReadAllText(srcPath);
            string dstText = File.ReadAllText(dstFull);

            // 解析对照工程：动画名 -> (HitData 行, VFXDataList 行)，并做字段名映射
            Dictionary<string, (List<string> hit, List<string> vfx)> srcByAni = ParseSource(srcText);
            List<string> dstAnis = ParseDstAnis(dstText);

            var missing = new List<string>();
            foreach (string ani in dstAnis)
                if (!srcByAni.ContainsKey(ani)) missing.Add(ani);

            if (missing.Count > 0)
                report.Add($"[警告] {fileName}：当前工程的段在对照工程里找不到对应动画：{string.Join(", ", missing)}");

            string rebuilt = Rebuild(dstText, srcByAni);
            File.WriteAllText(dstFull, rebuilt, new System.Text.UTF8Encoding(false));

            restoredFile++;
            restoredSegment += dstAnis.Count - missing.Count;
            report.Add($"[完成] {fileName}：回填 {dstAnis.Count - missing.Count} 段");
        }

        AssetDatabase.Refresh();

        report.Insert(0, $"从对照工程回填完成：{restoredFile} 张表 / {restoredSegment} 段。");
        report.Insert(0, $"对照工程路径：{srcConfigDir}");
        Debug.Log("[连招回填]\n" + string.Join("\n", report));

        EditorUtility.DisplayDialog("连招配置回填",
            string.Join("\n", report) + "\n\n请打开配置表核对，确认无误后再提交。", "好的");
    }

    // 解析对照工程：每个攻击段提取 HitData 块与 VFXDataList 块
    private static Dictionary<string, (List<string>, List<string>)> ParseSource(string text)
    {
        var result = new Dictionary<string, (List<string>, List<string>)>();
        string[] lines = text.Split('\n');
        int i = 0;

        while (i < lines.Length)
        {
            Match m = Regex.Match(lines[i], @"^  - attackAnimationName:\s*(.*)$");
            if (!m.Success) { i++; continue; }

            string ani = m.Groups[1].Value.Trim();
            i++;

            var hit = new List<string>();
            var vfx = new List<string>();
            bool inHit = false, inVfx = false;

            while (i < lines.Length && !Regex.IsMatch(lines[i], @"^  - attackAnimationName:"))
            {
                string l = lines[i];
                if (l.StartsWith("    HitData:")) { inHit = true; inVfx = false; hit.Add(l); i++; continue; }
                if (l.StartsWith("    VFXDataList:")) { inHit = false; inVfx = true; vfx.Add(l); i++; continue; }

                if (inHit) hit.Add(l);
                else if (inVfx) vfx.Add(l);
                i++;
            }

            result[ani] = (MapFields(hit), MapFields(vfx));
        }

        return result;
    }

    // 解析当前工程的动画名列表（保持顺序）
    private static List<string> ParseDstAnis(string text)
    {
        var anis = new List<string>();
        foreach (Match m in Regex.Matches(text, @"^  - attackAnimationName:\s*(.*)$", RegexOptions.Multiline))
            anis.Add(m.Groups[1].Value.Trim());
        return anis;
    }

    // 字段名大小写映射
    private static List<string> MapFields(List<string> lines)
    {
        var outLines = new List<string>(lines.Count);
        foreach (string l in lines)
        {
            string cur = l;
            foreach (var (from, to) in FieldMap)
                cur = Regex.Replace(cur, @"\b" + from + @"\s*:", to + ":");
            outLines.Add(cur);
        }

        return outLines;
    }

    // 重建当前工程：把每个段的 hitData 块 / vfxDataList 块替换为对照工程内容
    private static string Rebuild(string dstText, Dictionary<string, (List<string>, List<string>)> srcByAni)
    {
        string[] lines = dstText.Split('\n');
        var outLines = new List<string>(lines.Length + 64);
        int i = 0;

        while (i < lines.Length)
        {
            Match m = Regex.Match(lines[i], @"^  - attackAnimationName:\s*(.*)$");
            if (!m.Success) { outLines.Add(lines[i]); i++; continue; }

            string ani = m.Groups[1].Value.Trim();
            outLines.Add(lines[i]);
            i++;

            // 跳过原 hitData 块与 vfxDataList 块，其余行保留
            while (i < lines.Length && !Regex.IsMatch(lines[i], @"^  - attackAnimationName:"))
            {
                string l = lines[i];
                if (l.StartsWith("    hitData:"))
                {
                    i++;
                    while (i < lines.Length && !Regex.IsMatch(lines[i], @"^  - attackAnimationName:"))
                    {
                        if (lines[i].StartsWith("    vfxDataList:")) break;
                        i++;
                    }

                    continue;
                }

                if (l.StartsWith("    vfxDataList:"))
                {
                    i++;
                    while (i < lines.Length && !Regex.IsMatch(lines[i], @"^  - attackAnimationName:"))
                        i++;
                    continue;
                }

                outLines.Add(l);
                i++;
            }

            // 段结束：插入回填的 hitData 与 vfxDataList 块
            if (srcByAni.TryGetValue(ani, out var blocks))
            {
                outLines.AddRange(blocks.Item1);
                outLines.AddRange(blocks.Item2);
            }
        }

        return string.Join("\n", outLines);
    }
}
