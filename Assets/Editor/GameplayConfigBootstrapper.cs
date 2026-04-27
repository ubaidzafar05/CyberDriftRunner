using UnityEditor;
using UnityEngine;

public static class GameplayConfigBootstrapper
{
    private const string SettingsRoot = "Assets/Settings";
    private const string ConfigRoot = "Assets/Settings/Configs";
    private const string RunnerBalancePath = "Assets/Settings/Configs/RunnerBalanceConfig.asset";
    private const string EncounterTuningPath = "Assets/Settings/Configs/EncounterTuningConfig.asset";
    private const string VisualQualityPath = "Assets/Settings/Configs/VisualQualityConfig.asset";
    private const string DistrictLibraryPath = "Assets/Settings/Configs/DistrictPresentationLibrary.asset";
    private const string UiThemePath = "Assets/Settings/Configs/UiVisualTheme.asset";
    private const string AudioProfilePath = "Assets/Settings/Configs/AudioStyleProfile.asset";
    private const string VisualAssetCatalogPath = "Assets/Settings/Configs/VisualAssetCatalog.asset";
    private const string DistrictProfileRoot = "Assets/Settings/Configs/Districts";
    private const string AudioMusicRoot = "Assets/Audio/Music";
    private const string AudioSfxRoot = "Assets/Audio/SFX";
    private const string PrefabsRoot = "Assets/Prefabs";
    private const string MaterialsRoot = "Assets/Materials";

    public readonly struct ConfigBundle
    {
        public ConfigBundle(
            RunnerBalanceConfig runnerBalance,
            EncounterTuningConfig encounterTuning,
            VisualQualityConfig visualQuality,
            DistrictPresentationLibrary districtPresentation,
            UiVisualTheme uiTheme,
            AudioStyleProfile audioStyle,
            VisualAssetCatalog visualAssets)
        {
            RunnerBalance = runnerBalance;
            EncounterTuning = encounterTuning;
            VisualQuality = visualQuality;
            DistrictPresentation = districtPresentation;
            UiTheme = uiTheme;
            AudioStyle = audioStyle;
            VisualAssets = visualAssets;
        }

        public RunnerBalanceConfig RunnerBalance { get; }
        public EncounterTuningConfig EncounterTuning { get; }
        public VisualQualityConfig VisualQuality { get; }
        public DistrictPresentationLibrary DistrictPresentation { get; }
        public UiVisualTheme UiTheme { get; }
        public AudioStyleProfile AudioStyle { get; }
        public VisualAssetCatalog VisualAssets { get; }
    }

    public static ConfigBundle EnsureConfigs()
    {
        EnsureFolders();
        RunnerBalanceConfig runnerBalance = EnsureAsset<RunnerBalanceConfig>(RunnerBalancePath);
        EncounterTuningConfig encounterTuning = EnsureAsset<EncounterTuningConfig>(EncounterTuningPath);
        VisualQualityConfig visualQuality = EnsureAsset<VisualQualityConfig>(VisualQualityPath);
        UiVisualTheme uiTheme = EnsureAsset<UiVisualTheme>(UiThemePath);
        AudioStyleProfile audioStyle = EnsureAsset<AudioStyleProfile>(AudioProfilePath);
        VisualAssetCatalog visualAssets = EnsureAsset<VisualAssetCatalog>(VisualAssetCatalogPath);
        DistrictPresentationProfile[] districtProfiles = EnsureDistrictProfiles();
        DistrictPresentationLibrary districtPresentation = EnsureAsset<DistrictPresentationLibrary>(DistrictLibraryPath);
        ConfigureDistrictLibrary(districtPresentation, districtProfiles);
        ConfigureAudioProfile(audioStyle);
        ConfigureVisualCatalog(visualAssets);
        AssetDatabase.SaveAssets();
        return new ConfigBundle(runnerBalance, encounterTuning, visualQuality, districtPresentation, uiTheme, audioStyle, visualAssets);
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

        if (!AssetDatabase.IsValidFolder(DistrictProfileRoot))
        {
            AssetDatabase.CreateFolder(ConfigRoot, "Districts");
        }
    }

    private static DistrictPresentationProfile[] EnsureDistrictProfiles()
    {
        return new[]
        {
            EnsureDistrictProfile(
                "NeonGatewayPresentation.asset",
                0,
                "Neon Gateway",
                new Color(0.01f, 0.015f, 0.04f),
                new Color(0f, 0.96f, 1f),
                new Color(1f, 0f, 0.78f),
                new Color(0.42f, 0f, 1f)),
            EnsureDistrictProfile(
                "MarketStripPresentation.asset",
                1,
                "Market Strip",
                new Color(0.03f, 0.01f, 0.04f),
                new Color(1f, 0.84f, 0.24f),
                new Color(1f, 0.42f, 0.76f),
                new Color(0.3f, 1f, 0.8f)),
            EnsureDistrictProfile(
                "SecurityCorridorPresentation.asset",
                2,
                "Security Corridor",
                new Color(0.01f, 0.012f, 0.05f),
                new Color(0.42f, 0f, 1f),
                new Color(0.16f, 0.84f, 1f),
                new Color(1f, 0.3f, 0.48f)),
            EnsureDistrictProfile(
                "CitadelApproachPresentation.asset",
                3,
                "Citadel Approach",
                new Color(0.01f, 0.01f, 0.03f),
                new Color(1f, 0.96f, 0.96f),
                new Color(1f, 0.3f, 0.48f),
                new Color(0.26f, 0.94f, 1f))
        };
    }

