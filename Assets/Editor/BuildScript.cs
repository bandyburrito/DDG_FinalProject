using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Standalone-build helpers. Use the "Build" menu inside the editor, or invoke
/// BuildScript.BuildLinux64 / BuildWindows64 from the command line:
///
///   ~/Unity/Hub/Editor/6000.4.1f1/Editor/Unity -quit -batchmode -nographics \
///       -projectPath "&lt;project&gt;" -executeMethod BuildScript.BuildLinux64 -logFile -
///
/// Output goes to &lt;project&gt;/Builds/&lt;Platform&gt;/.
/// </summary>
public static class BuildScript
{
    private const string ProductName = "ShokisAdventure";

    private static string[] ScenePaths =>
        EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

    [MenuItem("Build/Build Linux (x86_64)")]
    public static void BuildLinux64()
    {
        Build(BuildTarget.StandaloneLinux64, "Linux", ProductName + ".x86_64");
    }

    [MenuItem("Build/Build Windows (x86_64)")]
    public static void BuildWindows64()
    {
        Build(BuildTarget.StandaloneWindows64, "Windows", ProductName + ".exe");
    }

    private static void Build(BuildTarget target, string folder, string exeName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outDir       = Path.Combine(projectRoot, "Builds", folder);
        Directory.CreateDirectory(outDir);

        var options = new BuildPlayerOptions
        {
            scenes           = ScenePaths,
            locationPathName = Path.Combine(outDir, exeName),
            target           = target,
            options          = BuildOptions.None
        };

        BuildReport report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"Build succeeded: {summary.totalSize} bytes -> {options.locationPathName}");
        else
            Debug.LogError($"Build failed: {summary.result} ({summary.totalErrors} errors)");
    }
}
