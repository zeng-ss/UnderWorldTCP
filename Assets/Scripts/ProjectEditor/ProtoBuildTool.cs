using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键：生成Proto C# → 编译Network.dll → 部署到Plugins/Net
/// 菜单: Tools → 一键部署 Network.dll
/// </summary>
public class ProtoBuildTool : EditorWindow
{
    private static readonly string Root = @"D:\BaiduNetdiskDownload\课程资料\ARPGDemo_Server";
    private static readonly string ProtoDir = @"D:\BaiduNetdiskDownload\课程资料\服务端\proto";
    private static readonly string Protoc = ProtoDir + @"\bin\protoc.exe";
    private static readonly string ProtoSrc = ProtoDir + @"\proto";
    private static readonly string ProtoOut = ProtoDir + @"\out";
    private static readonly string NetworkCsproj = Root + @"\Network\Network.csproj";
    private static readonly string NetworkProtoDir = Root + @"\Network\Proto";
    private static readonly string NetworkBin = Root + @"\Network\bin\Debug";
    private static readonly string UnityNetDir = Application.dataPath + @"/Plugins/Net";

    private static string _log = "";

    [MenuItem("Tools/一键部署 Network.dll")]
    public static void BuildAndDeploy()
    {
        _log = "";
        bool ok = true;

        // Step 1: protoc
        ok &= Step("生成 Proto CS", () =>
        {
            if (!File.Exists(Protoc))
                throw new System.Exception("protoc.exe 未找到: " + Protoc);
            Directory.CreateDirectory(ProtoOut);
            foreach (var pf in new[] { "ResultEntity.proto", "RequestEntity.proto" })
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Protoc,
                        Arguments = $"-I={Quote(ProtoSrc)} --csharp_out={Quote(ProtoOut)} {Quote(ProtoSrc + "\\" + pf)}",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode != 0) throw new System.Exception($"protoc {pf} 失败: {err}");
            }
        });

        // Step 2: 复制 CS 到 Network/Proto
        ok &= Step("复制 CS 文件", () =>
        {
            foreach (var f in new[] { "ResultEntity.cs", "RequestEntity.cs" })
            {
                var src = ProtoOut + "\\" + f;
                var dst = NetworkProtoDir + "\\" + f;
                if (File.Exists(src))
                    File.Copy(src, dst, true);
                else
                    LogWarning($"跳过 {f} (源不存在)");
            }
        });

        // Step 3: 编译 Network.csproj
        ok &= Step("编译 Network.csproj", () =>
        {
            string msbuild = FindMsBuild();
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = msbuild,
                    Arguments = $"{Quote(NetworkCsproj)} /t:Build /p:Configuration=Debug /v:minimal /nologo",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0) throw new System.Exception($"编译失败:\n{stdout}\n{stderr}");
            Log("编译输出 (最后5行):");
            var lines = stdout.Replace("\r", "").Split('\n');
            for (int i = System.Math.Max(0, lines.Length - 5); i < lines.Length; i++)
                if (!string.IsNullOrWhiteSpace(lines[i])) Log("  " + lines[i].Trim());
        });

        // Step 4: 删除旧 DLL + 复制新 DLL
        ok &= Step("部署 DLL 到 Plugins/Net", () =>
        {
            if (EditorApplication.isPlaying)
                throw new System.Exception("请先退出 Play 模式再执行");

            Directory.CreateDirectory(UnityNetDir);

            // 删除旧 DLL
            foreach (var f in Directory.GetFiles(UnityNetDir, "*.dll"))
            {
                File.Delete(f);
                Log($"  删除: {Path.GetFileName(f)}");
            }

            // 复制新 DLL
            foreach (var f in Directory.GetFiles(NetworkBin, "*.dll"))
            {
                string dst = UnityNetDir + "/" + Path.GetFileName(f);
                File.Copy(f, dst, true);
                Log($"  复制: {Path.GetFileName(f)}");
            }
        });

        // 刷新 Unity AssetDatabase
        AssetDatabase.Refresh();
        Log("");

        if (ok)
        {
            Log("======== 全部完成！Network.dll 已部署 ========");
            EditorUtility.DisplayDialog("部署完成", "Network.dll 已编译并复制到 Plugins/Net", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("部署失败", _log, "确定");
        }
    }

    [MenuItem("Tools/查看 Proto 构建日志")]
    public static void ShowLog()
    {
        EditorUtility.DisplayDialog("Proto 构建日志", string.IsNullOrEmpty(_log) ? "(无日志)" : _log, "确定");
    }

    private static bool Step(string name, System.Action action)
    {
        Log($"\n[{name}]");
        try
        {
            action();
            Log($"[{name}] ✓ 完成");
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"[{name}] ✗ 失败: {ex.Message}");
            return false;
        }
    }

    private static string FindMsBuild()
    {
        string[] candidates =
        {
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        };
        foreach (var c in candidates)
            if (File.Exists(c)) return c;

        // vswhere
        var vswhere = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        if (File.Exists(vswhere))
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = vswhere,
                    Arguments = "-latest -products * -requires Microsoft.Component.MSBuild -property installationPath",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string vsPath = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            if (!string.IsNullOrEmpty(vsPath))
            {
                var msbuild = Directory.GetFiles(vsPath, "MSBuild.exe", SearchOption.AllDirectories);
                if (msbuild.Length > 0) return msbuild[0];
            }
        }

        throw new System.Exception("未找到 MSBuild.exe。请确认 Visual Studio 已安装。");
    }

    private static string Quote(string s) => $"\"{s}\"";

    private static void Log(string msg)
    {
        _log += msg + "\n";
        UnityEngine.Debug.Log("[ProtoBuild] " + msg);
    }

    private static void LogWarning(string msg)
    {
        _log += "[WARN] " + msg + "\n";
        UnityEngine.Debug.LogWarning("[ProtoBuild] " + msg);
    }

    private static void LogError(string msg)
    {
        _log += "[ERR] " + msg + "\n";
        UnityEngine.Debug.LogError("[ProtoBuild] " + msg);
    }
}