    private static DistrictPresentationProfile EnsureDistrictProfile(
        string fileName,
        int districtIndex,
        string districtName,
        Color background,
        Color primary,
        Color secondary,
        Color tertiary)
    {
        string assetPath = $"{DistrictProfileRoot}/{fileName}";
        DistrictPresentationProfile profile = EnsureAsset<DistrictPresentationProfile>(assetPath);
        SerializedObject serializedObject = new SerializedObject(profile);
        serializedObject.FindProperty("districtIndex").intValue = districtIndex;
        serializedObject.FindProperty("districtName").stringValue = districtName;
        serializedObject.FindProperty("cameraBackground").colorValue = background;
        serializedObject.FindProperty("ambientColor").colorValue = Color.Lerp(background, primary, districtIndex == 3 ? 0.18f : 0.14f);
        serializedObject.FindProperty("fogColor").colorValue = Color.Lerp(background, secondary, districtIndex == 2 ? 0.26f : 0.2f);
        serializedObject.FindProperty("fogStartDistance").floatValue = districtIndex switch
        {
            2 => 16f,
            3 => 24f,
            _ => 18f
        };
        serializedObject.FindProperty("fogEndDistance").floatValue = districtIndex switch
        {
            0 => 124f,
            1 => 120f,
            2 => 110f,
            _ => 136f
        };
        serializedObject.FindProperty("keyLightColor").colorValue = Color.Lerp(primary, Color.white, districtIndex == 3 ? 0.52f : 0.42f);
        serializedObject.FindProperty("keyLightIntensity").floatValue = districtIndex switch
        {
            1 => 1.16f,
            2 => 1.12f,
            3 => 1.34f,
            _ => 1.26f
        };
        serializedObject.FindProperty("fillLightColor").colorValue = Color.Lerp(secondary, tertiary, districtIndex == 2 ? 0.58f : 0.4f);
        serializedObject.FindProperty("fillLightIntensity").floatValue = districtIndex switch
        {
            2 => 0.52f,
            3 => 0.46f,
            _ => 0.42f
        };
        serializedObject.FindProperty("primaryAccent").colorValue = primary;
        serializedObject.FindProperty("secondaryAccent").colorValue = secondary;
        serializedObject.FindProperty("tertiaryAccent").colorValue = tertiary;
        serializedObject.FindProperty("bloomIntensityMultiplier").floatValue = districtIndex switch
        {
            0 => 1.08f,
            1 => 1.12f,
            2 => 1.16f,
            _ => 1.2f
        };
        serializedObject.FindProperty("vignetteIntensityMultiplier").floatValue = districtIndex switch
        {
            2 => 1.16f,
            3 => 1.1f,
            _ => 1f
        };
        serializedObject.FindProperty("chromaticBoost").floatValue = districtIndex switch
        {
            2 => 0.035f,
            3 => 0.025f,
            _ => 0.012f
        };
        serializedObject.FindProperty("contrastBias").floatValue = districtIndex switch
        {
            1 => 4f,
            2 => 6f,
            3 => 8f,
            _ => 2f
        };
        serializedObject.FindProperty("saturationBias").floatValue = districtIndex switch
        {
            3 => -2f,
            _ => 6f
        };
        serializedObject.FindProperty("exposureBias").floatValue = districtIndex switch
        {
            0 => 0.08f,
            1 => 0.02f,
            2 => -0.03f,
            _ => -0.04f
        };
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ConfigureDistrictLibrary(DistrictPresentationLibrary library, DistrictPresentationProfile[] profiles)
    {
        SerializedObject serializedObject = new SerializedObject(library);
        SerializedProperty property = serializedObject.FindProperty("profiles");
        property.arraySize = profiles.Length;
        for (int i = 0; i < profiles.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
    }

    private static void ConfigureAudioProfile(AudioStyleProfile profile)
    {
        SerializedObject serializedObject = new SerializedObject(profile);
        AssignAudioClip(serializedObject, "menuLoop", $"{AudioMusicRoot}/menu_loop.wav");
        AssignAudioClip(serializedObject, "gameplayLoop", $"{AudioMusicRoot}/gameplay_loop.wav");
        AssignAudioClip(serializedObject, "bossLoop", $"{AudioMusicRoot}/boss_loop.wav");
        AssignAudioClip(serializedObject, "jumpClip", $"{AudioSfxRoot}/jump.wav");
        AssignAudioClip(serializedObject, "slideClip", $"{AudioSfxRoot}/slide.wav");
        AssignAudioClip(serializedObject, "shootClip", $"{AudioSfxRoot}/shoot.wav");
        AssignAudioClip(serializedObject, "hitClip", $"{AudioSfxRoot}/hit.wav");
        AssignAudioClip(serializedObject, "powerUpClip", $"{AudioSfxRoot}/power_up.wav");
        AssignAudioClip(serializedObject, "hackClip", $"{AudioSfxRoot}/hack.wav");
        AssignAudioClip(serializedObject, "reviveClip", $"{AudioSfxRoot}/revive.wav");
        AssignAudioClip(serializedObject, "bossDefeatClip", $"{AudioSfxRoot}/boss_defeat.wav");
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
    }

    private static void AssignAudioClip(SerializedObject serializedObject, string propertyName, string assetPath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = clip;
        }
    }

    private static void ConfigureVisualCatalog(VisualAssetCatalog catalog)
    {
        SerializedObject serializedObject = new SerializedObject(catalog);
        AssignPrefab(serializedObject, "playerPrefab", $"{PrefabsRoot}/Player.prefab");
        AssignPrefab(serializedObject, "projectilePrefab", $"{PrefabsRoot}/Projectile.prefab");
        AssignPrefab(serializedObject, "barrierPrefab", $"{PrefabsRoot}/Barrier.prefab");
        AssignPrefab(serializedObject, "carPrefab", $"{PrefabsRoot}/CarObstacle.prefab");
        AssignPrefab(serializedObject, "dronePrefab", $"{PrefabsRoot}/Drone.prefab");
        AssignPrefab(serializedObject, "bossPrefab", $"{PrefabsRoot}/BossDrone.prefab");
        AssignPrefab(serializedObject, "bossHazardPrefab", $"{PrefabsRoot}/BossLaneHazard.prefab");
        AssignPrefab(serializedObject, "bossStagePrefab", $"{PrefabsRoot}/BossStage.prefab");
        AssignPrefab(serializedObject, "creditPrefab", $"{PrefabsRoot}/Credit.prefab");

        AssignPrefabArray(serializedObject, "powerUpPrefabs", new[]
        {
            $"{PrefabsRoot}/ShieldPowerUp.prefab",
            $"{PrefabsRoot}/EmpPowerUp.prefab",
            $"{PrefabsRoot}/DoubleScorePowerUp.prefab",
            $"{PrefabsRoot}/SpeedBoostPowerUp.prefab",
            $"{PrefabsRoot}/MagnetPowerUp.prefab",
            $"{PrefabsRoot}/SlowMotionPowerUp.prefab"
        });

        AssignPrefabArray(serializedObject, "gatewayChunks", new[]
        {
            $"{PrefabsRoot}/Chunk_Gateway_A.prefab",
            $"{PrefabsRoot}/Chunk_Gateway_B.prefab",
            $"{PrefabsRoot}/Chunk_Gateway_C.prefab"
        });

        AssignPrefabArray(serializedObject, "commerceChunks", new[]
        {
            $"{PrefabsRoot}/Chunk_Commerce_A.prefab",
            $"{PrefabsRoot}/Chunk_Commerce_B.prefab",
            $"{PrefabsRoot}/Chunk_Commerce_C.prefab"
        });

        AssignPrefabArray(serializedObject, "securityChunks", new[]
        {
            $"{PrefabsRoot}/Chunk_Security_A.prefab",
            $"{PrefabsRoot}/Chunk_Security_B.prefab",
            $"{PrefabsRoot}/Chunk_Security_C.prefab"
        });

        AssignMaterial(serializedObject, "roadMaterial", $"{MaterialsRoot}/RoadDark.mat");
        AssignMaterial(serializedObject, "accentMaterial", $"{MaterialsRoot}/NeonCyan.mat");
        AssignMaterial(serializedObject, "alternateAccentMaterial", $"{MaterialsRoot}/NeonMagenta.mat");
        AssignMaterial(serializedObject, "tertiaryAccentMaterial", $"{MaterialsRoot}/NeonViolet.mat");
        AssignMaterial(serializedObject, "warningMaterial", $"{MaterialsRoot}/NeonGold.mat");
        SerializedProperty fallbackProperty = serializedObject.FindProperty("allowGeneratedFallbacks");
        if (fallbackProperty != null)
        {
            fallbackProperty.boolValue = false;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void AssignPrefab(SerializedObject serializedObject, string propertyName, string assetPath)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    private static void AssignPrefabArray(SerializedObject serializedObject, string propertyName, string[] assetPaths)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = assetPaths.Length;
        for (int i = 0; i < assetPaths.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i]);
        }
    }

    private static void AssignMaterial(SerializedObject serializedObject, string propertyName, string assetPath)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
    }
}
