using UnityEditor;
using UnityEngine;

public static class GameplayConfigBootstrapper
{
    private const string SettingsRoot = "Assets/Settings";
    private const string ConfigRoot = "Assets/Settings/Configs";
    private const string RunnerBalancePath = "Assets/Settings/Configs/RunnerBalanceConfig.asset";
    private const string EncounterTuningPath = "Assets/Settings/Configs/EncounterTuningConfig.asset";
    private const string VisualQualityPath = "Assets/Settings/Configs/VisualQualityConfig.asset";

    public readonly struct ConfigBundle
    {
        public ConfigBundle(RunnerBalanceConfig runnerBalance, EncounterTuningConfig encounterTuning, VisualQualityConfig visualQuality)
        {
            RunnerBalance = runnerBalance;
            EncounterTuning = encounterTuning;
            VisualQuality = visualQuality;
        }

        public RunnerBalanceConfig RunnerBalance { get; }
        public EncounterTuningConfig EncounterTuning { get; }
        public VisualQualityConfig VisualQuality { get; }
    }

    public static ConfigBundle EnsureConfigs()
    {
        EnsureFolders();
        RunnerBalanceConfig runnerBalance = EnsureAsset<RunnerBalanceConfig>(RunnerBalancePath);
        EncounterTuningConfig encounterTuning = EnsureAsset<EncounterTuningConfig>(EncounterTuningPath);
        VisualQualityConfig visualQuality = EnsureAsset<VisualQualityConfig>(VisualQualityPath);
        AssetDatabase.SaveAssets();
        return new ConfigBundle(runnerBalance, encounterTuning, visualQuality);
    }

    private static T EnsureAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(SettingsRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        if (!AssetDatabase.IsValidFolder(ConfigRoot))
        {
            AssetDatabase.CreateFolder(SettingsRoot, "Configs");
        }
    }
}
