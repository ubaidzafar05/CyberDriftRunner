using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReleasePipelineBuilder
{
    private static readonly string[] ShipScenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/GameScene.unity",
        "Assets/Scenes/GameOver.unity"
    };

    [MenuItem("Cyber Drift Runner/Release/Configure Android Release")]
    public static void ConfigureAndroidRelease()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
        PlayerSettings.MTRendering = true;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.Android.useCustomKeystore = false;
        if (string.IsNullOrWhiteSpace(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)))
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yourstudio.cyberdrift");
        }
        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        Application.targetFrameRate = 60;
        EditorUserBuildSettings.buildAppBundle = true;
        Debug.Log("[ReleasePipelineBuilder] Android release settings applied.");
    }

    [MenuItem("Cyber Drift Runner/Release/Build Android App Bundle")]
    public static void BuildAndroidAppBundle()
    {
        // REASONING:
        // 1. Release builds must use the exact shipping scene set, not whatever happens to be enabled.
        // 2. Production readiness must fail the build before Unity spends time packaging a broken release.
        // 3. This keeps vendor/demo scenes and incomplete editor experiments out of the ship path.
        ConfigureAndroidRelease();

        ProductionReadinessValidator.ValidationReport readiness = ProductionReadinessValidator.BuildReport();
        if (!readiness.IsValid)
        {
            Debug.LogError($"[ReleasePipelineBuilder] Refusing release build.\n{readiness.Text}");
            return;
        }

        string outputDirectory = Path.Combine("Builds", "Android");
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, "CyberDriftRunner.aab");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = ShipScenes,
            target = BuildTarget.Android,
            locationPathName = outputPath,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[ReleasePipelineBuilder] Build succeeded: {outputPath}");
            return;
        }

        Debug.LogError($"[ReleasePipelineBuilder] Build failed: {report.summary.result}");
    }

    [MenuItem("Cyber Drift Runner/Release/Configure Shipping Build Settings")]
    public static void ConfigureShippingBuildSettings()
    {
        EditorBuildSettingsScene[] configuredScenes = new EditorBuildSettingsScene[ShipScenes.Length];
        for (int i = 0; i < ShipScenes.Length; i++)
        {
            configuredScenes[i] = new EditorBuildSettingsScene(ShipScenes[i], true);
        }

        EditorBuildSettings.scenes = configuredScenes;
        Debug.Log($"[ReleasePipelineBuilder] Shipping build settings applied.\n{BuildSceneSummary()}");
    }

    private static string BuildSceneSummary()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < ShipScenes.Length; i++)
        {
            builder.Append(i + 1)
                .Append(". ")
                .AppendLine(ShipScenes[i]);
        }

        return builder.ToString();
    }
}
