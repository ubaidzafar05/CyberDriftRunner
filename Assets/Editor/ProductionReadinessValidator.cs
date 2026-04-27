using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class ProductionReadinessValidator
{
    private const string ScenesRoot = "Assets/Scenes";
    private static readonly string[] ShipScenePaths =
    {
        $"{ScenesRoot}/{SceneNames.MainMenu}.unity",
        $"{ScenesRoot}/{SceneNames.GameScene}.unity",
        $"{ScenesRoot}/{SceneNames.GameOver}.unity"
    };

    [MenuItem("Cyber Drift Runner/Validate Production Readiness")]
    public static void ValidateProductionReadiness()
    {
        ValidationReport report = BuildReport();
        if (report.IsValid)
        {
            Debug.Log(report.Text);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Production Readiness", report.Text, "OK");
            }
            return;
        }

        Debug.LogError(report.Text);
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Production Readiness", report.Text, "OK");
        }
    }

    public static ValidationReport BuildReport()
    {
        GameplayConfigBootstrapper.ConfigBundle configs = GameplayConfigBootstrapper.EnsureConfigs();
        List<string> issues = new List<string>();
        ValidateVisualCatalog(configs.VisualAssets, issues);
        ValidateAudioProfile(configs.AudioStyle, issues);
        ValidateDistrictProfiles(configs.DistrictPresentation, issues);
        ValidateUiTheme(configs.UiTheme, issues);
        ValidateBuildSettings(issues);
        ValidateScenes(issues);
        return CreateReport(issues);
    }

    public readonly struct ValidationReport
    {
        public ValidationReport(bool isValid, string text)
        {
            IsValid = isValid;
            Text = text;
        }

        public bool IsValid { get; }
        public string Text { get; }
    }

    private static ValidationReport CreateReport(List<string> issues)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Cyber Drift Runner production readiness");
        if (issues.Count == 0)
        {
            builder.Append("Result: ready for production validation.");
            return new ValidationReport(true, builder.ToString());
        }

        builder.AppendLine("Result: not ready.");
        for (int i = 0; i < issues.Count; i++)
        {
            builder.Append("- ").AppendLine(issues[i]);
        }

        return new ValidationReport(false, builder.ToString());
    }

    private static void ValidateVisualCatalog(VisualAssetCatalog catalog, List<string> issues)
    {
        ProductionAssetValidator.ValidationResult result = ProductionAssetValidator.Validate(catalog);
        if (!result.IsValid)
        {
            issues.Add("Visual asset catalog is incomplete.");
            return;
        }

        if (catalog == null)
        {
            issues.Add("VisualAssetCatalog is missing.");
            return;
        }

        if (!catalog.RequireAuthoredAssets)
        {
            issues.Add("VisualAssetCatalog must run in authored-only mode for the shipping configuration.");
        }
    }

    private static void ValidateAudioProfile(AudioStyleProfile profile, List<string> issues)
    {
        if (profile == null)
        {
            issues.Add("AudioStyleProfile is missing.");
            return;
        }

        ValidateAudioClip(profile.MenuLoop, "AudioStyleProfile.MenuLoop", issues);
        ValidateAudioClip(profile.GameplayLoop, "AudioStyleProfile.GameplayLoop", issues);
        ValidateAudioClip(profile.BossLoop, "AudioStyleProfile.BossLoop", issues);
        ValidateAudioClip(profile.JumpClip, "AudioStyleProfile.JumpClip", issues);
        ValidateAudioClip(profile.SlideClip, "AudioStyleProfile.SlideClip", issues);
        ValidateAudioClip(profile.ShootClip, "AudioStyleProfile.ShootClip", issues);
        ValidateAudioClip(profile.HitClip, "AudioStyleProfile.HitClip", issues);
        ValidateAudioClip(profile.PowerUpClip, "AudioStyleProfile.PowerUpClip", issues);
        ValidateAudioClip(profile.HackClip, "AudioStyleProfile.HackClip", issues);
        ValidateAudioClip(profile.ReviveClip, "AudioStyleProfile.ReviveClip", issues);
        ValidateAudioClip(profile.BossDefeatClip, "AudioStyleProfile.BossDefeatClip", issues);
    }

    private static void ValidateAudioClip(AudioClip clip, string label, List<string> issues)
    {
        if (clip == null)
        {
            issues.Add($"{label} is missing.");
        }
    }

    private static void ValidateDistrictProfiles(DistrictPresentationLibrary library, List<string> issues)
    {
        if (library == null)
        {
            issues.Add("DistrictPresentationLibrary is missing.");
            return;
        }

        DistrictPresentationProfile[] profiles = library.Profiles;
        for (int index = 0; index < 4; index++)
        {
            if (!HasDistrictProfile(profiles, index))
            {
                RunDistrictCatalog.DistrictInfo district = RunDistrictCatalog.GetByIndex(index);
                issues.Add($"DistrictPresentationLibrary is missing profile for district {index} ({district.Name}).");
            }
        }
    }

    private static bool HasDistrictProfile(DistrictPresentationProfile[] profiles, int districtIndex)
    {
        if (profiles == null)
        {
            return false;
        }

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] != null && profiles[i].DistrictIndex == districtIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateUiTheme(UiVisualTheme theme, List<string> issues)
    {
        if (theme == null)
        {
            issues.Add("UiVisualTheme is missing.");
            return;
        }

        if (theme.PanelFill.a <= 0f)
        {
            issues.Add("UiVisualTheme.PanelFill has zero alpha.");
        }

        if (theme.TitleText.a <= 0f)
        {
            issues.Add("UiVisualTheme.TitleText has zero alpha.");
        }
    }

    private static void ValidateBuildSettings(List<string> issues)
    {
        EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
        if (configuredScenes == null || configuredScenes.Length != ShipScenePaths.Length)
        {
            issues.Add("Build Settings must contain exactly the three shipping scenes.");
            return;
        }

        for (int i = 0; i < ShipScenePaths.Length; i++)
        {
            if (!configuredScenes[i].enabled || configuredScenes[i].path != ShipScenePaths[i])
            {
                issues.Add("Build Settings are not aligned to the shipping scene set.");
                return;
            }
        }
    }

    private static void ValidateScenes(List<string> issues)
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ValidateMainMenuScene(issues);
            ValidateGameScene(issues);
            ValidateGameOverScene(issues);
        }
        finally
        {
            if (previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }

    private static void ValidateMainMenuScene(List<string> issues)
    {
        Scene scene = OpenScene(SceneNames.MainMenu, issues);
        if (!scene.IsValid())
        {
            return;
        }

        RequireComponent<UIFlowController>(scene, "MainMenu", issues);
        RequireComponent<MainMenuController>(scene, "MainMenu", issues);
        RequireComponent<EventSystem>(scene, "MainMenu", issues);
        ValidateReference<UIFlowController>(scene, "menuCanvas", "MainMenu UIFlowController.menuCanvas", issues);
        ValidateReference<UIFlowController>(scene, "visualTheme", "MainMenu UIFlowController.visualTheme", issues);
    }

    private static void ValidateGameScene(List<string> issues)
    {
        Scene scene = OpenScene(SceneNames.GameScene, issues);
        if (!scene.IsValid())
        {
            return;
        }

        RequireComponent<GameManager>(scene, "GameScene", issues);
        RequireComponent<PresentationDirector>(scene, "GameScene", issues);
        RequireComponent<UIFlowController>(scene, "GameScene", issues);
        RequireComponent<ObstacleSpawner>(scene, "GameScene", issues);
        RequireComponent<LevelChunkGenerator>(scene, "GameScene", issues);
        RequireComponent<BossEncounterManager>(scene, "GameScene", issues);
        RequireComponent<ReviveOverlayController>(scene, "GameScene", issues);
        RequireComponent<HUDController>(scene, "GameScene", issues);
        RequireComponent<EventSystem>(scene, "GameScene", issues);
        ValidateReference<PresentationDirector>(scene, "districtLibrary", "GameScene PresentationDirector.districtLibrary", issues);
        ValidateReference<PresentationDirector>(scene, "postProcessingConfig", "GameScene PresentationDirector.postProcessingConfig", issues);
        ValidateReference<UIFlowController>(scene, "hudCanvas", "GameScene UIFlowController.hudCanvas", issues);
        ValidateReference<UIFlowController>(scene, "pausePanel", "GameScene UIFlowController.pausePanel", issues);
        ValidateReference<UIFlowController>(scene, "revivePanel", "GameScene UIFlowController.revivePanel", issues);
        ValidateReference<UIFlowController>(scene, "visualTheme", "GameScene UIFlowController.visualTheme", issues);
    }

    private static void ValidateGameOverScene(List<string> issues)
    {
        Scene scene = OpenScene(SceneNames.GameOver, issues);
        if (!scene.IsValid())
        {
            return;
        }

        RequireComponent<UIFlowController>(scene, "GameOver", issues);
        RequireComponent<GameOverController>(scene, "GameOver", issues);
        RequireComponent<EventSystem>(scene, "GameOver", issues);
        ValidateReference<UIFlowController>(scene, "gameOverCanvas", "GameOver UIFlowController.gameOverCanvas", issues);
        ValidateReference<UIFlowController>(scene, "visualTheme", "GameOver UIFlowController.visualTheme", issues);
        ValidateReference<GameOverController>(scene, "scoreText", "GameOver GameOverController.scoreText", issues);
        ValidateReference<GameOverController>(scene, "distanceText", "GameOver GameOverController.distanceText", issues);
        ValidateReference<GameOverController>(scene, "creditsText", "GameOver GameOverController.creditsText", issues);
        ValidateReference<GameOverController>(scene, "survivalText", "GameOver GameOverController.survivalText", issues);
    }

    private static Scene OpenScene(string sceneName, List<string> issues)
    {
        string path = $"{ScenesRoot}/{sceneName}.unity";
        if (!System.IO.File.Exists(path))
        {
            issues.Add($"Scene file is missing: {path}");
            return default;
        }

        return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static void RequireComponent<T>(Scene scene, string sceneLabel, List<string> issues) where T : Component
    {
        if (FindComponentInScene<T>(scene) == null)
        {
            issues.Add($"{sceneLabel} is missing component {typeof(T).Name}.");
        }
    }

    private static void ValidateReference<T>(Scene scene, string propertyName, string label, List<string> issues) where T : Component
    {
        T component = FindComponentInScene<T>(scene);
        if (component == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(component);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null)
        {
            return;
        }

        issues.Add($"{label} is missing.");
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
