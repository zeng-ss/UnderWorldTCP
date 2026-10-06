using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Build
{
    private static string buildPath => Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName,
        $"Builds/{Application.productName}.exe");

    /// <summary>
    /// 生成DLL字节文件到 DllBytes 目录（由 Addressables 收集，跑一遍 Tools/Addressables/一键标记资源 即可）
    /// </summary>
    [MenuItem("Build/GenerateDllFiles")]
    public static void GenerateDllFiles()
    {
        Debug.Log("开始生成DLL字节文件！");
        string environmentDir = Environment.CurrentDirectory;

        string aotDllDir = Path.Combine(environmentDir,
            SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget));
        string hotUpdateDllDir = Path.Combine(environmentDir,
            SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget));

        string aotTextDir = Path.Combine(environmentDir, "Assets/DllBytes/AOT");
        string hotUpdateTextDir = Path.Combine(environmentDir, "Assets/DllBytes/HotUpdate");
        string priorityHotUpdateTextDir = Path.Combine(environmentDir, "Assets/DllBytes/PriorityHotUpdate");

        DllConfig dllConfig = AssetDatabase.LoadAssetAtPath<DllConfig>("Assets/Config/DllConfig.asset");
        if (dllConfig == null)
        {
            Debug.LogError("未找到 DllConfig，请在 Assets/Config/ 下创建");
            return;
        }

        // 复制 AOT DLL
        Directory.CreateDirectory(aotTextDir);
        foreach (string dllName in dllConfig.aot)
        {
            string dllPath = Path.Combine(aotDllDir, $"{dllName}");
            if (!File.Exists(dllPath)) dllPath = Path.Combine(hotUpdateDllDir, $"{dllName}");
            if (!File.Exists(dllPath)) { Debug.LogWarning($"找不到DLL: {dllName}"); continue; }
            string dllBytesPath = Path.Combine(aotTextDir, $"{dllName}.bytes");
            File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"AOT: {dllName}");
        }

        // 复制 HotUpdate DLL
        Directory.CreateDirectory(hotUpdateTextDir);
        foreach (string dllName in dllConfig.hotUpdate)
        {
            string dllPath = Path.Combine(hotUpdateDllDir, $"{dllName}");
            if (!File.Exists(dllPath)) { Debug.LogWarning($"找不到DLL: {dllName}"); continue; }
            string dllBytesPath = Path.Combine(hotUpdateTextDir, $"{dllName}.bytes");
            File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"HotUpdate: {dllName}");
        }

        // 复制 PriorityHotUpdate DLL
        Directory.CreateDirectory(priorityHotUpdateTextDir);
        foreach (string dllName in dllConfig.priorityHotUpdate)
        {
            string dllPath = Path.Combine(hotUpdateDllDir, $"{dllName}");
            if (!File.Exists(dllPath)) { Debug.LogWarning($"找不到DLL: {dllName}"); continue; }
            string dllBytesPath = Path.Combine(priorityHotUpdateTextDir, $"{dllName}.bytes");
            File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"PriorityHotUpdate: {dllName}");
        }

        AssetDatabase.Refresh();
        Debug.Log("成功生成DLL字节文件！请运行 Tools/Addressables/一键标记资源 将新增的 .bytes 标记为 Addressable");
    }

    /// <summary>
    /// 构建新客户端（完整构建）
    /// </summary>
    [MenuItem("Build/NewClient")]
    public static void NewClient()
    {
        PrebuildCommand.GenerateAll();
        GenerateDllFiles();

        string[] scenes = new string[EditorSceneManager.sceneCountInBuildSettings];
        for (int i = 0; i < EditorSceneManager.sceneCountInBuildSettings; i++)
        {
            scenes[i] = SceneUtility.GetScenePathByBuildIndex(i);
            Debug.Log($"添加场景: {scenes[i]}");
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
        {
            scenes = scenes,
            target = EditorUserBuildSettings.activeBuildTarget,
            locationPathName = buildPath,
            options = BuildOptions.Development | BuildOptions.AllowDebugging,
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log($"构建完成: {buildPath}");
    }

    /// <summary>
    /// 增量更新客户端
    /// </summary>
    [MenuItem("Build/UpdateClient")]
    public static void UpdateClient()
    {
        PrebuildCommand.GenerateAll();
        GenerateDllFiles();
        Debug.Log("请执行 Build/Addressables/Build New Build > Default Build Script 构建 Addressables，然后更新远程资源");
    }
}
