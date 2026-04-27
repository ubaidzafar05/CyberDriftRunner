using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public static class SceneBootstrapper
{
    private const string AssetsRoot = "Assets";
    private const string ScenesRoot = "Assets/Scenes";
    private const string PrefabsRoot = "Assets/Prefabs";
    private const string MaterialsRoot = "Assets/Materials";
    private static UiVisualTheme s_uiTheme;

    [MenuItem("Cyber Drift Runner/Create Demo Scenes and Prefabs")]
    public static void CreateDemoScenesAndPrefabs()
    {
        EnsureFolder(AssetsRoot, "Scenes");
        EnsureFolder(AssetsRoot, "Prefabs");
        EnsureFolder(AssetsRoot, "Materials");
        RenderPipelineBootstrapper.EnsurePipelineAsset();
        GameplayConfigBootstrapper.ConfigBundle configs = GameplayConfigBootstrapper.EnsureConfigs();
        if (configs.VisualAssets != null && configs.VisualAssets.RequireAuthoredAssets)
        {
            ProductionAssetValidator.ValidationResult validation = ProductionAssetValidator.Validate(configs.VisualAssets);
            if (!validation.IsValid)
            {
                Debug.LogError(validation.Report);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Cyber Drift Runner", validation.Report, "OK");
                }

                return;
            }
        }

        s_uiTheme = configs.UiTheme;
        try
        {
            BootstrapAssets assets = CreateBootstrapAssets(configs);
            ValidateBootstrapAssets(assets, configs.VisualAssets);
            CreateMainMenuScene(assets, configs);
            CreateGameScene(assets, configs);
            CreateGameOverScene(assets, configs);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene($"{ScenesRoot}/{SceneNames.MainMenu}.unity");
            ProductionReadinessValidator.ValidationReport readiness = ProductionReadinessValidator.BuildReport();
            Debug.Log(readiness.Text);
            if (!Application.isBatchMode)
            {
                string message = readiness.IsValid
                    ? "Scenes and prefabs are ready. Open MainMenu and press Play."
                    : "Scenes and prefabs were generated, but the project is not production-ready yet. Run the production readiness report in the Console.";
                EditorUtility.DisplayDialog("Cyber Drift Runner", message, "OK");
            }
        }
        finally
        {
            s_uiTheme = null;
        }
    }

    private static BootstrapAssets CreateBootstrapAssets(GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Color neonCyan = new Color(0f, 0.96f, 1f);
        Color neonMagenta = new Color(1f, 0f, 0.78f);
        Color neonViolet = new Color(0.42f, 0f, 1f);
        Material neonBlue = CreateMaterial("NeonCyan", new Color(0.04f, 0.16f, 0.2f), neonCyan);
        Material neonPink = CreateMaterial("NeonMagenta", new Color(0.18f, 0.03f, 0.16f), neonMagenta);
        Material neonPurple = CreateMaterial("NeonViolet", new Color(0.1f, 0.04f, 0.22f), neonViolet);
        Material neonYellow = CreateMaterial("NeonGold", new Color(0.28f, 0.2f, 0.05f), new Color(1f, 0.8f, 0.18f));
        Material roadMaterial = CreateMaterial("RoadDark", new Color(0.04f, 0.05f, 0.09f), new Color(0f, 0.96f, 1f) * 0.1f);

        BootstrapAssets assets = new BootstrapAssets
        {
            Projectile = CreateProjectilePrefab(neonBlue),
            Barrier = CreateBarrierPrefab(neonPink),
            Car = CreateCarPrefab(neonYellow),
            Drone = CreateDronePrefab(neonPink),
            Boss = CreateBossPrefab(neonPink, neonBlue, neonPurple),
            BossHazard = CreateBossHazardPrefab(neonPink, neonBlue),
            Credit = CreateCreditPrefab(neonYellow),
            RoadMaterial = roadMaterial,
            AccentMaterial = neonBlue,
            AlternateAccentMaterial = neonPink,
            TertiaryAccentMaterial = neonPurple,
            WarningMaterial = neonYellow
        };

        assets.Player = CreatePlayerPrefab(neonBlue, assets.Projectile);
        assets.PowerUps = new[]
        {
            CreatePowerUpPrefab("ShieldPowerUp", PrimitiveType.Capsule, neonBlue, PowerUpType.Shield, 6f),
            CreatePowerUpPrefab("DoubleScorePowerUp", PrimitiveType.Cube, neonPink, PowerUpType.DoubleScore, 8f),
            CreatePowerUpPrefab("SlowMotionPowerUp", PrimitiveType.Sphere, neonYellow, PowerUpType.SlowMotion, 5f),
            CreatePowerUpPrefab("EmpPowerUp", PrimitiveType.Cylinder, neonPink, PowerUpType.EmpBlast, 0f),
            CreatePowerUpPrefab("MagnetPowerUp", PrimitiveType.Sphere, neonBlue, PowerUpType.Magnet, 7f),
            CreatePowerUpPrefab("SpeedBoostPowerUp", PrimitiveType.Capsule, neonYellow, PowerUpType.SpeedBoost, 5f)
        };

        assets.GatewayChunks = new[]
        {
            CreateChunkPrefab("Chunk_Gateway_A.prefab", roadMaterial, neonBlue, neonPink, neonPurple, neonYellow, ChunkVisualStyle.Gateway),
            CreateChunkPrefab("Chunk_Gateway_B.prefab", roadMaterial, neonBlue, neonPink, neonPurple, neonYellow, ChunkVisualStyle.Billboard),
            CreateChunkPrefab("Chunk_Gateway_C.prefab", roadMaterial, neonBlue, neonPink, neonPurple, neonYellow, ChunkVisualStyle.Plaza)
        };
        assets.CommerceChunks = new[]
        {
            CreateChunkPrefab("Chunk_Commerce_A.prefab", roadMaterial, neonPink, neonBlue, neonPurple, neonYellow, ChunkVisualStyle.Billboard),
            CreateChunkPrefab("Chunk_Commerce_B.prefab", roadMaterial, neonPink, neonYellow, neonBlue, neonPurple, ChunkVisualStyle.Bridge),
            CreateChunkPrefab("Chunk_Commerce_C.prefab", roadMaterial, neonPink, neonYellow, neonBlue, neonPurple, ChunkVisualStyle.Transit)
        };
        assets.SecurityChunks = new[]
        {
            CreateChunkPrefab("Chunk_Security_A.prefab", roadMaterial, neonPurple, neonBlue, neonPink, neonYellow, ChunkVisualStyle.Security),
            CreateChunkPrefab("Chunk_Security_B.prefab", roadMaterial, neonPurple, neonPink, neonBlue, neonYellow, ChunkVisualStyle.Tunnel),
            CreateChunkPrefab("Chunk_Security_C.prefab", roadMaterial, neonPurple, neonPink, neonBlue, neonYellow, ChunkVisualStyle.Citadel)
        };

        assets.BossStage = CreateBossStagePrefab("BossStage.prefab", roadMaterial, neonBlue, neonPink, neonPurple, neonYellow);
        return ApplyCatalogOverrides(assets, configs.VisualAssets);
    }

    private static GameObject CreatePlayerPrefab(Material material, GameObject projectilePrefab)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Player";
        root.transform.position = Vector3.up;
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());
        root.GetComponent<Renderer>().sharedMaterial = CreateMaterial("HeroSuitBlack", new Color(0.03f, 0.04f, 0.06f), new Color(0.04f, 0.04f, 0.06f));

        Material heroShell = CreateMaterial("HeroSuitBlack", new Color(0.03f, 0.04f, 0.06f), new Color(0.04f, 0.04f, 0.06f));
        Material heroTrim = CreateMaterial("HeroTrimAmber", new Color(0.18f, 0.11f, 0.03f), new Color(1f, 0.72f, 0.18f));
        Material heroVisor = CreateMaterial("HeroVisorGlass", new Color(0.06f, 0.14f, 0.18f), new Color(0f, 0.96f, 1f));
        Material heroAccent = CreateMaterial("HeroAccentPink", new Color(0.18f, 0.04f, 0.16f), new Color(1f, 0f, 0.78f));
        Material heroAccentPurple = CreateMaterial("HeroAccentPurple", new Color(0.1f, 0.04f, 0.22f), new Color(0.42f, 0f, 1f));

        CreateVisualPrimitive("TorsoPlate", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0.12f), new Vector3(0.6f, 0.74f, 0.34f), heroShell, root.transform);
        CreateVisualPrimitive("HipRig", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0.05f), new Vector3(0.66f, 0.24f, 0.3f), heroShell, root.transform);
        CreateVisualPrimitive("ShoulderLeft", PrimitiveType.Cube, new Vector3(-0.33f, 0.88f, 0.04f), new Vector3(0.18f, 0.16f, 0.26f), heroShell, root.transform);
        CreateVisualPrimitive("ShoulderRight", PrimitiveType.Cube, new Vector3(0.33f, 0.88f, 0.04f), new Vector3(0.18f, 0.16f, 0.26f), heroShell, root.transform);
        CreateVisualPrimitive("CoatCollarLeft", PrimitiveType.Cube, new Vector3(-0.18f, 1.02f, 0.02f), new Vector3(0.16f, 0.2f, 0.24f), heroTrim, root.transform);
        CreateVisualPrimitive("CoatCollarRight", PrimitiveType.Cube, new Vector3(0.18f, 1.02f, 0.02f), new Vector3(0.16f, 0.2f, 0.24f), heroTrim, root.transform);
        CreateVisualPrimitive("SpineLight", PrimitiveType.Cube, new Vector3(0f, 0.68f, -0.19f), new Vector3(0.08f, 0.92f, 0.08f), heroAccent, root.transform);
        CreateVisualPrimitive("BackpackCore", PrimitiveType.Cube, new Vector3(0f, 0.58f, -0.27f), new Vector3(0.44f, 0.8f, 0.16f), heroShell, root.transform);
        CreateVisualPrimitive("ChestCore", PrimitiveType.Cube, new Vector3(0f, 0.7f, 0.3f), new Vector3(0.24f, 0.46f, 0.09f), heroTrim, root.transform);
        CreateVisualPrimitive("ChestSideLeft", PrimitiveType.Cube, new Vector3(-0.18f, 0.7f, 0.26f), new Vector3(0.07f, 0.42f, 0.08f), material, root.transform);
        CreateVisualPrimitive("ChestSideRight", PrimitiveType.Cube, new Vector3(0.18f, 0.7f, 0.26f), new Vector3(0.07f, 0.42f, 0.08f), material, root.transform);
        CreateVisualPrimitive("VisorBand", PrimitiveType.Cube, new Vector3(0f, 1.17f, 0.22f), new Vector3(0.66f, 0.16f, 0.3f), heroVisor, root.transform);
        CreateVisualPrimitive("VisorGlow", PrimitiveType.Cube, new Vector3(0f, 1.17f, 0.39f), new Vector3(0.52f, 0.08f, 0.06f), material, root.transform);
        CreateVisualPrimitive("VisorWingLeft", PrimitiveType.Cube, new Vector3(-0.4f, 1.17f, 0.18f), new Vector3(0.15f, 0.08f, 0.22f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("VisorWingRight", PrimitiveType.Cube, new Vector3(0.4f, 1.17f, 0.18f), new Vector3(0.15f, 0.08f, 0.22f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("AntennaLeft", PrimitiveType.Cube, new Vector3(-0.22f, 1.42f, 0.02f), new Vector3(0.05f, 0.28f, 0.05f), heroTrim, root.transform);
        CreateVisualPrimitive("AntennaRight", PrimitiveType.Cube, new Vector3(0.22f, 1.42f, 0.02f), new Vector3(0.05f, 0.28f, 0.05f), heroTrim, root.transform);
        CreateVisualPrimitive("ArmBladeLeft", PrimitiveType.Cube, new Vector3(-0.5f, 0.52f, 0.12f), new Vector3(0.09f, 0.55f, 0.11f), material, root.transform);
        CreateVisualPrimitive("ArmBladeRight", PrimitiveType.Cube, new Vector3(0.5f, 0.52f, 0.12f), new Vector3(0.09f, 0.55f, 0.11f), material, root.transform);
        CreateVisualPrimitive("PauldronLeft", PrimitiveType.Cube, new Vector3(-0.34f, 0.98f, -0.02f), new Vector3(0.24f, 0.08f, 0.34f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("PauldronRight", PrimitiveType.Cube, new Vector3(0.34f, 0.98f, -0.02f), new Vector3(0.24f, 0.08f, 0.34f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("ForearmGlowLeft", PrimitiveType.Cube, new Vector3(-0.44f, 0.32f, 0.2f), new Vector3(0.05f, 0.36f, 0.06f), heroTrim, root.transform);
        CreateVisualPrimitive("ForearmGlowRight", PrimitiveType.Cube, new Vector3(0.44f, 0.32f, 0.2f), new Vector3(0.05f, 0.36f, 0.06f), heroTrim, root.transform);
        CreateVisualPrimitive("HipThrusterLeft", PrimitiveType.Cube, new Vector3(-0.34f, 0.14f, -0.18f), new Vector3(0.12f, 0.18f, 0.18f), heroAccent, root.transform);
        CreateVisualPrimitive("HipThrusterRight", PrimitiveType.Cube, new Vector3(0.34f, 0.14f, -0.18f), new Vector3(0.12f, 0.18f, 0.18f), heroAccent, root.transform);
        CreateVisualPrimitive("LegLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.34f, 0.04f), new Vector3(0.18f, 0.96f, 0.18f), heroShell, root.transform);
        CreateVisualPrimitive("LegRight", PrimitiveType.Cube, new Vector3(0.16f, -0.34f, 0.04f), new Vector3(0.18f, 0.96f, 0.18f), heroShell, root.transform);
        CreateVisualPrimitive("ThighLineLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.1f, 0.14f), new Vector3(0.07f, 0.74f, 0.05f), heroAccent, root.transform);
        CreateVisualPrimitive("ThighLineRight", PrimitiveType.Cube, new Vector3(0.16f, -0.1f, 0.14f), new Vector3(0.07f, 0.74f, 0.05f), heroAccent, root.transform);
        CreateVisualPrimitive("KneeGuardLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.42f, 0.18f), new Vector3(0.2f, 0.16f, 0.1f), heroTrim, root.transform);
        CreateVisualPrimitive("KneeGuardRight", PrimitiveType.Cube, new Vector3(0.16f, -0.42f, 0.18f), new Vector3(0.2f, 0.16f, 0.1f), heroTrim, root.transform);
        CreateVisualPrimitive("BootLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.9f, 0.2f), new Vector3(0.22f, 0.18f, 0.38f), heroTrim, root.transform);
        CreateVisualPrimitive("BootRight", PrimitiveType.Cube, new Vector3(0.16f, -0.9f, 0.2f), new Vector3(0.22f, 0.18f, 0.38f), heroTrim, root.transform);
        CreateVisualPrimitive("HeelLightLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.96f, -0.04f), new Vector3(0.16f, 0.06f, 0.08f), heroAccent, root.transform);
        CreateVisualPrimitive("HeelLightRight", PrimitiveType.Cube, new Vector3(0.16f, -0.96f, -0.04f), new Vector3(0.16f, 0.06f, 0.08f), heroAccent, root.transform);
        CreateVisualPrimitive("AnkleBladeLeft", PrimitiveType.Cube, new Vector3(-0.28f, -0.88f, 0.08f), new Vector3(0.08f, 0.18f, 0.12f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("AnkleBladeRight", PrimitiveType.Cube, new Vector3(0.28f, -0.88f, 0.08f), new Vector3(0.08f, 0.18f, 0.12f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("CoatTailLeft", PrimitiveType.Cube, new Vector3(-0.18f, -0.04f, -0.24f), new Vector3(0.16f, 0.74f, 0.06f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("CoatTailRight", PrimitiveType.Cube, new Vector3(0.18f, -0.04f, -0.24f), new Vector3(0.16f, 0.74f, 0.06f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("UtilityPackTop", PrimitiveType.Cube, new Vector3(0f, 1.04f, -0.16f), new Vector3(0.34f, 0.18f, 0.18f), heroTrim, root.transform);
        CreateVisualPrimitive("SignalBladeLeft", PrimitiveType.Cube, new Vector3(-0.42f, 1.18f, -0.04f), new Vector3(0.08f, 0.24f, 0.28f), heroAccent, root.transform);
        CreateVisualPrimitive("SignalBladeRight", PrimitiveType.Cube, new Vector3(0.42f, 1.18f, -0.04f), new Vector3(0.08f, 0.24f, 0.28f), heroAccent, root.transform);
        GameObject shoulderFinLeft = CreateVisualPrimitive("ShoulderFinLeft", PrimitiveType.Cube, new Vector3(-0.44f, 0.88f, -0.08f), new Vector3(0.08f, 0.32f, 0.22f), heroAccentPurple, root.transform);
        shoulderFinLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        GameObject shoulderFinRight = CreateVisualPrimitive("ShoulderFinRight", PrimitiveType.Cube, new Vector3(0.44f, 0.88f, -0.08f), new Vector3(0.08f, 0.32f, 0.22f), heroAccentPurple, root.transform);
        shoulderFinRight.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject shinBladeLeft = CreateVisualPrimitive("ShinBladeLeft", PrimitiveType.Cube, new Vector3(-0.24f, -0.44f, 0.16f), new Vector3(0.07f, 0.54f, 0.06f), heroTrim, root.transform);
        shinBladeLeft.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        GameObject shinBladeRight = CreateVisualPrimitive("ShinBladeRight", PrimitiveType.Cube, new Vector3(0.24f, -0.44f, 0.16f), new Vector3(0.07f, 0.54f, 0.06f), heroTrim, root.transform);
        shinBladeRight.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        GameObject runnerScarf = CreateVisualPrimitive("RunnerScarf", PrimitiveType.Cube, new Vector3(0.08f, 0.76f, -0.3f), new Vector3(0.12f, 0.8f, 0.04f), heroAccent, root.transform);
        runnerScarf.transform.localRotation = Quaternion.Euler(-12f, 0f, -10f);
        GameObject gauntletLeft = CreateVisualPrimitive("GauntletLeft", PrimitiveType.Cube, new Vector3(-0.46f, 0.28f, 0.22f), new Vector3(0.12f, 0.28f, 0.16f), heroTrim, root.transform);
        gauntletLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
        GameObject gauntletRight = CreateVisualPrimitive("GauntletRight", PrimitiveType.Cube, new Vector3(0.46f, 0.28f, 0.22f), new Vector3(0.12f, 0.28f, 0.16f), heroTrim, root.transform);
        gauntletRight.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
        GameObject holoMantleLeft = CreateVisualPrimitive("HoloMantleLeft", PrimitiveType.Cube, new Vector3(-0.56f, 0.82f, -0.18f), new Vector3(0.09f, 0.86f, 0.05f), heroAccent, root.transform);
        holoMantleLeft.transform.localRotation = Quaternion.Euler(-8f, 0f, 16f);
        GameObject holoMantleRight = CreateVisualPrimitive("HoloMantleRight", PrimitiveType.Cube, new Vector3(0.56f, 0.82f, -0.18f), new Vector3(0.09f, 0.86f, 0.05f), heroAccent, root.transform);
        holoMantleRight.transform.localRotation = Quaternion.Euler(-8f, 0f, -16f);
        GameObject backBanner = CreateVisualPrimitive("BackBanner", PrimitiveType.Cube, new Vector3(0.08f, 0.38f, -0.42f), new Vector3(0.12f, 1.18f, 0.04f), heroAccentPurple, root.transform);
        backBanner.transform.localRotation = Quaternion.Euler(-10f, 0f, -8f);
        GameObject hipSkirtLeft = CreateVisualPrimitive("HipSkirtLeft", PrimitiveType.Cube, new Vector3(-0.3f, 0.06f, -0.26f), new Vector3(0.16f, 0.68f, 0.05f), heroTrim, root.transform);
        hipSkirtLeft.transform.localRotation = Quaternion.Euler(-4f, 0f, 8f);
        GameObject hipSkirtRight = CreateVisualPrimitive("HipSkirtRight", PrimitiveType.Cube, new Vector3(0.3f, 0.08f, -0.26f), new Vector3(0.16f, 0.64f, 0.05f), heroTrim, root.transform);
        hipSkirtRight.transform.localRotation = Quaternion.Euler(-4f, 0f, -8f);
        CreateVisualPrimitive("BootJetLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.88f, -0.12f), new Vector3(0.1f, 0.16f, 0.22f), heroAccent, root.transform);
        CreateVisualPrimitive("BootJetRight", PrimitiveType.Cube, new Vector3(0.16f, -0.88f, -0.12f), new Vector3(0.1f, 0.16f, 0.22f), heroAccent, root.transform);

        CharacterController controller = root.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1f, 0f);
        controller.height = 2f;
        controller.radius = 0.45f;

        PlayerController player = root.AddComponent<PlayerController>();
        ShootingSystem shooting = root.AddComponent<ShootingSystem>();
        PowerUpSystem powerUps = root.AddComponent<PowerUpSystem>();
        root.AddComponent<PlayerSkinApplier>();
        root.AddComponent<PlayerVfxController>();

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 1.1f, 0.8f);

        GameObject poolRoot = new GameObject("ProjectilePool");
        poolRoot.transform.SetParent(root.transform, false);
        shooting.Configure(muzzle.transform, projectilePrefab, poolRoot.transform);
        player.Configure(controller, shooting, powerUps);

        return SavePrefab(root, "Player.prefab");
    }

    private static GameObject CreateProjectilePrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Projectile";
        root.transform.localScale = Vector3.one * 0.3f;
        Object.DestroyImmediate(root.GetComponent<SphereCollider>());
        root.GetComponent<Renderer>().sharedMaterial = material;
        root.AddComponent<Projectile>();
        return SavePrefab(root, "Projectile.prefab");
    }

    private static GameObject CreateBarrierPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Barrier";
        root.transform.localScale = new Vector3(2.15f, 2.4f, 1.1f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Material shell = CreateMaterial("BarrierMetal", new Color(0.06f, 0.06f, 0.08f), new Color(0f, 0f, 0f));
        root.GetComponent<Renderer>().sharedMaterial = shell;
        Material warningCore = CreateMaterial("BarrierWarning", new Color(0.26f, 0.16f, 0.04f), new Color(1f, 0.7f, 0.18f));
        Material laser = CreateMaterial("BarrierLaser", new Color(0.14f, 0.03f, 0.14f), new Color(1f, 0f, 0.78f));
        CreateVisualPrimitive("BarrierCrown", PrimitiveType.Cube, new Vector3(0f, 1f, 0.48f), new Vector3(2.48f, 0.16f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierCore", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.56f), new Vector3(1.34f, 1.56f, 0.14f), warningCore, root.transform);
        CreateVisualPrimitive("BarrierBraceLeft", PrimitiveType.Cube, new Vector3(-0.76f, 0.1f, 0.56f), new Vector3(0.12f, 1.7f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierBraceRight", PrimitiveType.Cube, new Vector3(0.76f, 0.1f, 0.56f), new Vector3(0.12f, 1.7f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierPylonLeft", PrimitiveType.Cube, new Vector3(-1.08f, -0.18f, 0.08f), new Vector3(0.24f, 2.16f, 0.3f), shell, root.transform);
        CreateVisualPrimitive("BarrierPylonRight", PrimitiveType.Cube, new Vector3(1.08f, -0.18f, 0.08f), new Vector3(0.24f, 2.16f, 0.3f), shell, root.transform);
        CreateVisualPrimitive("BarrierPylonCapLeft", PrimitiveType.Cylinder, new Vector3(-1.08f, 0.92f, 0.12f), new Vector3(0.16f, 0.18f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierPylonCapRight", PrimitiveType.Cylinder, new Vector3(1.08f, 0.92f, 0.12f), new Vector3(0.16f, 0.18f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierLaserTop", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.66f), new Vector3(1.9f, 0.06f, 0.05f), laser, root.transform);
        CreateVisualPrimitive("BarrierLaserMid", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.66f), new Vector3(1.9f, 0.05f, 0.05f), laser, root.transform);
        CreateVisualPrimitive("BarrierLaserBottom", PrimitiveType.Cube, new Vector3(0f, -0.48f, 0.66f), new Vector3(1.9f, 0.05f, 0.05f), laser, root.transform);
        CreateVisualPrimitive("BarrierBeaconLeft", PrimitiveType.Cube, new Vector3(-1.08f, 0.94f, 0.34f), new Vector3(0.14f, 0.08f, 0.12f), laser, root.transform);
        CreateVisualPrimitive("BarrierBeaconRight", PrimitiveType.Cube, new Vector3(1.08f, 0.94f, 0.34f), new Vector3(0.14f, 0.08f, 0.12f), laser, root.transform);
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "Barrier.prefab");
    }

    private static GameObject CreateCarPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "CarObstacle";
        root.transform.localScale = new Vector3(1.92f, 0.72f, 4.1f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Material hull = CreateMaterial("CarHullDark", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0f, 0f));
        root.GetComponent<Renderer>().sharedMaterial = hull;
        Material glass = CreateMaterial("TrafficGlass", new Color(0.08f, 0.18f, 0.22f), new Color(0.18f, 0.95f, 1f));
        Material thruster = CreateMaterial("NeonOrange", new Color(0.46f, 0.18f, 0.04f), new Color(1f, 0.5f, 0.18f));
        Material ribbon = CreateMaterial("TrafficRibbon", new Color(0.08f, 0.03f, 0.16f), new Color(0.42f, 0f, 1f));
        CreateVisualPrimitive("NoseBlade", PrimitiveType.Cube, new Vector3(0f, 0.04f, 1.8f), new Vector3(1.18f, 0.16f, 0.26f), material, root.transform);
        CreateVisualPrimitive("SideBladeLeft", PrimitiveType.Cube, new Vector3(-1.02f, -0.04f, -0.08f), new Vector3(0.12f, 0.18f, 3.52f), material, root.transform);
        CreateVisualPrimitive("SideBladeRight", PrimitiveType.Cube, new Vector3(1.02f, -0.04f, -0.08f), new Vector3(0.12f, 0.18f, 3.52f), material, root.transform);
        CreateVisualPrimitive("CabinBase", PrimitiveType.Cube, new Vector3(0f, 0.3f, -0.12f), new Vector3(1.28f, 0.42f, 2.08f), glass, root.transform);
        CreateVisualPrimitive("Canopy", PrimitiveType.Cube, new Vector3(0f, 0.58f, -0.18f), new Vector3(1.04f, 0.26f, 1.44f), glass, root.transform);
        CreateVisualPrimitive("CanopyGlow", PrimitiveType.Cube, new Vector3(0f, 0.74f, -0.08f), new Vector3(0.92f, 0.05f, 1.2f), material, root.transform);
        CreateVisualPrimitive("RearFinLeft", PrimitiveType.Cube, new Vector3(-0.5f, 0.2f, -1.64f), new Vector3(0.14f, 0.34f, 0.54f), ribbon, root.transform);
        CreateVisualPrimitive("RearFinRight", PrimitiveType.Cube, new Vector3(0.5f, 0.2f, -1.64f), new Vector3(0.14f, 0.34f, 0.54f), ribbon, root.transform);
        CreateVisualPrimitive("ThrusterL", PrimitiveType.Cylinder, new Vector3(-0.76f, -0.18f, -1.8f), new Vector3(0.16f, 0.3f, 0.16f), thruster, root.transform);
        CreateVisualPrimitive("ThrusterR", PrimitiveType.Cylinder, new Vector3(0.76f, -0.18f, -1.8f), new Vector3(0.16f, 0.3f, 0.16f), thruster, root.transform);
        CreateVisualPrimitive("NoseLight", PrimitiveType.Cube, new Vector3(0f, 0.08f, 2.02f), new Vector3(1.02f, 0.08f, 0.08f), material, root.transform);
        CreateVisualPrimitive("UnderGlow", PrimitiveType.Cube, new Vector3(0f, -0.34f, 0f), new Vector3(1.1f, 0.04f, 2.9f), ribbon, root.transform);
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "CarObstacle.prefab");
    }

    private static GameObject CreateDronePrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Drone";
        root.transform.localScale = new Vector3(1.02f, 0.58f, 1.38f);
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        Material shell = CreateMaterial("DroneShell", new Color(0.03f, 0.05f, 0.07f), new Color(0f, 0f, 0f));
        Material engine = CreateMaterial("DroneEngine", new Color(0.16f, 0.02f, 0.05f), new Color(1f, 0.22f, 0.26f));
        Material eye = CreateMaterial("DroneEye", new Color(0.06f, 0.14f, 0.18f), new Color(0f, 0.92f, 1f));
        root.GetComponent<Renderer>().sharedMaterial = shell;
        CreateVisualPrimitive("WingLeft", PrimitiveType.Cube, new Vector3(-1f, 0.02f, -0.04f), new Vector3(1.06f, 0.08f, 0.38f), shell, root.transform);
        CreateVisualPrimitive("WingRight", PrimitiveType.Cube, new Vector3(1f, 0.02f, -0.04f), new Vector3(1.06f, 0.08f, 0.38f), shell, root.transform);
        CreateVisualPrimitive("NoseBlade", PrimitiveType.Cube, new Vector3(0f, 0f, 0.72f), new Vector3(0.42f, 0.16f, 0.34f), material, root.transform);
        CreateVisualPrimitive("EyeBar", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.58f), new Vector3(0.82f, 0.12f, 0.12f), eye, root.transform);
        CreateVisualPrimitive("EngineLeft", PrimitiveType.Cylinder, new Vector3(-0.74f, 0.08f, -0.24f), new Vector3(0.14f, 0.2f, 0.14f), engine, root.transform);
        CreateVisualPrimitive("EngineRight", PrimitiveType.Cylinder, new Vector3(0.74f, 0.08f, -0.24f), new Vector3(0.14f, 0.2f, 0.14f), engine, root.transform);
        CreateVisualPrimitive("Core", PrimitiveType.Sphere, new Vector3(0f, -0.02f, 0.06f), Vector3.one * 0.3f, CreateMaterial("DroneCore", new Color(0.14f, 0.03f, 0.05f), new Color(1f, 0.18f, 0.22f)), root.transform);
        CreateVisualPrimitive("DorsalFin", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.18f), new Vector3(0.12f, 0.3f, 0.28f), material, root.transform);
        CreateVisualPrimitive("VentralFin", PrimitiveType.Cube, new Vector3(0f, -0.26f, -0.12f), new Vector3(0.1f, 0.16f, 0.3f), shell, root.transform);
        GameObject scytheLeft = CreateVisualPrimitive("ScytheLeft", PrimitiveType.Cube, new Vector3(-0.72f, -0.2f, 0.52f), new Vector3(0.1f, 0.44f, 0.08f), material, root.transform);
        scytheLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        GameObject scytheRight = CreateVisualPrimitive("ScytheRight", PrimitiveType.Cube, new Vector3(0.72f, -0.2f, 0.52f), new Vector3(0.1f, 0.44f, 0.08f), material, root.transform);
        scytheRight.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        CreateVisualPrimitive("AntennaSpire", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.02f), new Vector3(0.06f, 0.24f, 0.06f), eye, root.transform);
        CreateVisualPrimitive("TailStabilizer", PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.62f), new Vector3(0.2f, 0.12f, 0.34f), engine, root.transform);
        CreateVisualPrimitive("PodLeft", PrimitiveType.Cube, new Vector3(-0.96f, 0.08f, -0.22f), new Vector3(0.22f, 0.18f, 0.44f), shell, root.transform);
        CreateVisualPrimitive("PodRight", PrimitiveType.Cube, new Vector3(0.96f, 0.08f, -0.22f), new Vector3(0.22f, 0.18f, 0.44f), shell, root.transform);
        CreateVisualPrimitive("WingEdgeLeft", PrimitiveType.Cube, new Vector3(-1.28f, 0.02f, 0.14f), new Vector3(0.24f, 0.06f, 0.18f), eye, root.transform);
        CreateVisualPrimitive("WingEdgeRight", PrimitiveType.Cube, new Vector3(1.28f, 0.02f, 0.14f), new Vector3(0.24f, 0.06f, 0.18f), eye, root.transform);
        GameObject mandibleLeft = CreateVisualPrimitive("MandibleLeft", PrimitiveType.Cube, new Vector3(-0.28f, -0.18f, 0.86f), new Vector3(0.1f, 0.5f, 0.1f), material, root.transform);
        mandibleLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        GameObject mandibleRight = CreateVisualPrimitive("MandibleRight", PrimitiveType.Cube, new Vector3(0.28f, -0.18f, 0.86f), new Vector3(0.1f, 0.5f, 0.1f), material, root.transform);
        mandibleRight.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        CreateVisualPrimitive("SensorHalo", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0.18f), new Vector3(0.72f, 0.04f, 0.72f), eye, root.transform);
        GameObject tailForkLeft = CreateVisualPrimitive("TailForkLeft", PrimitiveType.Cube, new Vector3(-0.26f, 0.12f, -0.9f), new Vector3(0.08f, 0.34f, 0.26f), engine, root.transform);
        tailForkLeft.transform.localRotation = Quaternion.Euler(-10f, 0f, 18f);
        GameObject tailForkRight = CreateVisualPrimitive("TailForkRight", PrimitiveType.Cube, new Vector3(0.26f, 0.12f, -0.9f), new Vector3(0.08f, 0.34f, 0.26f), engine, root.transform);
        tailForkRight.transform.localRotation = Quaternion.Euler(-10f, 0f, -18f);
        CreateVisualPrimitive("EngineGlowLeft", PrimitiveType.Cube, new Vector3(-0.74f, 0.08f, -0.54f), new Vector3(0.18f, 0.08f, 0.18f), eye, root.transform);
        CreateVisualPrimitive("EngineGlowRight", PrimitiveType.Cube, new Vector3(0.74f, 0.08f, -0.54f), new Vector3(0.18f, 0.08f, 0.18f), eye, root.transform);
        root.AddComponent<EnemyDrone>();
        return SavePrefab(root, "Drone.prefab");
    }

    private static GameObject CreateBossPrefab(Material primaryAccent, Material secondaryAccent, Material tertiaryAccent)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "BossDrone";
        root.transform.localScale = new Vector3(2.8f, 1.44f, 3.9f);
        Object.DestroyImmediate(root.GetComponent<SphereCollider>());
        Material bossShell = CreateMaterial("BossShell", new Color(0.03f, 0.04f, 0.06f), new Color(0.08f, 0.02f, 0.04f));
        Material bossCore = CreateMaterial("BossCore", new Color(0.18f, 0.03f, 0.05f), new Color(1f, 0.18f, 0.24f));
        Material bossEye = CreateMaterial("BossEye", new Color(0.06f, 0.14f, 0.18f), new Color(0f, 0.96f, 1f));
        Material thruster = CreateMaterial("BossThruster", new Color(0.18f, 0.1f, 0.03f), new Color(1f, 0.62f, 0.18f));
        root.GetComponent<Renderer>().sharedMaterial = bossShell;
        CreateVisualPrimitive("BossRing", PrimitiveType.Cylinder, new Vector3(0f, -0.12f, -0.12f), new Vector3(3.2f, 0.06f, 3.2f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("BossCore", PrimitiveType.Sphere, new Vector3(0f, -0.08f, 1.04f), Vector3.one * 0.92f, bossCore, root.transform);
        CreateVisualPrimitive("BossEye", PrimitiveType.Cube, new Vector3(0f, 0.22f, 1.92f), new Vector3(1.7f, 0.22f, 0.18f), bossEye, root.transform);
        CreateVisualPrimitive("WingLeft", PrimitiveType.Cube, new Vector3(-2.52f, 0.04f, -0.18f), new Vector3(1.86f, 0.14f, 0.72f), primaryAccent, root.transform);
        CreateVisualPrimitive("WingRight", PrimitiveType.Cube, new Vector3(2.52f, 0.04f, -0.18f), new Vector3(1.86f, 0.14f, 0.72f), primaryAccent, root.transform);
        CreateVisualPrimitive("ArmorPlateLeft", PrimitiveType.Cube, new Vector3(-1.28f, 0.2f, 0.96f), new Vector3(0.58f, 0.36f, 1.02f), bossShell, root.transform);
        CreateVisualPrimitive("ArmorPlateRight", PrimitiveType.Cube, new Vector3(1.28f, 0.2f, 0.96f), new Vector3(0.58f, 0.36f, 1.02f), bossShell, root.transform);
        CreateVisualPrimitive("MandibleLeft", PrimitiveType.Cube, new Vector3(-0.82f, -0.56f, 1.82f), new Vector3(0.22f, 0.52f, 0.18f), primaryAccent, root.transform);
        CreateVisualPrimitive("MandibleRight", PrimitiveType.Cube, new Vector3(0.82f, -0.56f, 1.82f), new Vector3(0.22f, 0.52f, 0.18f), primaryAccent, root.transform);
        CreateVisualPrimitive("CannonLeft", PrimitiveType.Cylinder, new Vector3(-1.3f, -0.56f, 1.22f), new Vector3(0.22f, 0.52f, 0.22f), secondaryAccent, root.transform);
        CreateVisualPrimitive("CannonRight", PrimitiveType.Cylinder, new Vector3(1.3f, -0.56f, 1.22f), new Vector3(0.22f, 0.52f, 0.22f), secondaryAccent, root.transform);
        CreateVisualPrimitive("TopFin", PrimitiveType.Cube, new Vector3(0f, 0.86f, 0.12f), new Vector3(0.34f, 0.38f, 1.4f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("CrownSpineLeft", PrimitiveType.Cube, new Vector3(-0.74f, 0.98f, 0.34f), new Vector3(0.16f, 0.52f, 0.82f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("CrownSpineRight", PrimitiveType.Cube, new Vector3(0.74f, 0.98f, 0.34f), new Vector3(0.16f, 0.52f, 0.82f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("WingTipLeft", PrimitiveType.Cube, new Vector3(-3.44f, 0.04f, 0.06f), new Vector3(0.64f, 0.08f, 0.32f), secondaryAccent, root.transform);
        CreateVisualPrimitive("WingTipRight", PrimitiveType.Cube, new Vector3(3.44f, 0.04f, 0.06f), new Vector3(0.64f, 0.08f, 0.32f), secondaryAccent, root.transform);
        CreateVisualPrimitive("ReactorHalo", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, -1.12f), new Vector3(1.46f, 0.04f, 1.46f), primaryAccent, root.transform);
        CreateVisualPrimitive("UnderCarriage", PrimitiveType.Cube, new Vector3(0f, -0.78f, 0.14f), new Vector3(1.52f, 0.18f, 1.92f), bossShell, root.transform);
        CreateVisualPrimitive("RearThrusterLeft", PrimitiveType.Cube, new Vector3(-0.62f, -0.1f, -2.08f), new Vector3(0.52f, 0.22f, 0.18f), thruster, root.transform);
        CreateVisualPrimitive("RearThrusterRight", PrimitiveType.Cube, new Vector3(0.62f, -0.1f, -2.08f), new Vector3(0.52f, 0.22f, 0.18f), thruster, root.transform);
        CreateVisualPrimitive("DorsalBladeLeft", PrimitiveType.Cube, new Vector3(-1.12f, 0.62f, -0.22f), new Vector3(0.12f, 0.66f, 1.22f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("DorsalBladeRight", PrimitiveType.Cube, new Vector3(1.12f, 0.62f, -0.22f), new Vector3(0.12f, 0.66f, 1.22f), tertiaryAccent, root.transform);
        GameObject clawLeft = CreateVisualPrimitive("ClawLeft", PrimitiveType.Cube, new Vector3(-1.54f, -0.64f, 1.58f), new Vector3(0.16f, 0.84f, 0.12f), primaryAccent, root.transform);
        clawLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        GameObject clawRight = CreateVisualPrimitive("ClawRight", PrimitiveType.Cube, new Vector3(1.54f, -0.64f, 1.58f), new Vector3(0.16f, 0.84f, 0.12f), primaryAccent, root.transform);
        clawRight.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        CreateVisualPrimitive("EnginePodLeft", PrimitiveType.Cylinder, new Vector3(-2.36f, -0.08f, -0.98f), new Vector3(0.28f, 0.56f, 0.28f), thruster, root.transform);
        CreateVisualPrimitive("EnginePodRight", PrimitiveType.Cylinder, new Vector3(2.36f, -0.08f, -0.98f), new Vector3(0.28f, 0.56f, 0.28f), thruster, root.transform);
        CreateVisualPrimitive("CoreShield", PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 1.12f), new Vector3(1.26f, 0.05f, 1.26f), secondaryAccent, root.transform);
        GameObject noseHorn = CreateVisualPrimitive("NoseHorn", PrimitiveType.Cube, new Vector3(0f, 0.4f, 2.14f), new Vector3(0.22f, 0.18f, 0.42f), secondaryAccent, root.transform);
        noseHorn.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        root.AddComponent<BossController>();
        return SavePrefab(root, "BossDrone.prefab");
    }

    private static GameObject CreateBossHazardPrefab(Material telegraphMaterial, Material activeMaterial)
    {
        GameObject root = new GameObject("BossLaneHazard");
        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1.8f, 2f, 3.4f);

        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
        telegraph.name = "Telegraph";
        telegraph.transform.SetParent(root.transform, false);
        telegraph.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        telegraph.transform.localScale = new Vector3(1.6f, 0.05f, 3f);
        telegraph.GetComponent<Renderer>().sharedMaterial = CreateMaterial("BossHazardTelegraph", new Color(0.18f, 0.03f, 0.08f), telegraphMaterial.color);
        Object.DestroyImmediate(telegraph.GetComponent<BoxCollider>());

        GameObject active = GameObject.CreatePrimitive(PrimitiveType.Cube);
        active.name = "ActiveHazard";
        active.transform.SetParent(root.transform, false);
        active.transform.localPosition = new Vector3(0f, 1f, 0f);
        active.transform.localScale = new Vector3(0.28f, 2f, 3f);
        active.GetComponent<Renderer>().sharedMaterial = CreateMaterial("BossHazardActive", new Color(0.06f, 0.14f, 0.18f), activeMaterial.color);
        Object.DestroyImmediate(active.GetComponent<BoxCollider>());

        BossLaneHazard hazard = root.AddComponent<BossLaneHazard>();
        SetSerializedField(hazard, "damageCollider", collider);
        SetSerializedField(hazard, "telegraphRenderer", telegraph.GetComponent<Renderer>());
        SetSerializedField(hazard, "activeRenderer", active.GetComponent<Renderer>());
        return SavePrefab(root, "BossLaneHazard.prefab");
    }

    private static GameObject CreateCreditPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "Credit";
        root.transform.localScale = new Vector3(0.35f, 0.1f, 0.35f);
        CapsuleCollider collider = root.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        root.GetComponent<Renderer>().sharedMaterial = material;
        Material halo = CreateMaterial("NeonGold", new Color(0.44f, 0.34f, 0.08f), new Color(1f, 0.88f, 0.2f));
        CreateVisualPrimitive("CreditHalo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.02f, 0.5f), halo, root.transform);
        CreateVisualPrimitive("CreditGlyph", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.05f, 0.42f), halo, root.transform);
        CreateVisualPrimitive("CreditCore", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.12f, 0.22f), CreateMaterial("CreditCore", new Color(0.2f, 0.15f, 0.04f), new Color(1f, 0.74f, 0.18f)), root.transform);
        root.AddComponent<CreditPickup>();
        return SavePrefab(root, "Credit.prefab");
    }

    private static GameObject CreatePowerUpPrefab(string assetName, PrimitiveType primitiveType, Material material, PowerUpType powerUpType, float duration)
    {
        GameObject root = GameObject.CreatePrimitive(primitiveType);
        root.name = assetName;
        Collider collider = root.GetComponent<Collider>();
        collider.isTrigger = true;
        root.transform.localScale = Vector3.one * 0.9f;
        root.GetComponent<Renderer>().sharedMaterial = material;
        Material shell = CreateMaterial($"{assetName}Shell", new Color(0.06f, 0.08f, 0.12f), Color.black);
        CreateVisualPrimitive($"{assetName}Aura", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.66f, 0.04f, 0.66f), material, root.transform);
        CreateVisualPrimitive($"{assetName}Core", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.72f, 0.22f), shell, root.transform);
        switch (powerUpType)
        {
            case PowerUpType.Shield:
                CreateVisualPrimitive($"{assetName}ShieldBar", PrimitiveType.Cube, new Vector3(0f, 0f, 0.02f), new Vector3(0.22f, 0.62f, 0.1f), material, root.transform);
                CreateVisualPrimitive($"{assetName}ShieldArcLeft", PrimitiveType.Cube, new Vector3(-0.2f, 0f, 0.02f), new Vector3(0.12f, 0.5f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}ShieldArcRight", PrimitiveType.Cube, new Vector3(0.2f, 0f, 0.02f), new Vector3(0.12f, 0.5f, 0.08f), material, root.transform);
                break;
            case PowerUpType.DoubleScore:
                CreateVisualPrimitive($"{assetName}Cross", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.68f, 0.12f, 0.12f), material, root.transform);
                CreateVisualPrimitive($"{assetName}ScoreBar", PrimitiveType.Cube, new Vector3(0f, 0f, 0.18f), new Vector3(0.14f, 0.56f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}Ring", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.48f, 0.02f, 0.48f), shell, root.transform);
                break;
            case PowerUpType.SlowMotion:
                CreateVisualPrimitive($"{assetName}Upper", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.52f, 0.1f, 0.1f), material, root.transform);
                CreateVisualPrimitive($"{assetName}Lower", PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.52f, 0.1f, 0.1f), material, root.transform);
                CreateVisualPrimitive($"{assetName}Waist", PrimitiveType.Cube, new Vector3(0f, 0f, 0.08f), new Vector3(0.12f, 0.36f, 0.08f), material, root.transform);
                break;
            case PowerUpType.EmpBlast:
                CreateVisualPrimitive($"{assetName}ProngLeft", PrimitiveType.Cube, new Vector3(-0.22f, 0f, 0.1f), new Vector3(0.1f, 0.62f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}ProngRight", PrimitiveType.Cube, new Vector3(0.22f, 0f, 0.1f), new Vector3(0.1f, 0.62f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}Pulse", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.02f, 0.5f), material, root.transform);
                break;
            case PowerUpType.Magnet:
                CreateVisualPrimitive($"{assetName}ArcLeft", PrimitiveType.Cube, new Vector3(-0.18f, 0f, 0.08f), new Vector3(0.12f, 0.56f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}ArcRight", PrimitiveType.Cube, new Vector3(0.18f, 0f, 0.08f), new Vector3(0.12f, 0.56f, 0.08f), material, root.transform);
                CreateVisualPrimitive($"{assetName}Bridge", PrimitiveType.Cube, new Vector3(0f, 0.24f, 0.12f), new Vector3(0.48f, 0.1f, 0.08f), material, root.transform);
                break;
            case PowerUpType.SpeedBoost:
                GameObject arrowLeft = CreateVisualPrimitive($"{assetName}ArrowLeft", PrimitiveType.Cube, new Vector3(-0.14f, 0f, 0.12f), new Vector3(0.16f, 0.52f, 0.08f), material, root.transform);
                arrowLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
                GameObject arrowRight = CreateVisualPrimitive($"{assetName}ArrowRight", PrimitiveType.Cube, new Vector3(0.14f, 0f, 0.12f), new Vector3(0.16f, 0.52f, 0.08f), material, root.transform);
                arrowRight.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
                CreateVisualPrimitive($"{assetName}Spine", PrimitiveType.Cube, new Vector3(0f, -0.06f, 0.04f), new Vector3(0.12f, 0.54f, 0.08f), material, root.transform);
                break;
            default:
                CreateVisualPrimitive($"{assetName}Cross", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.62f, 0.12f, 0.12f), material, root.transform);
                break;
        }

        PowerUpPickup pickup = root.AddComponent<PowerUpPickup>();
        SetEnumField(pickup, "powerUpType", powerUpType);
        SetFloatField(pickup, "duration", duration);

        return SavePrefab(root, $"{assetName}.prefab");
    }

    private static GameObject CreateChunkPrefab(string assetName, Material roadMaterial, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent, Material warningAccent, ChunkVisualStyle style)
    {
        GameObject root = new GameObject(assetName.Replace(".prefab", string.Empty));
        CreateVisualPrimitive("ChunkRoad", PrimitiveType.Cube, new Vector3(0f, -0.16f, 15f), new Vector3(10.2f, 0.08f, 30f), roadMaterial, root.transform);
        CreateVisualPrimitive("ChunkRoadShoulderLeft", PrimitiveType.Cube, new Vector3(-3.85f, -0.13f, 15f), new Vector3(1.5f, 0.03f, 30f), CreateMaterial("ChunkRoadShoulder", new Color(0.08f, 0.09f, 0.13f), Color.black), root.transform);
        CreateVisualPrimitive("ChunkRoadShoulderRight", PrimitiveType.Cube, new Vector3(3.85f, -0.13f, 15f), new Vector3(1.5f, 0.03f, 30f), CreateMaterial("ChunkRoadShoulder", new Color(0.08f, 0.09f, 0.13f), Color.black), root.transform);
        CreateVisualPrimitive("ChunkLeftRail", PrimitiveType.Cube, new Vector3(-5.1f, 0.5f, 15f), new Vector3(0.08f, 1f, 30f), primaryAccent, root.transform);
        CreateVisualPrimitive("ChunkRightRail", PrimitiveType.Cube, new Vector3(5.1f, 0.5f, 15f), new Vector3(0.08f, 1f, 30f), secondaryAccent, root.transform);
        CreateVisualPrimitive("ChunkLeftWall", PrimitiveType.Cube, new Vector3(-5.55f, 0.24f, 15f), new Vector3(0.42f, 0.52f, 30f), CreateMaterial("ChunkWallShell", new Color(0.05f, 0.06f, 0.09f), Color.black), root.transform);
        CreateVisualPrimitive("ChunkRightWall", PrimitiveType.Cube, new Vector3(5.55f, 0.24f, 15f), new Vector3(0.42f, 0.52f, 30f), CreateMaterial("ChunkWallShell", new Color(0.05f, 0.06f, 0.09f), Color.black), root.transform);
        CreateChunkLaneLights(root.transform, tertiaryAccent);
        CreateChunkShoulderModules(root.transform, primaryAccent, secondaryAccent, tertiaryAccent, style != ChunkVisualStyle.Security && style != ChunkVisualStyle.Citadel);

        switch (style)
        {
            case ChunkVisualStyle.Gateway:
                CreateChunkArchSeries(root.transform, primaryAccent, secondaryAccent, 3);
                CreateChunkForegroundAnchors(root.transform, secondaryAccent, warningAccent, false);
                break;
            case ChunkVisualStyle.Billboard:
                CreateChunkBillboards(root.transform, primaryAccent, warningAccent);
                CreateChunkForegroundAnchors(root.transform, secondaryAccent, tertiaryAccent, true);
                break;
            case ChunkVisualStyle.Bridge:
                CreateChunkBillboards(root.transform, primaryAccent, warningAccent);
                CreateChunkSideStructures(root.transform, secondaryAccent, tertiaryAccent, false);
                CreateChunkTransitRibbon(root.transform, tertiaryAccent, false);
                break;
            case ChunkVisualStyle.Tunnel:
                CreateChunkArchSeries(root.transform, primaryAccent, secondaryAccent, 5);
                CreateChunkTransitRibbon(root.transform, tertiaryAccent, true);
                CreateChunkForegroundAnchors(root.transform, secondaryAccent, warningAccent, false);
                break;
            case ChunkVisualStyle.Security:
                CreateChunkSideStructures(root.transform, secondaryAccent, tertiaryAccent, true);
                CreateChunkSecurityCheckpoint(root.transform, secondaryAccent, tertiaryAccent, warningAccent);
                break;
            case ChunkVisualStyle.Plaza:
                CreateChunkArchSeries(root.transform, primaryAccent, secondaryAccent, 2);
                CreateChunkMarketStalls(root.transform, primaryAccent, tertiaryAccent, warningAccent);
                CreateChunkForegroundAnchors(root.transform, secondaryAccent, warningAccent, true);
                break;
            case ChunkVisualStyle.Transit:
                CreateChunkBillboards(root.transform, primaryAccent, warningAccent);
                CreateChunkTransitRibbon(root.transform, secondaryAccent, false);
                CreateChunkMarketStalls(root.transform, primaryAccent, tertiaryAccent, warningAccent);
                break;
            case ChunkVisualStyle.Citadel:
                CreateChunkSideStructures(root.transform, secondaryAccent, tertiaryAccent, true);
                CreateChunkSecurityCheckpoint(root.transform, primaryAccent, tertiaryAccent, warningAccent);
                CreateChunkTransitRibbon(root.transform, secondaryAccent, true);
                CreateChunkForegroundAnchors(root.transform, warningAccent, tertiaryAccent, false);
                break;
        }

        CreateChunkDistrictSignature(root.transform, style, primaryAccent, secondaryAccent, tertiaryAccent, warningAccent);

        return SavePrefab(root, assetName);
    }

    private static GameObject CreateBossStagePrefab(string assetName, Material roadMaterial, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent, Material warningAccent)
    {
        GameObject root = new GameObject(assetName.Replace(".prefab", string.Empty));
        Material shell = CreateMaterial("BossStageShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        CreateVisualPrimitive("StageDeck", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(12.4f, 0.14f, 26f), roadMaterial, root.transform);
        CreateVisualPrimitive("StageRailLeft", PrimitiveType.Cube, new Vector3(-6.08f, 0.92f, 0f), new Vector3(0.14f, 1.8f, 24f), shell, root.transform);
        CreateVisualPrimitive("StageRailRight", PrimitiveType.Cube, new Vector3(6.08f, 0.92f, 0f), new Vector3(0.14f, 1.8f, 24f), shell, root.transform);
        CreateVisualPrimitive("StageGlowLeft", PrimitiveType.Cube, new Vector3(-5.74f, 1.28f, 0f), new Vector3(0.08f, 0.06f, 24f), primaryAccent, root.transform);
        CreateVisualPrimitive("StageGlowRight", PrimitiveType.Cube, new Vector3(5.74f, 1.28f, 0f), new Vector3(0.08f, 0.06f, 24f), secondaryAccent, root.transform);
        CreateVisualPrimitive("StagePylonLeft", PrimitiveType.Cube, new Vector3(-7.8f, 4.4f, 1.6f), new Vector3(1.8f, 8.8f, 2.8f), shell, root.transform);
        CreateVisualPrimitive("StagePylonRight", PrimitiveType.Cube, new Vector3(7.8f, 4.4f, -1.6f), new Vector3(1.8f, 8.8f, 2.8f), shell, root.transform);
        CreateVisualPrimitive("StagePylonTrimLeft", PrimitiveType.Cube, new Vector3(-6.96f, 4.5f, 1.6f), new Vector3(0.12f, 7.2f, 2.1f), primaryAccent, root.transform);
        CreateVisualPrimitive("StagePylonTrimRight", PrimitiveType.Cube, new Vector3(6.96f, 4.5f, -1.6f), new Vector3(0.12f, 7.2f, 2.1f), secondaryAccent, root.transform);
        CreateVisualPrimitive("StageScreenLeft", PrimitiveType.Cube, new Vector3(-8.25f, 5.6f, 5.4f), new Vector3(2.7f, 1.8f, 0.14f), CreateMaterial("BossStageScreen", new Color(0.05f, 0.12f, 0.18f), primaryAccent.color), root.transform);
        CreateVisualPrimitive("StageScreenRight", PrimitiveType.Cube, new Vector3(8.25f, 5.6f, -5.4f), new Vector3(2.7f, 1.8f, 0.14f), CreateMaterial("BossStageWarning", new Color(0.16f, 0.05f, 0.05f), warningAccent.color), root.transform);
        CreateVisualPrimitive("StageHaloOuter", PrimitiveType.Cylinder, new Vector3(0f, 5.8f, 7.6f), new Vector3(8.6f, 0.08f, 8.6f), shell, root.transform);
        CreateVisualPrimitive("StageHaloInner", PrimitiveType.Cylinder, new Vector3(0f, 5.96f, 7.6f), new Vector3(7.8f, 0.05f, 7.8f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("StageCeiling", PrimitiveType.Cube, new Vector3(0f, 7.45f, 0f), new Vector3(11.6f, 0.2f, 7.8f), shell, root.transform);
        CreateVisualPrimitive("StageCeilingGlow", PrimitiveType.Cube, new Vector3(0f, 7.64f, 0f), new Vector3(9.2f, 0.06f, 5.8f), tertiaryAccent, root.transform);

        NeonPropAnimator haloAnimator = root.transform.Find("StageHaloInner")?.gameObject.AddComponent<NeonPropAnimator>();
        haloAnimator?.Configure(new Color(0.26f, 1f, 1f), 1.55f, 0.85f, 1.45f);
        NeonPropAnimator ceilingAnimator = root.transform.Find("StageCeilingGlow")?.gameObject.AddComponent<NeonPropAnimator>();
        ceilingAnimator?.Configure(new Color(1f, 0.38f, 0.92f), 1.18f, 0.65f, 1.15f);

        return SavePrefab(root, assetName);
    }

    private static void CreateGameScene(BootstrapAssets assets, GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.GameScene;

        CreatePersistentSystems(configs);
        CreateDirectionalLight();
        CreateEnvironment(assets);
        CreateEventSystem();

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(assets.Player);
        player.transform.position = new Vector3(0f, 0.2f, 0f);

        Camera camera = CreateCamera(player.transform);
        camera.gameObject.AddComponent<FpsCounter>();
        PostProcessingConfig postProcessing = camera.gameObject.AddComponent<PostProcessingConfig>();
        SetSerializedField(postProcessing, "qualityConfig", configs.VisualQuality);
        PresentationDirector presentationDirector = camera.gameObject.AddComponent<PresentationDirector>();
        SetSerializedField(presentationDirector, "districtLibrary", configs.DistrictPresentation);
        SetSerializedField(presentationDirector, "postProcessingConfig", postProcessing);

        new GameObject("ScreenShake").AddComponent<ScreenShake>();
        new GameObject("ComboSystem").AddComponent<ComboSystem>();
        new GameObject("HapticFeedback").AddComponent<HapticFeedback>();
        new GameObject("MilestoneSystem").AddComponent<MilestoneSystem>();
        new GameObject("FeverMode").AddComponent<FeverMode>();
        new GameObject("NearMissDetector").AddComponent<NearMissDetector>();
        new GameObject("UIAnimator").AddComponent<UIAnimator>();
        new GameObject("AchievementPopup").AddComponent<AchievementPopup>();
        new GameObject("FloatingText").AddComponent<FloatingTextManager>();
        new GameObject("PerformanceAuditor").AddComponent<PerformanceAuditor>();
        new GameObject("ScrollingGround").AddComponent<ScrollingGround>();
        EnvironmentQualityController environmentQuality = new GameObject("EnvironmentQualityController").AddComponent<EnvironmentQualityController>();
        SetSerializedField(environmentQuality, "qualityConfig", configs.VisualQuality);

        GameObject pauseObject = new GameObject("PauseController");
        PauseController pauseCtrl = pauseObject.AddComponent<PauseController>();
        Canvas hudCanvas = CreateHudCanvas(player.GetComponent<PlayerController>(), camera, pauseCtrl, out HoldButton holdButton);
        holdButton.Bind(player.GetComponent<PlayerController>());

        UIFlowController uiFlow = new GameObject("UIFlowController").AddComponent<UIFlowController>();
        SetSerializedField(uiFlow, "hudCanvas", hudCanvas);
        SetSerializedField(uiFlow, "pausePanel", FindNamedChild(hudCanvas.transform, "PausePanel"));
        SetSerializedField(uiFlow, "revivePanel", FindNamedChild(hudCanvas.transform, "RevivePanel"));
        SetSerializedField(uiFlow, "visualTheme", configs.UiTheme);

        player.AddComponent<MagnetField>();
        DynamicCameraController dynamicCamera = camera.gameObject.AddComponent<DynamicCameraController>();
        SetVector3Field(dynamicCamera, "baseOffset", new Vector3(2.18f, 5.28f, -12.15f));
        SetFloatField(dynamicCamera, "lookHeight", 1.45f);
        SetFloatField(dynamicCamera, "lookAheadDistance", 15f);
        SetFloatField(dynamicCamera, "sideBias", 1.65f);
        SetFloatField(dynamicCamera, "fovBase", 57f);
        SetFloatField(dynamicCamera, "fovMax", 71f);

        GameObject spawnerObject = new GameObject("ObstacleSpawner");
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        GameObject poolsRoot = new GameObject("Pools");
        poolsRoot.transform.SetParent(spawnerObject.transform, false);
        spawner.Configure(player.GetComponent<PlayerController>(), new[] { assets.Barrier, assets.Car }, assets.Drone, assets.PowerUps, assets.Credit, poolsRoot.transform);
        SetSerializedField(spawner, "encounterConfig", configs.EncounterTuning);

        BossEncounterManager bossEncounter = new GameObject("BossEncounterManager").AddComponent<BossEncounterManager>();
        bossEncounter.Configure(player.GetComponent<PlayerController>(), spawner, assets.Boss, assets.BossHazard, assets.BossStage);

        LevelChunkGenerator chunkGenerator = new GameObject("LevelChunkGenerator").AddComponent<LevelChunkGenerator>();
        Transform chunkPoolRoot = new GameObject("ChunkPools").transform;
        chunkPoolRoot.SetParent(chunkGenerator.transform, false);
        chunkGenerator.Configure(player.GetComponent<PlayerController>(), BuildChunkSets(assets), chunkPoolRoot);
        EditorUtility.SetDirty(chunkGenerator);

        DifficultyDirector difficultyDirector = new GameObject("DifficultyDirector").AddComponent<DifficultyDirector>();
        difficultyDirector.Configure(spawner);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameScene}.unity");
    }

    private static void CreateMainMenuScene(BootstrapAssets assets, GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.MainMenu;

        CreatePersistentSystems(configs);
        CreateDirectionalLight();
        Camera menuCamera = CreateStandaloneCamera(new Vector3(2.8f, 2.62f, -12.6f), new Vector3(10.5f, -10.2f, 0f));
        menuCamera.fieldOfView = 46f;
        CreateMenuShowcase(assets);
        CreateEventSystem();

        GameObject menuRoot = new GameObject("MainMenuUI");
        MainMenuController controller = menuRoot.AddComponent<MainMenuController>();
        Canvas canvas = CreateCanvas("MainMenuCanvas");
        Font font = GetBuiltinFont();
        UIFlowController uiFlow = new GameObject("UIFlowController").AddComponent<UIFlowController>();
        SetSerializedField(uiFlow, "menuCanvas", canvas);
        SetSerializedField(uiFlow, "visualTheme", configs.UiTheme);

        CreateMenuBackdropUi(canvas.transform);
        CreatePanel(canvas.transform, "TitlePanel", new Vector2(-142f, 358f), new Vector2(642f, 154f));
        CreatePanel(canvas.transform, "ControlDockPanel", new Vector2(-356f, -44f), new Vector2(324f, 752f));
        GameObject profilePanel = CreatePanel(canvas.transform, "ProfilePanel", new Vector2(372f, 170f), new Vector2(388f, 320f));
        GameObject loadoutPanel = CreatePanel(canvas.transform, "LoadoutPanel", new Vector2(372f, -126f), new Vector2(388f, 222f));
        CreatePanel(canvas.transform, "ShowcaseCaptionPanel", new Vector2(118f, 56f), new Vector2(312f, 96f));

        CreateText(canvas.transform, font, "SECTOR N9 // LIVE CONTRACT", new Vector2(-228f, 392f), new Vector2(360f, 22f), 14, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0.24f));
        CreateText(canvas.transform, font, "CYBER DRIFT", new Vector2(-176f, 352f), new Vector2(520f, 56f), 46, TextAnchor.MiddleLeft, Color.white);
        CreateText(canvas.transform, font, "RUNNER", new Vector2(-176f, 312f), new Vector2(420f, 56f), 50, TextAnchor.MiddleLeft, new Color(0.2f, 0.96f, 1f));
        CreateText(canvas.transform, font, "NEON COURIER // HACK // EVADE // SURVIVE", new Vector2(-178f, 274f), new Vector2(460f, 24f), 14, TextAnchor.MiddleLeft, new Color(0.72f, 0.9f, 1f));
        CreateText(canvas.transform, font, "NIGHT SHIFT DELIVERY", new Vector2(-356f, 244f), new Vector2(246f, 28f), 16, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
        CreateText(canvas.transform, font, "HOT CONTRACT", new Vector2(118f, 82f), new Vector2(210f, 24f), 18, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.22f));
        CreateText(canvas.transform, font, "Courier lane compromised.\nPush the run and break contact.", new Vector2(118f, 46f), new Vector2(258f, 42f), 13, TextAnchor.MiddleCenter, new Color(0.7f, 0.88f, 1f));

        Button playButton = CreateButton(canvas.transform, font, "Play", new Vector2(-344f, 100f), new Vector2(238f, 76f));
        Button skinsButton = CreateButton(canvas.transform, font, "Garage", new Vector2(-344f, 10f), new Vector2(238f, 64f));
        Button upgradesButton = CreateButton(canvas.transform, font, "Upgrades", new Vector2(-344f, -68f), new Vector2(238f, 64f));
        Button leaderboardButton = CreateButton(canvas.transform, font, "Leaderboard", new Vector2(-344f, -146f), new Vector2(238f, 64f));
        Button settingsButton = CreateButton(canvas.transform, font, "Settings", new Vector2(-344f, -224f), new Vector2(238f, 64f));
        Button quitButton = CreateButton(canvas.transform, font, "Quit", new Vector2(-344f, -316f), new Vector2(238f, 60f));

        CreateText(profilePanel.transform, font, "RUNNER PROFILE", new Vector2(0f, 102f), new Vector2(300f, 34f), 24, TextAnchor.MiddleCenter, Color.cyan);
        Text levelText = CreateText(profilePanel.transform, font, "Level 1", new Vector2(0f, 54f), new Vector2(280f, 30f), 21, TextAnchor.MiddleCenter, Color.white);
        Text currencyText = CreateText(profilePanel.transform, font, "0", new Vector2(0f, 14f), new Vector2(320f, 30f), 20, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.24f));
        Text dailyRewardText = CreateText(profilePanel.transform, font, "Claim daily reward", new Vector2(0f, -24f), new Vector2(320f, 30f), 15, TextAnchor.MiddleCenter, new Color(0.72f, 0.92f, 1f));
        Text streakText = CreateText(profilePanel.transform, font, "Streak", new Vector2(0f, -54f), new Vector2(320f, 22f), 13, TextAnchor.MiddleCenter, new Color(0.64f, 0.88f, 1f));
        Text routeText = CreateText(profilePanel.transform, font, "Current Route  //  Neon Gateway", new Vector2(0f, -88f), new Vector2(330f, 24f), 15, TextAnchor.MiddleCenter, new Color(0.42f, 1f, 0.72f));
        Text bestRunText = CreateText(profilePanel.transform, font, "Best 000000  //  0m", new Vector2(0f, -118f), new Vector2(330f, 24f), 15, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.24f));
        Button dailyRewardButton = CreateButton(profilePanel.transform, font, "Claim", new Vector2(0f, -156f), new Vector2(154f, 42f));
        controller.BindProgress(levelText, currencyText, dailyRewardText);
        controller.BindProfileDetails(routeText, bestRunText, null, streakText);

        CreateText(loadoutPanel.transform, font, "ACTIVE LOADOUT", new Vector2(0f, 62f), new Vector2(280f, 30f), 22, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.24f));
        Text loadoutText = CreateText(loadoutPanel.transform, font, "Street Default\nDefault Trail  //  Standard Blaster", new Vector2(0f, -2f), new Vector2(320f, 62f), 16, TextAnchor.MiddleCenter, new Color(0.76f, 0.9f, 1f));
        CreateText(loadoutPanel.transform, font, "Cosmetics, trail, and weapon finish", new Vector2(0f, -48f), new Vector2(300f, 20f), 12, TextAnchor.MiddleCenter, new Color(0.56f, 0.78f, 1f));
        controller.BindProfileDetails(routeText, bestRunText, loadoutText, streakText);

        GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0f, 0f), new Vector2(480f, 300f));
        settingsPanel.SetActive(false);
        Text soundLabel = CreateText(settingsPanel.transform, font, "Sound", new Vector2(-120f, 60f), new Vector2(200f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text vibrationLabel = CreateText(settingsPanel.transform, font, "Vibration", new Vector2(-120f, 0f), new Vector2(200f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text soundValue = CreateText(settingsPanel.transform, font, "On", new Vector2(90f, 60f), new Vector2(100f, 40f), 24, TextAnchor.MiddleCenter, Color.cyan);
        Text vibrationValue = CreateText(settingsPanel.transform, font, "On", new Vector2(90f, 0f), new Vector2(100f, 40f), 24, TextAnchor.MiddleCenter, Color.cyan);
        Button soundButton = CreateButton(settingsPanel.transform, font, "Toggle", new Vector2(160f, 60f), new Vector2(120f, 44f));
        Button vibrationButton = CreateButton(settingsPanel.transform, font, "Toggle", new Vector2(160f, 0f), new Vector2(120f, 44f));
        Button settingsClose = CreateButton(settingsPanel.transform, font, "Close", new Vector2(0f, -90f), new Vector2(180f, 52f));
        controller.Configure(settingsPanel, soundValue, vibrationValue);

        GameObject shopPanel = CreatePanel(canvas.transform, "ShopPanel", new Vector2(0f, 0f), new Vector2(760f, 640f));
        CreateText(shopPanel.transform, font, "GARAGE", new Vector2(0f, 276f), new Vector2(320f, 42f), 34, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.24f));
        Text bankText = CreateText(shopPanel.transform, font, "Bank 0", new Vector2(0f, 236f), new Vector2(360f, 34f), 24, TextAnchor.MiddleCenter, Color.cyan);
        CreateText(shopPanel.transform, font, "Skins, trails, weapon finishes, and premium bundles", new Vector2(0f, 204f), new Vector2(520f, 24f), 16, TextAnchor.MiddleCenter, new Color(0.66f, 0.86f, 1f));
        RectTransform shopContent = CreateContentRoot(shopPanel.transform, new Vector2(680f, 520f), -56f);
        SkinShopController shopController = shopPanel.AddComponent<SkinShopController>();
        shopController.Configure(shopPanel, bankText, shopContent, font);
        controller.BindShop(shopController);
        shopPanel.SetActive(false);

        GameObject leaderboardPanel = CreatePanel(canvas.transform, "LeaderboardPanel", new Vector2(0f, 0f), new Vector2(720f, 600f));
        CreateText(leaderboardPanel.transform, font, "LEADERBOARD", new Vector2(0f, 230f), new Vector2(420f, 44f), 32, TextAnchor.MiddleCenter, Color.cyan);
        CreateText(leaderboardPanel.transform, font, "Night Shift city rankings", new Vector2(0f, 196f), new Vector2(420f, 24f), 16, TextAnchor.MiddleCenter, new Color(0.66f, 0.86f, 1f));
        RectTransform leaderboardContent = CreateContentRoot(leaderboardPanel.transform, new Vector2(650f, 500f), -62f);
        LeaderboardPanel lbController = leaderboardPanel.AddComponent<LeaderboardPanel>();
        lbController.Configure(leaderboardPanel, leaderboardContent, font);
        controller.BindLeaderboard(lbController);
        leaderboardPanel.SetActive(false);

        GameObject upgradePanel = CreatePanel(canvas.transform, "UpgradeShopPanel", new Vector2(0f, 0f), new Vector2(760f, 720f));
        Text upgradeCurrency = CreateText(upgradePanel.transform, font, "Credits: 0", new Vector2(0f, 300f), new Vector2(360f, 40f), 24, TextAnchor.MiddleCenter, Color.yellow);
        CreateText(upgradePanel.transform, font, "UPGRADES", new Vector2(0f, 256f), new Vector2(320f, 44f), 32, TextAnchor.MiddleCenter, Color.cyan);
        CreateText(upgradePanel.transform, font, "Mobility, combat, and support tuning", new Vector2(0f, 224f), new Vector2(460f, 24f), 16, TextAnchor.MiddleCenter, new Color(0.66f, 0.86f, 1f));
        RectTransform upgradeContent = CreateContentRoot(upgradePanel.transform, new Vector2(680f, 600f), -62f);
        UpgradeShopController upgradeShop = upgradePanel.AddComponent<UpgradeShopController>();
        upgradeShop.Configure(upgradePanel, upgradeContent, upgradeCurrency, font);
        Button upgradeClose = CreateButton(upgradePanel.transform, font, "Close", new Vector2(0f, -320f), new Vector2(180f, 52f));
        UnityEventTools.AddPersistentListener(upgradeClose.onClick, upgradeShop.TogglePanel);
        upgradePanel.SetActive(false);

        GameObject starterRoot = new GameObject("StarterPackOffer");
        starterRoot.transform.SetParent(canvas.transform, false);
        StarterPackOffer starterPack = starterRoot.AddComponent<StarterPackOffer>();
        GameObject starterPanel = CreatePanel(canvas.transform, "StarterPackPanel", Vector2.zero, new Vector2(500f, 400f));
        Text starterTitle = CreateText(starterPanel.transform, font, "STARTER PACK", new Vector2(0f, 150f), new Vector2(400f, 50f), 36, TextAnchor.MiddleCenter, Color.yellow);
        Text starterDesc = CreateText(starterPanel.transform, font, string.Empty, new Vector2(0f, 40f), new Vector2(380f, 100f), 20, TextAnchor.MiddleCenter, Color.white);
        Text starterPrice = CreateText(starterPanel.transform, font, "$0.99", new Vector2(0f, -40f), new Vector2(200f, 40f), 28, TextAnchor.MiddleCenter, Color.green);
        Button starterBuy = CreateButton(starterPanel.transform, font, "BUY NOW", new Vector2(-80f, -120f), new Vector2(160f, 60f));
        Button starterDismiss = CreateButton(starterPanel.transform, font, "No Thanks", new Vector2(80f, -120f), new Vector2(160f, 60f));
        starterPack.Configure(starterPanel, starterTitle, starterDesc, starterPrice, starterBuy, starterDismiss);
        starterPanel.SetActive(false);

        UnityEventTools.AddPersistentListener(playButton.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(dailyRewardButton.onClick, controller.ClaimDailyReward);
        UnityEventTools.AddPersistentListener(skinsButton.onClick, controller.ToggleShop);
        UnityEventTools.AddPersistentListener(upgradesButton.onClick, upgradeShop.TogglePanel);
        UnityEventTools.AddPersistentListener(leaderboardButton.onClick, controller.ToggleLeaderboard);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.ToggleSettings);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);
        UnityEventTools.AddPersistentListener(soundButton.onClick, controller.ToggleSound);
        UnityEventTools.AddPersistentListener(vibrationButton.onClick, controller.ToggleVibration);
        UnityEventTools.AddPersistentListener(settingsClose.onClick, controller.ToggleSettings);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.MainMenu}.unity");
    }

    private static void CreateGameOverScene(BootstrapAssets assets, GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.GameOver;

        CreatePersistentSystems(configs);
        CreateDirectionalLight();
        CreateStandaloneCamera(new Vector3(0f, 2f, -8f), new Vector3(15f, 0f, 0f));
        CreateGameOverShowcase(assets);
        CreateEventSystem();

        GameObject controllerObject = new GameObject("GameOverUI");
        GameOverController controller = controllerObject.AddComponent<GameOverController>();
        Canvas canvas = CreateCanvas("GameOverCanvas");
        Font font = GetBuiltinFont();
        UIFlowController uiFlow = new GameObject("UIFlowController").AddComponent<UIFlowController>();
        SetSerializedField(uiFlow, "gameOverCanvas", canvas);
        SetSerializedField(uiFlow, "visualTheme", configs.UiTheme);

        CreateMenuBackdropUi(canvas.transform);
        CreatePanel(canvas.transform, "GameOverPanel", new Vector2(0f, -40f), new Vector2(700f, 760f));
        CreatePanel(canvas.transform, "ResultsHeaderPanel", new Vector2(0f, 304f), new Vector2(520f, 92f));
        CreatePanel(canvas.transform, "RewardInfoPanel", new Vector2(0f, -236f), new Vector2(640f, 118f));
        CreateText(canvas.transform, font, "Run Terminated", new Vector2(0f, 340f), new Vector2(700f, 100f), 48, TextAnchor.MiddleCenter, Color.white);
        CreateText(canvas.transform, font, "MISSION DEBRIEF", new Vector2(0f, 300f), new Vector2(380f, 36f), 20, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.24f));
        Text scoreText = CreateText(canvas.transform, font, "Score 000000", new Vector2(0f, 220f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text distanceText = CreateText(canvas.transform, font, "Distance 0m", new Vector2(0f, 170f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text creditsText = CreateText(canvas.transform, font, "Credits 0", new Vector2(0f, 120f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text survivalText = CreateText(canvas.transform, font, "Survival 0.0s", new Vector2(0f, 70f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text highScoreText = CreateText(canvas.transform, font, "Best Score 000000", new Vector2(0f, 10f), new Vector2(500f, 40f), 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.7f, 0.3f));
        Text bestDistText = CreateText(canvas.transform, font, "Best Distance 0m", new Vector2(0f, -30f), new Vector2(500f, 40f), 22, TextAnchor.MiddleCenter, new Color(0.9f, 0.7f, 0.3f));
        Text newHighLabel = CreateText(canvas.transform, font, "★ NEW HIGH SCORE ★", new Vector2(0f, 280f), new Vector2(600f, 50f), 32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.15f));
        Text nearBestText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, -70f), new Vector2(600f, 40f), 22, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.3f));
        Text rankText = CreateText(canvas.transform, font, "Rank #1", new Vector2(0f, -110f), new Vector2(300f, 40f), 22, TextAnchor.MiddleCenter, Color.white);
        Text xpText = CreateText(canvas.transform, font, "+0 XP", new Vector2(0f, -150f), new Vector2(300f, 40f), 22, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.5f));
        Text challengeText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, -190f), new Vector2(600f, 40f), 20, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.2f));
        Text tipText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, -235f), new Vector2(760f, 44f), 20, TextAnchor.MiddleCenter, new Color(0.72f, 0.82f, 1f));
        Text doubleRewardText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, -272f), new Vector2(620f, 36f), 18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        Text districtInfoText = CreateText(canvas.transform, font, "District // Neon Gateway", new Vector2(-176f, -178f), new Vector2(260f, 30f), 18, TextAnchor.MiddleCenter, new Color(0.34f, 1f, 0.72f));
        Text gradeText = CreateText(canvas.transform, font, "Grade C", new Vector2(182f, -178f), new Vector2(180f, 34f), 24, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.24f));
        Text rewardTitleText = CreateText(canvas.transform, font, "Run Cache", new Vector2(0f, -214f), new Vector2(520f, 34f), 24, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.24f));
        Text rewardDetailText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, -246f), new Vector2(580f, 40f), 18, TextAnchor.MiddleCenter, new Color(0.74f, 0.86f, 1f));

        Button retryButton = CreateButton(canvas.transform, font, "Retry", new Vector2(0f, -330f), new Vector2(220f, 70f));
        Button doubleRewardButton = CreateButton(canvas.transform, font, "2x Rewards", new Vector2(0f, -414f), new Vector2(220f, 70f));
        Button menuButton = CreateButton(canvas.transform, font, "Main Menu", new Vector2(0f, -498f), new Vector2(220f, 70f));
        Button shareButton = CreateButton(canvas.transform, font, "Share", new Vector2(0f, -572f), new Vector2(220f, 60f));

        controller.Configure(scoreText, distanceText, creditsText, survivalText);
        SetTextField(controller, "highScoreText", highScoreText);
        SetTextField(controller, "bestDistanceText", bestDistText);
        SetTextField(controller, "newHighScoreLabel", newHighLabel);
        SetTextField(controller, "nearBestText", nearBestText);
        SetTextField(controller, "leaderboardRankText", rankText);
        SetTextField(controller, "xpGainText", xpText);
        SetTextField(controller, "dailyChallengeText", challengeText);
        SetTextField(controller, "tipText", tipText);
        SetTextField(controller, "districtText", districtInfoText);
        SetTextField(controller, "gradeText", gradeText);
        SetTextField(controller, "rewardTitleText", rewardTitleText);
        SetTextField(controller, "rewardDetailText", rewardDetailText);
        controller.BindDoubleRewards(doubleRewardButton, doubleRewardText);

        UnityEventTools.AddPersistentListener(retryButton.onClick, controller.Retry);
        UnityEventTools.AddPersistentListener(doubleRewardButton.onClick, controller.ClaimDoubleRewards);
        UnityEventTools.AddPersistentListener(menuButton.onClick, controller.BackToMenu);
        UnityEventTools.AddPersistentListener(shareButton.onClick, controller.ShareScore);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameOver}.unity");
    }

    private static void CreatePersistentSystems(GameplayConfigBootstrapper.ConfigBundle configs)
    {
        GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
        new GameObject("MonetizationManager").AddComponent<MonetizationManager>();
        AudioManager audioManager = new GameObject("AudioManager").AddComponent<AudioManager>();
        new GameObject("MobilePerformanceManager").AddComponent<MobilePerformanceManager>();
        new GameObject("XpLevelSystem").AddComponent<XpLevelSystem>();
        new GameObject("DailyRewardSystem").AddComponent<DailyRewardSystem>();
        new GameObject("DailyChallengeSystem").AddComponent<DailyChallengeSystem>();
        new GameObject("LimitedTimeEventSystem").AddComponent<LimitedTimeEventSystem>();
        new GameObject("LiveOpsSystem").AddComponent<LiveOpsSystem>();
        new GameObject("MissionSystem").AddComponent<MissionSystem>();
        new GameObject("AchievementSystem").AddComponent<AchievementSystem>();
        new GameObject("UpgradeSystem").AddComponent<UpgradeSystem>();
        new GameObject("EconomySystem").AddComponent<EconomySystem>();
        new GameObject("ShopSystem").AddComponent<ShopSystem>();
        LeaderboardSystem leaderboard = new GameObject("LeaderboardSystem").AddComponent<LeaderboardSystem>();
        MockLeaderboardTransport leaderboardTransport = leaderboard.gameObject.AddComponent<MockLeaderboardTransport>();
        new GameObject("SeasonPassSystem").AddComponent<SeasonPassSystem>();
        new GameObject("MonetizationV2").AddComponent<MonetizationV2>();
        new GameObject("AnalyticsManager").AddComponent<AnalyticsManager>();
        new GameObject("ShareManager").AddComponent<ShareManager>();
        SettingsManager settingsManager = new GameObject("SettingsManager").AddComponent<SettingsManager>();
        new GameObject("GooglePlayManager").AddComponent<GooglePlayManager>();
        new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
        new GameObject("GhostRunManager").AddComponent<GhostRunManager>();
        new GameObject("RunAnalyticsStore").AddComponent<RunAnalyticsStore>();
        new GameObject("NetworkSessionManager").AddComponent<NetworkSessionManager>();
        new GameObject("NotificationScheduler").AddComponent<NotificationScheduler>();
        new GameObject("TipSystem").AddComponent<TipSystem>();
        new GameObject("ConsentManager").AddComponent<ConsentManager>();
        new GameObject("SceneLoader").AddComponent<SceneLoader>();
        new GameObject("RateAppPrompt").AddComponent<RateAppPrompt>();
        new GameObject("GameServicesBootstrapper").AddComponent<GameServicesBootstrapper>();
        SetSerializedField(gameManager, "balanceConfig", configs.RunnerBalance);
        SetSerializedField(settingsManager, "qualityConfig", configs.VisualQuality);
        SetSerializedField(leaderboard, "transportBehaviour", leaderboardTransport);
        SetSerializedField(audioManager, "styleProfile", configs.AudioStyle);
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.34f;
        light.color = new Color(0.64f, 0.82f, 1f);
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(31f, -24f, 0f);

        GameObject fillObject = new GameObject("Atmosphere Fill");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.42f;
        fill.color = new Color(1f, 0.34f, 0.58f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(12f, 138f, 0f);
    }

    private static void CreateEnvironment(BootstrapAssets assets)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.position = new Vector3(0f, -0.25f, 5000f);
        road.transform.localScale = new Vector3(10.2f, 0.5f, 10000f);
        road.GetComponent<Renderer>().sharedMaterial = assets.RoadMaterial;

        Material sidewalk = CreateMaterial("Sidewalk", new Color(0.05f, 0.05f, 0.09f), new Color(0f, 0f, 0f));
        Material barrierMetal = CreateMaterial("CityBarrierMetal", new Color(0.05f, 0.06f, 0.08f), new Color(0f, 0f, 0f));
        CreateGroundStrip("LeftWalkway", new Vector3(-7.7f, -0.08f, 5000f), new Vector3(4.2f, 0.24f, 10000f), sidewalk);
        CreateGroundStrip("RightWalkway", new Vector3(7.7f, -0.08f, 5000f), new Vector3(4.2f, 0.24f, 10000f), sidewalk);
        CreateGroundStrip("LeftBarrier", new Vector3(-5.45f, 0.65f, 5000f), new Vector3(0.18f, 1.3f, 10000f), barrierMetal);
        CreateGroundStrip("RightBarrier", new Vector3(5.45f, 0.65f, 5000f), new Vector3(0.18f, 1.3f, 10000f), barrierMetal);
        CreateGroundStrip("LeftGlowRail", new Vector3(-5.18f, 1.12f, 5000f), new Vector3(0.06f, 0.05f, 10000f), assets.AccentMaterial);
        CreateGroundStrip("RightGlowRail", new Vector3(5.18f, 1.12f, 5000f), new Vector3(0.06f, 0.05f, 10000f), assets.AlternateAccentMaterial);
        CreateRoadSurfaceBreakup(assets, sidewalk, barrierMetal);
        CreateBackdropBand(-34f, 26f, assets.TertiaryAccentMaterial);
        CreateBackdropBand(34f, 29f, assets.AccentMaterial);
        CreateGroundStrip("CenterEnergySpine", new Vector3(0f, -0.04f, 5000f), new Vector3(0.22f, 0.02f, 10000f), assets.TertiaryAccentMaterial);
        CreateSkyGlowBands(assets);
        CreateAtmosphericBackdrop(assets);
        CreateForegroundFrame(assets);
        CreateNearCameraDistrictSetDressing(assets);
        CreateHeroCorridorShell(assets);
        CreateLaunchSetPiece(assets);
        CreateLaunchRunwayFrames(assets);
        CreateBossApproachLandmarks(assets);

        CreateLaneMarker(-2.5f, assets.AccentMaterial);
        CreateLaneMarker(0f, assets.AccentMaterial);
        CreateLaneMarker(2.5f, assets.AccentMaterial);

        for (int i = 0; i < 22; i++)
        {
            float z = 120f + (i * 180f);
            int districtIndex = z < 900f ? 0 : (z < 1900f ? 1 : 2);
            Material districtPrimary = districtIndex == 0 ? assets.AccentMaterial : districtIndex == 1 ? assets.AlternateAccentMaterial : assets.TertiaryAccentMaterial;
            Material districtSecondary = districtIndex == 0 ? assets.TertiaryAccentMaterial : districtIndex == 1 ? assets.WarningMaterial : assets.AccentMaterial;
            Material districtWarning = districtIndex == 2 ? assets.WarningMaterial : assets.AlternateAccentMaterial;
            float skylineOffset = districtIndex == 1 ? 14.2f : 15.5f;
            CreateStreetGate(z, districtPrimary, CreateMaterial("GateMetal", new Color(0.08f, 0.08f, 0.14f), new Color(0f, 0f, 0f)));
            CreateSkylineCluster(z, -skylineOffset, districtPrimary);
            CreateSkylineCluster(z + 50f, skylineOffset, districtSecondary);
            CreateBillboard(z + 26f, districtIndex == 1 ? -10.4f : -11.8f, districtPrimary);
            CreateBillboard(z + 82f, districtIndex == 2 ? 10.2f : 11.8f, districtWarning);
            CreateSideStructure(z + 18f, districtIndex == 2 ? -9.6f : -8.6f, districtPrimary, false);
            CreateSideStructure(z + 96f, districtIndex == 1 ? 7.9f : 8.6f, districtSecondary, true);
            CreateDataSpire(z + 42f, districtIndex == 0 ? -12.8f : -13.4f, districtSecondary);
            CreateDataSpire(z + 122f, districtIndex == 2 ? 12.6f : 13.4f, districtWarning);

            switch (districtIndex)
            {
                case 0:
                    CreateDistrictGatewayCluster(z + 38f, assets);
                    if (i % 2 == 0)
                    {
                        CreateSkyBridge(z + 54f, districtSecondary);
                    }
                     break;
                case 1:
                    CreateDistrictMarketCluster(z + 46f, assets);
                    CreateSkyBridge(z + 54f, districtPrimary);
                    if (i % 2 == 1)
                    {
                        CreateSkyBridge(z + 118f, districtSecondary);
                    }
                    break;
                default:
                    CreateDistrictSecurityCluster(z + 44f, assets);
                    CreateNeonTunnelSegment(z + 108f, districtPrimary, districtSecondary);
                    if (i % 2 == 0)
                    {
                        CreateSecurityArray(z + 84f, assets.WarningMaterial, assets.TertiaryAccentMaterial);
                    }
                    break;
            }
        }

        CreateTrafficStream(-12.2f, assets.WarningMaterial);
        CreateTrafficStream(12.2f, assets.AlternateAccentMaterial);
    }

    private static void CreateLaneMarker(float xPosition, Material material)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = $"LaneMarker_{xPosition:0}";
        marker.transform.position = new Vector3(xPosition, 0.02f, 5000f);
        marker.transform.localScale = new Vector3(0.08f, 0.04f, 10000f);
        marker.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());

        GameObject pulseStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pulseStrip.name = $"LaneGlow_{xPosition:0}";
        pulseStrip.transform.position = new Vector3(xPosition, 0.08f, 5000f);
        pulseStrip.transform.localScale = new Vector3(0.28f, 0.02f, 10000f);
        pulseStrip.GetComponent<Renderer>().sharedMaterial = CreateMaterial("LaneGlow", new Color(0.08f, 0.2f, 0.28f), new Color(0.2f, 0.92f, 1f));
        Object.DestroyImmediate(pulseStrip.GetComponent<BoxCollider>());
        pulseStrip.AddComponent<NeonPropAnimator>().Configure(new Color(0.2f, 0.92f, 1f), 3f, 0.7f, 1.55f);
    }

    private static void CreateLaunchRunwayFrames(BootstrapAssets assets)
    {
        Material frameShell = CreateMaterial("LaunchGateShell", new Color(0.055f, 0.065f, 0.09f), Color.black);
        float[] zPositions = { 22f, 48f, 78f, 114f };
        for (int i = 0; i < zPositions.Length; i++)
        {
            float z = zPositions[i];
            Material frameAccent = i % 2 == 0 ? assets.AccentMaterial : assets.AlternateAccentMaterial;
            Material warningAccent = i % 2 == 0 ? assets.WarningMaterial : assets.TertiaryAccentMaterial;

            CreateGroundStrip($"LaunchFrameLeftLeg_{i}", new Vector3(-6.2f, 3.2f, z), new Vector3(0.26f, 6.4f, 0.26f), frameShell);
            CreateGroundStrip($"LaunchFrameRightLeg_{i}", new Vector3(6.2f, 3.2f, z), new Vector3(0.26f, 6.4f, 0.26f), frameShell);
            CreateGroundStrip($"LaunchFrameTop_{i}", new Vector3(0f, 6.26f, z), new Vector3(12.8f, 0.18f, 0.42f), frameShell);
            CreateGroundStrip($"LaunchFrameUnderglow_{i}", new Vector3(0f, 6.04f, z), new Vector3(10.2f, 0.05f, 0.1f), frameAccent);
            CreateGroundStrip($"LaunchFrameWarningLeft_{i}", new Vector3(-5.34f, 1.62f, z), new Vector3(0.08f, 1.4f, 0.1f), warningAccent);
            CreateGroundStrip($"LaunchFrameWarningRight_{i}", new Vector3(5.34f, 1.62f, z), new Vector3(0.08f, 1.4f, 0.1f), warningAccent);
            CreateGroundStrip($"LaunchFrameScreen_{i}", new Vector3(0f, 4.86f, z), new Vector3(3.6f, 0.72f, 0.08f), frameAccent);
        }

        CreateGroundStrip("LaunchTrafficRibbonLeft", new Vector3(-9.6f, 4.8f, 80f), new Vector3(0.22f, 0.08f, 180f), assets.WarningMaterial);
        CreateGroundStrip("LaunchTrafficRibbonRight", new Vector3(9.6f, 5.2f, 92f), new Vector3(0.22f, 0.08f, 200f), assets.AlternateAccentMaterial);
    }

    private static Camera CreateCamera(Transform target)
    {
        return CreateStandaloneCamera(new Vector3(2.18f, 5.22f, -12.15f), new Vector3(18f, -9.5f, 0f));
    }

    private static Camera CreateStandaloneCamera(Vector3 position, Vector3 rotation)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.backgroundColor = s_uiTheme != null ? s_uiTheme.CanvasBackground : new Color(0.01f, 0.015f, 0.04f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.farClipPlane = 360f;
        camera.orthographic = false;
        camera.fieldOfView = 60f;
        camera.allowHDR = true;
        UniversalAdditionalCameraData urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = true;
        urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraObject.transform.position = position;
        cameraObject.transform.rotation = Quaternion.Euler(rotation);
        return camera;
    }

    private static Canvas CreateHudCanvas(PlayerController player, Camera camera, PauseController pauseCtrl, out HoldButton holdButton)
    {
        Canvas canvas = CreateCanvas("HUDCanvas");
        canvas.worldCamera = camera;
        Font font = GetBuiltinFont();

        GameObject hudRoot = new GameObject("HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        HUDController hudController = hudRoot.AddComponent<HUDController>();

        GameObject infoPanel = CreatePanel(canvas.transform, "HudInfoPanel", Vector2.zero, new Vector2(396f, 208f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(0f, 1f);
        infoRect.pivot = new Vector2(0f, 1f);
        infoRect.anchoredPosition = new Vector2(24f, -22f);
        GameObject topBanner = CreatePanel(canvas.transform, "TopBannerPanel", Vector2.zero, new Vector2(520f, 128f));
        RectTransform topBannerRect = topBanner.GetComponent<RectTransform>();
        topBannerRect.anchorMin = new Vector2(0.5f, 1f);
        topBannerRect.anchorMax = new Vector2(0.5f, 1f);
        topBannerRect.pivot = new Vector2(0.5f, 1f);
        topBannerRect.anchoredPosition = new Vector2(0f, -18f);
        GameObject powerTray = CreatePanel(canvas.transform, "PowerTrayPanel", Vector2.zero, new Vector2(332f, 110f));
        RectTransform powerTrayRect = powerTray.GetComponent<RectTransform>();
        powerTrayRect.anchorMin = new Vector2(0f, 1f);
        powerTrayRect.anchorMax = new Vector2(0f, 1f);
        powerTrayRect.pivot = new Vector2(0f, 1f);
        powerTrayRect.anchoredPosition = new Vector2(24f, -246f);
        GameObject fpsPanel = CreatePanel(canvas.transform, "FpsPanel", Vector2.zero, new Vector2(132f, 52f));
        RectTransform fpsPanelRect = fpsPanel.GetComponent<RectTransform>();
        fpsPanelRect.anchorMin = new Vector2(1f, 1f);
        fpsPanelRect.anchorMax = new Vector2(1f, 1f);
        fpsPanelRect.pivot = new Vector2(1f, 1f);
        fpsPanelRect.anchoredPosition = new Vector2(-24f, -22f);

        CreateText(infoPanel.transform, font, "RUN DATA", new Vector2(-94f, 72f), new Vector2(152f, 24f), 15, TextAnchor.MiddleLeft, new Color(0.22f, 0.95f, 1f));
        CreateText(infoPanel.transform, font, "CONTRACT TRACK", new Vector2(-94f, -66f), new Vector2(188f, 22f), 14, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.22f));
        CreateText(powerTray.transform, font, "ACTIVE KIT", new Vector2(-42f, 20f), new Vector2(124f, 22f), 14, TextAnchor.MiddleLeft, new Color(0.22f, 0.95f, 1f));

        Text scoreText = CreateText(canvas.transform, font, "Score 000000", new Vector2(134f, -38f), new Vector2(248f, 34f), 21, TextAnchor.MiddleLeft, Color.white);
        Text distanceText = CreateText(canvas.transform, font, "Distance 0m", new Vector2(134f, -72f), new Vector2(248f, 34f), 21, TextAnchor.MiddleLeft, Color.white);
        Text creditsText = CreateText(canvas.transform, font, "Credits 0", new Vector2(134f, -106f), new Vector2(248f, 34f), 21, TextAnchor.MiddleLeft, Color.white);
        Text powerUpText = CreateText(canvas.transform, font, "WEAPON // STANDARD BLASTER", new Vector2(132f, -148f), new Vector2(344f, 34f), 19, TextAnchor.MiddleLeft, new Color(0.72f, 0.88f, 1f));
        Text missionText = CreateText(canvas.transform, font, "Mission", new Vector2(140f, -188f), new Vector2(344f, 34f), 18, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
        Text bossText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 104f), new Vector2(460f, 40f), 25, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f));
        Text comboText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 158f), new Vector2(300f, 48f), 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.15f));
        Text zoneText = CreateText(canvas.transform, font, "DISTRICT // NEON GATEWAY", new Vector2(0f, 206f), new Vector2(640f, 58f), 24, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.5f));
        Text feverText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 254f), new Vector2(440f, 48f), 26, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.15f));
        Text fpsText = CreateText(canvas.transform, font, string.Empty, new Vector2(84f, -28f), new Vector2(120f, 28f), 18, TextAnchor.MiddleLeft, Color.green);
        RectTransform fpsRect = fpsText.GetComponent<RectTransform>();
        fpsRect.anchorMin = new Vector2(0f, 1f);
        fpsRect.anchorMax = new Vector2(0f, 1f);
        comboText.gameObject.SetActive(false);
        bossText.gameObject.SetActive(false);
        zoneText.gameObject.SetActive(true);
        feverText.gameObject.SetActive(false);
        fpsText.gameObject.SetActive(false);

        hudController.Configure(scoreText, distanceText, creditsText, powerUpText);
        SetTextField(hudController, "comboText", comboText);
        SetTextField(hudController, "zoneText", zoneText);
        SetTextField(hudController, "feverText", feverText);
        SetTextField(hudController, "fpsText", fpsText);
        SetTextField(hudController, "missionText", missionText);
        SetTextField(hudController, "bossText", bossText);
        camera.GetComponent<FpsCounter>()?.Configure(fpsText);

        Button pauseButton = CreateButton(canvas.transform, font, "| |", new Vector2(0f, 0f), new Vector2(88f, 60f));
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-56f, -34f);

        GameObject pausePanel = CreatePanel(canvas.transform, "PausePanel", Vector2.zero, new Vector2(500f, 400f));
        CreateText(pausePanel.transform, font, "PAUSED", new Vector2(0f, 140f), new Vector2(300f, 60f), 42, TextAnchor.MiddleCenter, Color.cyan);
        Button resumeBtn = CreateButton(pausePanel.transform, font, "Resume", new Vector2(0f, 30f), new Vector2(260f, 70f));
        Button quitBtn = CreateButton(pausePanel.transform, font, "Quit to Menu", new Vector2(0f, -70f), new Vector2(260f, 70f));
        pausePanel.SetActive(false);
        pauseCtrl.Configure(pausePanel, pauseButton, resumeBtn, quitBtn);

        GameObject hackButtonObject = new GameObject("HackButton", typeof(RectTransform), typeof(Image), typeof(HoldButton));
        hackButtonObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = hackButtonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-120f, 120f);
        rect.sizeDelta = new Vector2(160f, 160f);

        Image image = hackButtonObject.GetComponent<Image>();
        image.color = new Color(0.04f, 0.08f, 0.12f, 0.92f);
        Outline hackOutline = hackButtonObject.AddComponent<Outline>();
        hackOutline.effectColor = new Color(0.22f, 0.95f, 1f, 0.95f);
        hackOutline.effectDistance = new Vector2(3f, -3f);
        holdButton = hackButtonObject.GetComponent<HoldButton>();
        holdButton.Bind(player);

        GameObject hackAccent = new GameObject("HackAccent", typeof(RectTransform), typeof(Image));
        hackAccent.transform.SetParent(hackButtonObject.transform, false);
        RectTransform hackAccentRect = hackAccent.GetComponent<RectTransform>();
        hackAccentRect.anchorMin = new Vector2(0.1f, 0.85f);
        hackAccentRect.anchorMax = new Vector2(0.9f, 0.95f);
        hackAccentRect.offsetMin = Vector2.zero;
        hackAccentRect.offsetMax = Vector2.zero;
        hackAccent.GetComponent<Image>().color = new Color(0.18f, 1f, 1f, 0.85f);
        CreateHackRing(hackButtonObject.transform, new Color(0.42f, 0f, 1f, 0.35f));

        CreateText(hackButtonObject.transform, font, "HACK", Vector2.zero, new Vector2(140f, 40f), 28, TextAnchor.MiddleCenter, Color.white);
        CreateTutorialOverlay(canvas.transform, font);
        CreateReviveOverlay(canvas.transform, font);
        CreateScreenFlash(canvas.transform);
        return canvas;
    }

    private static void CreateScreenFlash(Transform canvasTransform)
    {
        GameObject flashObj = new GameObject("ScreenFlash", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        flashObj.transform.SetParent(canvasTransform, false);
        RectTransform rt = flashObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UnityEngine.UI.Image img = flashObj.GetComponent<UnityEngine.UI.Image>();
        img.color = Color.clear;
        img.raycastTarget = false;

        ScreenFlash flash = flashObj.AddComponent<ScreenFlash>();
        flash.SetFlashImage(img);
    }

    private static void CreateReviveOverlay(Transform canvasTransform, Font font)
    {
        GameObject overlayRoot = new GameObject("ReviveOverlay");
        ReviveOverlayController overlay = overlayRoot.AddComponent<ReviveOverlayController>();
        GameObject panel = CreatePanel(canvasTransform, "RevivePanel", Vector2.zero, new Vector2(720f, 340f));
        Text statusText = CreateText(panel.transform, font, "Watch a rewarded ad to continue this run once.", new Vector2(0f, 70f), new Vector2(620f, 80f), 30, TextAnchor.MiddleCenter, Color.white);
        Button watchButton = CreateButton(panel.transform, font, "Revive", new Vector2(0f, -10f), new Vector2(220f, 70f));
        Button skipButton = CreateButton(panel.transform, font, "Skip", new Vector2(0f, -100f), new Vector2(220f, 60f));
        overlay.Configure(panel, statusText, watchButton, skipButton);
        panel.SetActive(false);
    }

    private static void CreateTutorialOverlay(Transform canvasTransform, Font font)
    {
        GameObject panel = CreatePanel(canvasTransform, "TutorialPanel", Vector2.zero, new Vector2(900f, 500f));
        Image panelBg = panel.GetComponent<Image>();
        if (panelBg != null)
        {
            panelBg.color = new Color(0.02f, 0.02f, 0.08f, 0.92f);
        }

        CreateText(panel.transform, font, "HOW TO PLAY", new Vector2(0f, 180f), new Vector2(500f, 50f), 36, TextAnchor.MiddleCenter, Color.cyan);
        Text instructionText = CreateText(panel.transform, font, string.Empty, new Vector2(0f, 20f), new Vector2(700f, 200f), 30, TextAnchor.MiddleCenter, Color.white);
        Button dismissButton = CreateButton(panel.transform, font, "Next ▶", new Vector2(0f, -180f), new Vector2(220f, 70f));

        TutorialOverlay tutorial = panel.AddComponent<TutorialOverlay>();
        SetSerializedField(tutorial, "overlayRoot", panel);
        SetSerializedField(tutorial, "instructionText", instructionText);
        SetSerializedField(tutorial, "dismissButton", dismissButton);
    }

    private static EventSystem CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return eventSystemObject.GetComponent<EventSystem>();
    }

    private static GameObject CreateVisualPrimitive(string name, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material, Transform parent)
    {
        GameObject child = GameObject.CreatePrimitive(primitiveType);
        child.name = name;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localScale = localScale;
        child.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = child.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return child;
    }

    private static void CreateGroundStrip(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.position = position;
        strip.transform.localScale = scale;
        strip.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(strip.GetComponent<BoxCollider>());
    }

    private static void CreateRoadSurfaceBreakup(BootstrapAssets assets, Material sidewalk, Material barrierMetal)
    {
        Material shoulderShell = CreateMaterial("ChunkShoulderShell", new Color(0.05f, 0.06f, 0.09f), new Color(0f, 0f, 0f));
        Material shoulderLine = CreateMaterial("ChunkRoadShoulder", new Color(0.08f, 0.1f, 0.14f), new Color(0.16f, 0.88f, 1f));

        CreateGroundStrip("LeftLaneShoulder", new Vector3(-3.92f, -0.12f, 5000f), new Vector3(1.58f, 0.08f, 10000f), shoulderShell);
        CreateGroundStrip("RightLaneShoulder", new Vector3(3.92f, -0.12f, 5000f), new Vector3(1.58f, 0.08f, 10000f), shoulderShell);
        CreateGroundStrip("LeftShoulderLine", new Vector3(-4.78f, 0.04f, 5000f), new Vector3(0.16f, 0.03f, 10000f), shoulderLine);
        CreateGroundStrip("RightShoulderLine", new Vector3(4.78f, 0.04f, 5000f), new Vector3(0.16f, 0.03f, 10000f), assets.WarningMaterial);
        CreateGroundStrip("LeftWalkwayGlow", new Vector3(-7.72f, 0.06f, 5000f), new Vector3(1.46f, 0.02f, 10000f), assets.TertiaryAccentMaterial);
        CreateGroundStrip("RightWalkwayGlow", new Vector3(7.72f, 0.06f, 5000f), new Vector3(1.46f, 0.02f, 10000f), assets.AlternateAccentMaterial);

        for (int i = 0; i < 16; i++)
        {
            float z = 28f + (i * 34f);
            float width = i % 2 == 0 ? 1.36f : 0.96f;
            CreateGroundStrip($"LanePadCenter_{i}", new Vector3(0f, -0.02f, z), new Vector3(width, 0.03f, 6.8f), assets.AccentMaterial);
            CreateGroundStrip($"LanePadLeft_{i}", new Vector3(-3.98f, 0.02f, z + 6f), new Vector3(0.42f, 0.04f, 5.4f), assets.TertiaryAccentMaterial);
            CreateGroundStrip($"LanePadRight_{i}", new Vector3(3.98f, 0.02f, z + 18f), new Vector3(0.42f, 0.04f, 5.4f), assets.WarningMaterial);
        }

        CreateGroundStrip("ServiceTrenchLeft", new Vector3(-6.12f, -0.22f, 5000f), new Vector3(0.56f, 0.12f, 10000f), barrierMetal);
        CreateGroundStrip("ServiceTrenchRight", new Vector3(6.12f, -0.22f, 5000f), new Vector3(0.56f, 0.12f, 10000f), barrierMetal);
        CreateGroundStrip("LeftWalkwayMatte", new Vector3(-8.92f, -0.04f, 5000f), new Vector3(1.12f, 0.06f, 10000f), sidewalk);
        CreateGroundStrip("RightWalkwayMatte", new Vector3(8.92f, -0.04f, 5000f), new Vector3(1.12f, 0.06f, 10000f), sidewalk);
    }

    private static void CreateAtmosphericBackdrop(BootstrapAssets assets)
    {
        Material shell = CreateMaterial("ChunkWallShell", new Color(0.03f, 0.04f, 0.07f), Color.black);
        Material ribbon = CreateMaterial("TrafficRibbon", new Color(0.08f, 0.12f, 0.18f), new Color(0.18f, 0.92f, 1f));

        CreateGroundStrip("AtmosLeftWall", new Vector3(-17.8f, 11f, 260f), new Vector3(7.4f, 22f, 620f), shell);
        CreateGroundStrip("AtmosRightWall", new Vector3(17.8f, 12f, 260f), new Vector3(8.2f, 24f, 620f), shell);
        CreateGroundStrip("AtmosCeilingBand", new Vector3(0f, 20.8f, 184f), new Vector3(34f, 0.9f, 220f), shell);
        CreateGroundStrip("AtmosHoloRibbonLeft", new Vector3(-14.2f, 12.8f, 210f), new Vector3(0.36f, 9.2f, 260f), ribbon);
        CreateGroundStrip("AtmosHoloRibbonRight", new Vector3(14.6f, 13.1f, 250f), new Vector3(0.36f, 10.2f, 280f), assets.AlternateAccentMaterial);
        CreateGroundStrip("AtmosHorizonShelf", new Vector3(0f, 14.6f, 126f), new Vector3(42f, 1.4f, 64f), shell);
        CreateGroundStrip("AtmosHorizonGlow", new Vector3(0f, 14.8f, 126.2f), new Vector3(26f, 0.12f, 18f), assets.WarningMaterial);
        CreateGroundStrip("AtmosCloudShelfLeft", new Vector3(-10.8f, 18.4f, 150f), new Vector3(18f, 3.2f, 90f), assets.AccentMaterial);
        CreateGroundStrip("AtmosCloudShelfRight", new Vector3(12.6f, 20.2f, 178f), new Vector3(22f, 3.6f, 108f), assets.TertiaryAccentMaterial);
    }

    private static void CreateHeroCorridorShell(BootstrapAssets assets)
    {
        Material shell = CreateMaterial("GatewayClusterShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        Material signShell = CreateMaterial("SecurityGateShell", new Color(0.06f, 0.08f, 0.12f), new Color(0.18f, 0.92f, 1f) * 0.4f);

        CreateGroundStrip("HeroMassLeft", new Vector3(-10.8f, 5.8f, 52f), new Vector3(3.2f, 11.6f, 84f), shell);
        CreateGroundStrip("HeroMassRight", new Vector3(10.6f, 6.4f, 58f), new Vector3(3.8f, 12.8f, 96f), shell);
        CreateGroundStrip("HeroMassLeftTrim", new Vector3(-9.16f, 6.2f, 52.6f), new Vector3(0.18f, 8.6f, 64f), assets.AccentMaterial);
        CreateGroundStrip("HeroMassRightTrim", new Vector3(8.58f, 6.7f, 57.4f), new Vector3(0.18f, 9.4f, 74f), assets.WarningMaterial);
        CreateGroundStrip("HeroMassLeftSign", new Vector3(-8.8f, 4.8f, 28f), new Vector3(4.4f, 1.8f, 0.16f), signShell);
        CreateGroundStrip("HeroMassRightSign", new Vector3(8.8f, 5.3f, 42f), new Vector3(4.8f, 2f, 0.16f), signShell);
        CreateGroundStrip("HeroMassLeftSignGlow", new Vector3(-8.8f, 4.74f, 28.08f), new Vector3(3.2f, 0.08f, 0.04f), assets.AccentMaterial);
        CreateGroundStrip("HeroMassRightSignGlow", new Vector3(8.8f, 5.24f, 42.08f), new Vector3(3.4f, 0.08f, 0.04f), assets.AlternateAccentMaterial);

        for (int i = 0; i < 4; i++)
        {
            float z = 18f + (i * 22f);
            float height = 7.8f + (i * 0.3f);
            CreateGroundStrip($"HeroCrossBeam_{i}", new Vector3(0f, height, z), new Vector3(13.6f, 0.2f, 1.1f), shell);
            CreateGroundStrip($"HeroCrossBeamGlow_{i}", new Vector3(0f, height + 0.1f, z), new Vector3(8.8f, 0.06f, 0.14f), i % 2 == 0 ? assets.TertiaryAccentMaterial : assets.WarningMaterial);
            CreateGroundStrip($"HeroCrossBladeLeft_{i}", new Vector3(-6.12f, height - 1.44f, z), new Vector3(0.16f, 3f, 0.42f), assets.AccentMaterial);
            CreateGroundStrip($"HeroCrossBladeRight_{i}", new Vector3(6.12f, height - 1.44f, z), new Vector3(0.16f, 3f, 0.42f), assets.AlternateAccentMaterial);
        }

        CreateGroundStrip("HeroCanopyLeft", new Vector3(-4.9f, 3.3f, 31f), new Vector3(1.1f, 0.12f, 22f), assets.AccentMaterial);
        CreateGroundStrip("HeroCanopyRight", new Vector3(4.9f, 3.5f, 47f), new Vector3(1.1f, 0.12f, 24f), assets.WarningMaterial);
        CreateGroundStrip("HeroCenterPortal", new Vector3(0f, 4.9f, 66f), new Vector3(10.8f, 0.24f, 1.2f), signShell);
        CreateGroundStrip("HeroCenterPortalGlow", new Vector3(0f, 4.96f, 66f), new Vector3(8.2f, 0.08f, 0.1f), assets.TertiaryAccentMaterial);
    }

    private static void CreateSideStructure(float zPosition, float xPosition, Material accentMaterial, bool mirrored)
    {
        float direction = mirrored ? -1f : 1f;
        Material towerMaterial = CreateMaterial("DistrictTower", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0f, 0f));
        CreateGroundStrip($"StructureBase_{zPosition:0}_{xPosition:0}", new Vector3(xPosition, 2f, zPosition), new Vector3(2.5f, 4f, 3.6f), towerMaterial);
        CreateGroundStrip($"StructureTop_{zPosition:0}_{xPosition:0}", new Vector3(xPosition + (0.55f * direction), 4.55f, zPosition + 0.3f), new Vector3(1.2f, 1.1f, 2f), towerMaterial);
        CreateGroundStrip($"StructureTrim_{zPosition:0}_{xPosition:0}", new Vector3(xPosition + (1.2f * direction), 3.2f, zPosition + 1.72f), new Vector3(0.08f, 3.2f, 3.05f), accentMaterial);
        CreateGroundStrip($"StructureGlow_{zPosition:0}_{xPosition:0}", new Vector3(xPosition, 5.75f, zPosition + 0.6f), new Vector3(1.9f, 0.08f, 1.3f), accentMaterial);
        CreateGroundStrip($"StructureCanopy_{zPosition:0}_{xPosition:0}", new Vector3(xPosition + (0.85f * direction), 2.1f, zPosition - 1.42f), new Vector3(0.6f, 0.12f, 1.6f), accentMaterial);
    }

    private static void CreateSkyBridge(float zPosition, Material accentMaterial)
    {
        Material bridgeMetal = CreateMaterial("BridgeMetal", new Color(0.05f, 0.05f, 0.08f), new Color(0f, 0f, 0f));
        CreateGroundStrip($"BridgeSpan_{zPosition:0}", new Vector3(0f, 6.6f, zPosition), new Vector3(14f, 0.28f, 1.6f), bridgeMetal);
        CreateGroundStrip($"BridgeGlow_{zPosition:0}", new Vector3(0f, 6.78f, zPosition), new Vector3(11.2f, 0.05f, 0.12f), accentMaterial);
        CreateGroundStrip($"BridgeSideLeft_{zPosition:0}", new Vector3(-6.3f, 7.35f, zPosition), new Vector3(0.08f, 1.5f, 1.5f), accentMaterial);
        CreateGroundStrip($"BridgeSideRight_{zPosition:0}", new Vector3(6.3f, 7.35f, zPosition), new Vector3(0.08f, 1.5f, 1.5f), accentMaterial);
        CreateGroundStrip($"BridgeBraceLeft_{zPosition:0}", new Vector3(-4.2f, 5.6f, zPosition), new Vector3(0.12f, 2.2f, 0.3f), bridgeMetal);
        CreateGroundStrip($"BridgeBraceRight_{zPosition:0}", new Vector3(4.2f, 5.6f, zPosition), new Vector3(0.12f, 2.2f, 0.3f), bridgeMetal);
        CreateGroundStrip($"BridgeUnderglow_{zPosition:0}", new Vector3(0f, 6.34f, zPosition), new Vector3(9.2f, 0.06f, 0.16f), accentMaterial);
    }

    private static void CreateNeonTunnelSegment(float zPosition, Material primaryAccent, Material secondaryAccent)
    {
        for (int i = 0; i < 4; i++)
        {
            float inset = i * 0.55f;
            float height = 4.3f - (i * 0.34f);
            float halfWidth = 4.8f - inset;
            float z = zPosition + (i * 3.2f);
            Material accent = i % 2 == 0 ? primaryAccent : secondaryAccent;
            CreateGroundStrip($"TunnelLeft_{z:0}_{i}", new Vector3(-halfWidth, height * 0.5f, z), new Vector3(0.08f, height, 0.12f), accent);
            CreateGroundStrip($"TunnelRight_{z:0}_{i}", new Vector3(halfWidth, height * 0.5f, z), new Vector3(0.08f, height, 0.12f), accent);
            CreateGroundStrip($"TunnelTop_{z:0}_{i}", new Vector3(0f, height, z), new Vector3((halfWidth * 2f) + 0.08f, 0.08f, 0.12f), accent);
        }
    }

    private static void CreateBackdropBand(float xPosition, float baseHeight, Material accentMaterial)
    {
        Material shell = CreateMaterial("BackdropTower", new Color(0.04f, 0.04f, 0.08f), new Color(0f, 0f, 0f));
        for (int i = 0; i < 16; i++)
        {
            float height = baseHeight + ((i % 5) * 4.2f);
            float z = 180f + (i * 132f);
            float width = 3.4f + ((i % 3) * 0.8f);
            CreateGroundStrip($"Backdrop_{xPosition:0}_{i}", new Vector3(xPosition, height * 0.5f, z), new Vector3(width, height, 8f), shell);
            CreateGroundStrip($"BackdropTrim_{xPosition:0}_{i}", new Vector3(xPosition, height - 1.1f, z + 3.7f), new Vector3(width * 0.7f, 0.14f, 0.12f), accentMaterial);
        }
    }

    private static void CreateSkyGlowBands(BootstrapAssets assets)
    {
        CreateGroundStrip("SkyGlowCore", new Vector3(0f, 17f, 188f), new Vector3(58f, 9f, 180f), assets.AccentMaterial);
        CreateGroundStrip("SkyGlowMagenta", new Vector3(-14f, 23f, 228f), new Vector3(44f, 12f, 136f), assets.AlternateAccentMaterial);
        CreateGroundStrip("SkyGlowViolet", new Vector3(18f, 28f, 264f), new Vector3(60f, 14f, 120f), assets.TertiaryAccentMaterial);
    }

    private static void CreateForegroundFrame(BootstrapAssets assets)
    {
        Material shell = CreateMaterial("ForegroundShell", new Color(0.03f, 0.04f, 0.07f), Color.black);
        Material signMaterial = CreateMaterial("ForegroundSign", new Color(0.04f, 0.12f, 0.16f), assets.TertiaryAccentMaterial.color);
        CreateGroundStrip("FrameLeftNear", new Vector3(-9.2f, 4.6f, 18f), new Vector3(1.6f, 9.2f, 3.6f), shell);
        CreateGroundStrip("FrameRightNear", new Vector3(9.2f, 4.6f, 26f), new Vector3(1.9f, 9.2f, 4.2f), shell);
        CreateGroundStrip("FrameLeftAccent", new Vector3(-8.42f, 6.1f, 18.4f), new Vector3(0.12f, 6.2f, 2.4f), assets.AccentMaterial);
        CreateGroundStrip("FrameRightAccent", new Vector3(8.26f, 6.3f, 26.6f), new Vector3(0.12f, 6.6f, 2.6f), assets.AlternateAccentMaterial);
        CreateGroundStrip("FrameBridge", new Vector3(0f, 8.6f, 34f), new Vector3(13.8f, 0.26f, 1.4f), shell);
        CreateGroundStrip("FrameBridgeGlow", new Vector3(0f, 8.78f, 34f), new Vector3(10.4f, 0.06f, 0.12f), assets.TertiaryAccentMaterial);
        CreateGroundStrip("FrameLeftBlade", new Vector3(-7.1f, 3.4f, 14.8f), new Vector3(0.9f, 6.8f, 1.1f), shell);
        CreateGroundStrip("FrameRightBlade", new Vector3(7.5f, 3.7f, 30.4f), new Vector3(1.1f, 7.4f, 1.3f), shell);
        CreateGroundStrip("FrameLeftBladeGlow", new Vector3(-6.7f, 5.8f, 14.8f), new Vector3(0.1f, 4.2f, 0.7f), assets.WarningMaterial);
        CreateGroundStrip("FrameRightBladeGlow", new Vector3(7.02f, 6.1f, 30.4f), new Vector3(0.1f, 4.6f, 0.7f), assets.AccentMaterial);
        CreateGroundStrip("FrameSuspendedSign", new Vector3(0f, 5.6f, 22f), new Vector3(6.4f, 1.6f, 0.14f), signMaterial);
        CreateGroundStrip("FrameSuspendedGlow", new Vector3(0f, 5.58f, 22.08f), new Vector3(4.8f, 0.08f, 0.04f), assets.TertiaryAccentMaterial);
        CreateGroundStrip("FrameCableLeft", new Vector3(-3.05f, 7.2f, 21.6f), new Vector3(0.06f, 3.3f, 0.06f), shell);
        CreateGroundStrip("FrameCableRight", new Vector3(3.05f, 7.2f, 22.4f), new Vector3(0.06f, 3.3f, 0.06f), shell);
        CreateGroundStrip("FrameLeftBaseApron", new Vector3(-8.06f, 1.02f, 18.2f), new Vector3(2.6f, 0.2f, 4.2f), shell);
        CreateGroundStrip("FrameRightBaseApron", new Vector3(8.34f, 1.06f, 26.4f), new Vector3(3f, 0.2f, 5f), shell);
        CreateGroundStrip("FrameCenterScanner", new Vector3(0f, 7.1f, 28.6f), new Vector3(8.4f, 0.08f, 0.12f), assets.WarningMaterial);
    }

    private static void CreateNearCameraDistrictSetDressing(BootstrapAssets assets)
    {
        Material shell = CreateMaterial("ForegroundShell", new Color(0.03f, 0.04f, 0.07f), Color.black);
        Material holoSign = CreateMaterial("ForegroundNearHolo", new Color(0.05f, 0.11f, 0.16f), assets.AccentMaterial.color);
        Material threatSign = CreateMaterial("ForegroundNearThreat", new Color(0.16f, 0.05f, 0.08f), assets.WarningMaterial.color);

        CreateGroundStrip("NearTowerLeftA", new Vector3(-11.8f, 6.4f, 20f), new Vector3(2.8f, 12.8f, 3.4f), shell);
        CreateGroundStrip("NearTowerRightA", new Vector3(11.4f, 6f, 34f), new Vector3(2.4f, 12f, 3f), shell);
        CreateGroundStrip("NearTowerLeftGlowA", new Vector3(-10.52f, 6.8f, 20.4f), new Vector3(0.14f, 9.8f, 2.1f), assets.AccentMaterial);
        CreateGroundStrip("NearTowerRightGlowA", new Vector3(10.24f, 6.4f, 33.8f), new Vector3(0.14f, 8.8f, 2f), assets.AlternateAccentMaterial);

        CreateGroundStrip("NearBridgeDeckA", new Vector3(0f, 8.8f, 44f), new Vector3(16.8f, 0.3f, 1.9f), shell);
        CreateGroundStrip("NearBridgeGlowA", new Vector3(0f, 9.02f, 44f), new Vector3(13.6f, 0.06f, 0.12f), assets.TertiaryAccentMaterial);
        CreateGroundStrip("NearBridgeBraceLeftA", new Vector3(-7.9f, 6.9f, 44f), new Vector3(0.16f, 3.8f, 1.2f), assets.AccentMaterial);
        CreateGroundStrip("NearBridgeBraceRightA", new Vector3(7.9f, 6.9f, 44f), new Vector3(0.16f, 3.8f, 1.2f), assets.WarningMaterial);

        CreateGroundStrip("NearSignLeft", new Vector3(-8.7f, 5.5f, 30f), new Vector3(4.2f, 1.6f, 0.14f), holoSign);
        CreateGroundStrip("NearSignLeftGlow", new Vector3(-8.7f, 5.48f, 30.08f), new Vector3(3.2f, 0.08f, 0.04f), assets.AccentMaterial);
        CreateGroundStrip("NearSignRight", new Vector3(8.9f, 5.8f, 54f), new Vector3(4.6f, 1.7f, 0.14f), threatSign);
        CreateGroundStrip("NearSignRightGlow", new Vector3(8.9f, 5.78f, 54.08f), new Vector3(3.5f, 0.08f, 0.04f), assets.WarningMaterial);

        CreateStreetGate(28f, assets.AccentMaterial, shell);
        CreateStreetGate(64f, assets.AlternateAccentMaterial, shell);
        CreateStreetGate(108f, assets.WarningMaterial, shell);
        CreateSkyBridge(74f, assets.TertiaryAccentMaterial);
        CreateDataSpire(24f, -13.8f, assets.AccentMaterial);
        CreateDataSpire(52f, 13.6f, assets.WarningMaterial);
        CreateDataSpire(92f, -13.4f, assets.AlternateAccentMaterial);
        CreateSecurityArray(118f, assets.WarningMaterial, assets.TertiaryAccentMaterial);
        CreateGroundStrip("NearTerraceLeft", new Vector3(-13.9f, 2.2f, 42f), new Vector3(2.6f, 4.4f, 28f), shell);
        CreateGroundStrip("NearTerraceRight", new Vector3(13.7f, 2.4f, 58f), new Vector3(2.8f, 4.8f, 30f), shell);
        CreateGroundStrip("NearTerraceLeftGlow", new Vector3(-12.58f, 3.2f, 42.2f), new Vector3(0.14f, 5.2f, 22f), assets.AccentMaterial);
        CreateGroundStrip("NearTerraceRightGlow", new Vector3(12.34f, 3.4f, 58.2f), new Vector3(0.14f, 5.8f, 24f), assets.AlternateAccentMaterial);
        CreateGroundStrip("NearThreatBanner", new Vector3(0f, 6.3f, 58f), new Vector3(8.2f, 1.8f, 0.14f), threatSign);
        CreateGroundStrip("NearThreatBannerGlow", new Vector3(0f, 6.24f, 58.08f), new Vector3(5.8f, 0.08f, 0.04f), assets.WarningMaterial);
    }

    private static void CreateLaunchSetPiece(BootstrapAssets assets)
    {
        Material gateShell = CreateMaterial("LaunchGateShell", new Color(0.05f, 0.06f, 0.1f), Color.black);
        CreateStreetGate(22f, assets.AccentMaterial, gateShell);
        CreateStreetGate(56f, assets.TertiaryAccentMaterial, gateShell);
        CreateStreetGate(96f, assets.AlternateAccentMaterial, gateShell);
        CreateSkyBridge(42f, assets.AlternateAccentMaterial);
        CreateSkyBridge(86f, assets.TertiaryAccentMaterial);
        CreateBillboard(18f, -9.2f, assets.AccentMaterial);
        CreateBillboard(38f, 9.6f, assets.WarningMaterial);
        CreateBillboard(74f, -10f, assets.AlternateAccentMaterial);
        CreateSideStructure(26f, -8.4f, assets.AccentMaterial, false);
        CreateSideStructure(48f, 8.2f, assets.AlternateAccentMaterial, true);
        CreateSideStructure(88f, -8.8f, assets.TertiaryAccentMaterial, false);
        CreateDataSpire(34f, 13.1f, assets.WarningMaterial);
        CreateDataSpire(68f, -13.2f, assets.AccentMaterial);
        CreateNeonTunnelSegment(112f, assets.AccentMaterial, assets.TertiaryAccentMaterial);
    }

    private static void CreateDataSpire(float zPosition, float xPosition, Material accentMaterial)
    {
        Material shell = CreateMaterial("DataSpireMetal", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0f, 0f));
        CreateGroundStrip($"DataSpireBase_{zPosition:0}_{xPosition:0}", new Vector3(xPosition, 3.8f, zPosition), new Vector3(1.1f, 7.6f, 1.1f), shell);
        CreateGroundStrip($"DataSpireGlow_{zPosition:0}_{xPosition:0}", new Vector3(xPosition, 6.95f, zPosition), new Vector3(1.45f, 0.08f, 1.45f), accentMaterial);
        CreateGroundStrip($"DataSpireStrip_{zPosition:0}_{xPosition:0}", new Vector3(xPosition, 4f, zPosition + 0.58f), new Vector3(0.12f, 5.4f, 0.08f), accentMaterial);
    }

    private static void CreateStreetGate(float zPosition, Material accentMaterial, Material frameMaterial)
    {
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = $"GateLeft_{zPosition:0}";
        left.transform.position = new Vector3(-5.2f, 3.2f, zPosition);
        left.transform.localScale = new Vector3(0.42f, 6.4f, 0.42f);
        left.GetComponent<Renderer>().sharedMaterial = frameMaterial;
        Object.DestroyImmediate(left.GetComponent<BoxCollider>());

        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.name = $"GateRight_{zPosition:0}";
        right.transform.position = new Vector3(5.2f, 3.2f, zPosition);
        right.transform.localScale = new Vector3(0.42f, 6.4f, 0.42f);
        right.GetComponent<Renderer>().sharedMaterial = frameMaterial;
        Object.DestroyImmediate(right.GetComponent<BoxCollider>());

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = $"GateTop_{zPosition:0}";
        top.transform.position = new Vector3(0f, 5.9f, zPosition);
        top.transform.localScale = new Vector3(10.9f, 0.28f, 0.45f);
        top.GetComponent<Renderer>().sharedMaterial = accentMaterial;
        Object.DestroyImmediate(top.GetComponent<BoxCollider>());
        top.AddComponent<NeonPropAnimator>().Configure(Color.cyan, 1.8f, 0.75f, 1.3f);
        CreateGroundStrip($"GateCore_{zPosition:0}", new Vector3(0f, 4.46f, zPosition), new Vector3(6.2f, 0.9f, 0.18f), frameMaterial);
        CreateGroundStrip($"GateCoreGlow_{zPosition:0}", new Vector3(0f, 4.44f, zPosition + 0.12f), new Vector3(4.4f, 0.08f, 0.04f), accentMaterial);
        CreateGroundStrip($"GateShoulderLeft_{zPosition:0}", new Vector3(-4.52f, 2.1f, zPosition), new Vector3(0.12f, 4.1f, 0.2f), accentMaterial);
        CreateGroundStrip($"GateShoulderRight_{zPosition:0}", new Vector3(4.52f, 2.1f, zPosition), new Vector3(0.12f, 4.1f, 0.2f), accentMaterial);
    }

    private static void CreateSkylineCluster(float zPosition, float xBase, Material accentMaterial)
    {
        Material towerMaterial = CreateMaterial("TowerDark", new Color(0.04f, 0.04f, 0.08f), new Color(0f, 0f, 0f));
        for (int i = 0; i < 4; i++)
        {
            float seed = Mathf.Abs(Mathf.Sin((zPosition * 0.013f) + (i * 0.73f)));
            float width = 1.8f + (i * 0.55f) + (seed * 0.85f);
            float height = 11f + (i * 4.6f) + (seed * 8.5f);
            float x = xBase + (i * (2.35f + seed * 0.9f) * Mathf.Sign(xBase));
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = $"Tower_{zPosition:0}_{i}";
            tower.transform.position = new Vector3(x, height * 0.5f, zPosition + (i * 12f));
            tower.transform.localScale = new Vector3(width, height, width);
            tower.GetComponent<Renderer>().sharedMaterial = towerMaterial;
            Object.DestroyImmediate(tower.GetComponent<BoxCollider>());

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = $"TowerTrim_{zPosition:0}_{i}";
            trim.transform.position = tower.transform.position + new Vector3(0f, 0f, width * 0.5f + 0.02f);
            trim.transform.localScale = new Vector3(width + 0.08f, height, 0.08f);
            trim.GetComponent<Renderer>().sharedMaterial = accentMaterial;
            Object.DestroyImmediate(trim.GetComponent<BoxCollider>());

            CreateGroundStrip($"TowerBeacon_{zPosition:0}_{i}", new Vector3(x, height + 0.22f, zPosition + (i * 12f)), new Vector3(width * 0.4f, 0.08f, width * 0.4f), accentMaterial);
            if (i % 2 == 0)
            {
                CreateGroundStrip($"TowerBridgeCap_{zPosition:0}_{i}", new Vector3(x, height * 0.68f, zPosition + (i * 12f) + (width * 0.34f)), new Vector3(width * 0.64f, 0.12f, 0.12f), accentMaterial);
            }

            CreateGroundStrip($"TowerSideFin_{zPosition:0}_{i}", new Vector3(x + (Mathf.Sign(xBase) * (width * 0.46f)), height * 0.56f, zPosition + (i * 12f) - (width * 0.12f)), new Vector3(0.14f, height * 0.62f, 0.24f), accentMaterial);
            CreateGroundStrip($"TowerWindowBand_{zPosition:0}_{i}", new Vector3(x, height * 0.82f, zPosition + (i * 12f) + (width * 0.18f)), new Vector3(width * 0.58f, 0.1f, 0.12f), accentMaterial);
        }
    }

    private static void CreateBillboard(float zPosition, float xPosition, Material accentMaterial)
    {
        Material postMaterial = CreateMaterial("BillboardPost", new Color(0.08f, 0.08f, 0.14f), new Color(0f, 0f, 0f));
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = $"BillboardPost_{zPosition:0}";
        post.transform.position = new Vector3(xPosition, 2.8f, zPosition);
        post.transform.localScale = new Vector3(0.3f, 5.6f, 0.3f);
        post.GetComponent<Renderer>().sharedMaterial = postMaterial;
        Object.DestroyImmediate(post.GetComponent<BoxCollider>());

        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = $"Billboard_{zPosition:0}";
        board.transform.position = new Vector3(xPosition, 5.4f, zPosition);
        board.transform.localScale = new Vector3(3.8f, 2.1f, 0.18f);
        board.GetComponent<Renderer>().sharedMaterial = CreateMaterial("HoloBillboard", new Color(0.06f, 0.12f, 0.16f), new Color(0.15f, 0.95f, 1f));
        Object.DestroyImmediate(board.GetComponent<BoxCollider>());
        board.AddComponent<NeonPropAnimator>().Configure(new Color(1f, 0.25f, 0.85f), 1.8f, 0.75f, 1.35f);

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = $"BillboardFrame_{zPosition:0}";
        frame.transform.position = board.transform.position + new Vector3(0f, 0f, 0.11f);
        frame.transform.localScale = new Vector3(4.05f, 2.28f, 0.05f);
        frame.GetComponent<Renderer>().sharedMaterial = accentMaterial;
        Object.DestroyImmediate(frame.GetComponent<BoxCollider>());

        CreateGroundStrip($"BillboardAccent_{zPosition:0}", board.transform.position + new Vector3(0f, -0.78f, 0.12f), new Vector3(2.6f, 0.06f, 0.03f), accentMaterial);
        CreateGroundStrip($"BillboardHeader_{zPosition:0}", board.transform.position + new Vector3(0f, 0.78f, 0.09f), new Vector3(2.8f, 0.08f, 0.03f), accentMaterial);
        CreateGroundStrip($"BillboardBlade_{zPosition:0}", new Vector3(xPosition + Mathf.Sign(xPosition) * 1.8f, 4.22f, zPosition - 0.1f), new Vector3(0.12f, 3f, 0.12f), accentMaterial);
    }

    private static void CreateTrafficStream(float xPosition, Material material)
    {
        for (int i = 0; i < 6; i++)
        {
            GameObject traffic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            traffic.name = $"Traffic_{xPosition:0}_{i}";
            traffic.transform.position = new Vector3(xPosition, 0.6f, 80f + (i * 42f));
            traffic.transform.localScale = new Vector3(1.6f, 0.75f, 3.6f);
            traffic.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(traffic.GetComponent<BoxCollider>());

            CreateVisualPrimitive("Cabin", PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.1f), new Vector3(0.9f, 0.42f, 1.4f), CreateMaterial("TrafficGlass", new Color(0.08f, 0.22f, 0.28f), new Color(0.18f, 0.9f, 1f)), traffic.transform);
            CreateVisualPrimitive("SkidGlow", PrimitiveType.Cube, new Vector3(0f, -0.18f, -1.54f), new Vector3(0.85f, 0.03f, 0.12f), material, traffic.transform);
            traffic.AddComponent<LoopingTrafficProp>().Configure(Random.Range(5.5f, 8.5f), 10f, 250f, xPosition);
        }
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static RectTransform CreateContentRoot(Transform parent, Vector2 size, float yOffset)
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(parent, false);
        RectTransform rect = contentObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, yOffset);
        rect.sizeDelta = size;
        return rect;
    }

    private static Button CreateButton(Transform parent, Font font, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject buttonObject = new GameObject($"{label}Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        bool destructive = label.Contains("Quit") || label.Contains("Skip");
        bool premium = label.Contains("BUY") || label.Contains("2x") || label.Contains("Claim");
        Color accentColor = destructive
            ? ResolveThemeColor(theme => theme.DestructiveAccent, new Color(1f, 0.32f, 0.36f, 0.88f))
            : premium
                ? ResolveThemeColor(theme => theme.CommerceAccent, new Color(1f, 0.78f, 0.2f, 0.92f))
                : ResolveThemeColor(theme => theme.PanelAccent, new Color(0.18f, 0.95f, 1f, 0.88f));

        Image image = buttonObject.GetComponent<Image>();
        image.color = ResolveThemeColor(theme => theme.PanelFill, new Color(0.01f, 0.022f, 0.05f, 0.9f));
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.62f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(6f, -6f);

        GameObject backPlate = new GameObject("BackPlate", typeof(RectTransform), typeof(Image));
        backPlate.transform.SetParent(buttonObject.transform, false);
        RectTransform backPlateRect = backPlate.GetComponent<RectTransform>();
        backPlateRect.anchorMin = Vector2.zero;
        backPlateRect.anchorMax = Vector2.one;
        backPlateRect.offsetMin = new Vector2(4f, 4f);
        backPlateRect.offsetMax = new Vector2(-4f, -4f);
        Image backPlateImage = backPlate.GetComponent<Image>();
        backPlateImage.color = new Color(image.color.r * 0.9f, image.color.g * 0.9f, image.color.b * 0.95f, 0.88f);
        backPlateImage.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(buttonObject.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(10f, 10f);
        fillRect.offsetMax = new Vector2(-10f, -10f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(accentColor.r * 0.18f, accentColor.g * 0.2f, accentColor.b * 0.24f, 0.92f);
        fillImage.raycastTarget = false;

        GameObject sheen = new GameObject("Sheen", typeof(RectTransform), typeof(Image));
        sheen.transform.SetParent(buttonObject.transform, false);
        RectTransform sheenRect = sheen.GetComponent<RectTransform>();
        sheenRect.anchorMin = new Vector2(0f, 1f);
        sheenRect.anchorMax = new Vector2(1f, 1f);
        sheenRect.offsetMin = new Vector2(16f, -18f);
        sheenRect.offsetMax = new Vector2(-56f, -6f);
        Image sheenImage = sheen.GetComponent<Image>();
        sheenImage.color = new Color(1f, 1f, 1f, 0.08f);
        sheenImage.raycastTarget = false;

        GameObject leftBar = new GameObject("LeftBar", typeof(RectTransform), typeof(Image));
        leftBar.transform.SetParent(buttonObject.transform, false);
        RectTransform leftBarRect = leftBar.GetComponent<RectTransform>();
        leftBarRect.anchorMin = new Vector2(0f, 0f);
        leftBarRect.anchorMax = new Vector2(0f, 1f);
        leftBarRect.offsetMin = new Vector2(8f, 10f);
        leftBarRect.offsetMax = new Vector2(24f, -10f);
        Image leftBarImage = leftBar.GetComponent<Image>();
        leftBarImage.color = accentColor;
        leftBarImage.raycastTarget = false;

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(buttonObject.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = new Vector2(18f, -8f);
        accentRect.offsetMax = new Vector2(-18f, -2f);
        Image accentImage = accent.GetComponent<Image>();
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;

        GameObject bottomGlow = new GameObject("BottomGlow", typeof(RectTransform), typeof(Image));
        bottomGlow.transform.SetParent(buttonObject.transform, false);
        RectTransform bottomGlowRect = bottomGlow.GetComponent<RectTransform>();
        bottomGlowRect.anchorMin = new Vector2(0f, 0f);
        bottomGlowRect.anchorMax = new Vector2(1f, 0f);
        bottomGlowRect.offsetMin = new Vector2(30f, 4f);
        bottomGlowRect.offsetMax = new Vector2(-20f, 12f);
        Image bottomGlowImage = bottomGlow.GetComponent<Image>();
        bottomGlowImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f);
        bottomGlowImage.raycastTarget = false;

        GameObject rightCap = new GameObject("RightCap", typeof(RectTransform), typeof(Image));
        rightCap.transform.SetParent(buttonObject.transform, false);
        RectTransform rightCapRect = rightCap.GetComponent<RectTransform>();
        rightCapRect.anchorMin = new Vector2(1f, 0f);
        rightCapRect.anchorMax = new Vector2(1f, 1f);
        rightCapRect.offsetMin = new Vector2(-32f, 10f);
        rightCapRect.offsetMax = new Vector2(-10f, -10f);
        Image rightCapImage = rightCap.GetComponent<Image>();
        rightCapImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f);
        rightCapImage.raycastTarget = false;

        GameObject rightChevron = new GameObject("RightChevron", typeof(RectTransform), typeof(Image));
        rightChevron.transform.SetParent(buttonObject.transform, false);
        RectTransform chevronRect = rightChevron.GetComponent<RectTransform>();
        chevronRect.anchorMin = new Vector2(1f, 0.5f);
        chevronRect.anchorMax = new Vector2(1f, 0.5f);
        chevronRect.anchoredPosition = new Vector2(-24f, 0f);
        chevronRect.sizeDelta = new Vector2(14f, size.y * 0.54f);
        Image rightChevronImage = rightChevron.GetComponent<Image>();
        rightChevronImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.54f);
        rightChevronImage.raycastTarget = false;

        CreateText(buttonObject.transform, font, label.ToUpperInvariant(), new Vector2(14f, 0f), size, size.y >= 80f ? 28 : 24, TextAnchor.MiddleCenter, ResolveThemeColor(theme => theme.TitleText, Color.white));
        return buttonObject.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, Font font, string content, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject($"{(string.IsNullOrEmpty(content) ? "Label" : content)}Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        if (fontSize >= 30)
        {
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r * 0.14f, color.g * 0.22f, color.b * 0.24f, 0.48f);
            outline.effectDistance = new Vector2(1f, -1f);
        }
        return text;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        bool hudPanel = name.Contains("Hud") || name.Contains("TopBanner") || name.Contains("PowerTray") || name.Contains("Fps");
        bool overlayPanel = name.Contains("Pause") || name.Contains("Tutorial") || name.Contains("Revive") || name.Contains("Settings");
        bool commercePanel = name.Contains("Shop") || name.Contains("Upgrade") || name.Contains("Leaderboard") || name.Contains("Starter");
        Color accentColor = commercePanel
            ? ResolveThemeColor(theme => theme.CommerceAccent, new Color(1f, 0.82f, 0.24f, 0.82f))
            : overlayPanel
                ? ResolveThemeColor(theme => theme.DestructiveAccent, new Color(1f, 0.34f, 0.74f, 0.72f))
                : ResolveThemeColor(theme => theme.PanelAccent, new Color(0.16f, 0.95f, 1f, 0.72f));

        Image image = panel.GetComponent<Image>();
        image.color = hudPanel
            ? ResolveThemeColor(theme => theme.HudPanelFill, new Color(0.01f, 0.022f, 0.048f, 0.54f))
            : overlayPanel
                ? ResolveThemeColor(theme => theme.OverlayPanelFill, new Color(0.015f, 0.02f, 0.05f, 0.88f))
                : ResolveThemeColor(theme => theme.PanelFill, new Color(0.015f, 0.024f, 0.05f, 0.76f));
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, hudPanel ? 0.34f : 0.52f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(8f, -8f);

        GameObject dropPlate = new GameObject("DropPlate", typeof(RectTransform), typeof(Image));
        dropPlate.transform.SetParent(panel.transform, false);
        RectTransform dropPlateRect = dropPlate.GetComponent<RectTransform>();
        dropPlateRect.anchorMin = Vector2.zero;
        dropPlateRect.anchorMax = Vector2.one;
        dropPlateRect.offsetMin = new Vector2(6f, 6f);
        dropPlateRect.offsetMax = new Vector2(-6f, -6f);
        Image dropPlateImage = dropPlate.GetComponent<Image>();
        dropPlateImage.color = new Color(0f, 0f, 0f, 0.2f);
        dropPlateImage.raycastTarget = false;

        GameObject innerFill = new GameObject("InnerFill", typeof(RectTransform), typeof(Image));
        innerFill.transform.SetParent(panel.transform, false);
        RectTransform innerRect = innerFill.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(12f, 12f);
        innerRect.offsetMax = new Vector2(-12f, -12f);
        Image innerFillImage = innerFill.GetComponent<Image>();
        innerFillImage.color = hudPanel
            ? new Color(0.03f, 0.08f, 0.14f, 0.64f)
            : new Color(0.03f, 0.07f, 0.12f, 0.9f);
        innerFillImage.raycastTarget = false;

        GameObject glassFill = new GameObject("GlassFill", typeof(RectTransform), typeof(Image));
        glassFill.transform.SetParent(panel.transform, false);
        RectTransform glassRect = glassFill.GetComponent<RectTransform>();
        glassRect.anchorMin = Vector2.zero;
        glassRect.anchorMax = Vector2.one;
        glassRect.offsetMin = new Vector2(18f, 18f);
        glassRect.offsetMax = new Vector2(-18f, -18f);
        Image glassFillImage = glassFill.GetComponent<Image>();
        glassFillImage.color = new Color(0.18f, 0.42f, 0.56f, hudPanel ? 0.04f : 0.08f);
        glassFillImage.raycastTarget = false;

        GameObject topLine = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
        topLine.transform.SetParent(panel.transform, false);
        RectTransform topRect = topLine.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.offsetMin = new Vector2(18f, -10f);
        topRect.offsetMax = new Vector2(-18f, -2f);
        Image topLineImage = topLine.GetComponent<Image>();
        topLineImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, hudPanel ? 0.48f : 0.76f);
        topLineImage.raycastTarget = false;

        GameObject topBand = new GameObject("TopBand", typeof(RectTransform), typeof(Image));
        topBand.transform.SetParent(panel.transform, false);
        RectTransform topBandRect = topBand.GetComponent<RectTransform>();
        topBandRect.anchorMin = new Vector2(0f, 1f);
        topBandRect.anchorMax = new Vector2(1f, 1f);
        topBandRect.offsetMin = new Vector2(20f, -34f);
        topBandRect.offsetMax = new Vector2(-64f, -12f);
        Image topBandImage = topBand.GetComponent<Image>();
        topBandImage.color = commercePanel
            ? new Color(0.26f, 0.18f, 0.06f, 0.3f)
            : new Color(0.14f, 0.24f, 0.38f, hudPanel ? 0.18f : 0.34f);
        topBandImage.raycastTarget = false;

        GameObject bottomLine = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
        bottomLine.transform.SetParent(panel.transform, false);
        RectTransform bottomRect = bottomLine.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.offsetMin = new Vector2(24f, 4f);
        bottomRect.offsetMax = new Vector2(-24f, 12f);
        Image bottomLineImage = bottomLine.GetComponent<Image>();
        bottomLineImage.color = overlayPanel
            ? new Color(1f, 0.28f, 0.76f, 0.34f)
            : new Color(accentColor.r, accentColor.g, accentColor.b, hudPanel ? 0.18f : 0.28f);
        bottomLineImage.raycastTarget = false;

        GameObject sideBand = new GameObject("SideBand", typeof(RectTransform), typeof(Image));
        sideBand.transform.SetParent(panel.transform, false);
        RectTransform sideBandRect = sideBand.GetComponent<RectTransform>();
        sideBandRect.anchorMin = new Vector2(0f, 0f);
        sideBandRect.anchorMax = new Vector2(0f, 1f);
        sideBandRect.offsetMin = new Vector2(8f, 18f);
        sideBandRect.offsetMax = new Vector2(16f, -18f);
        Image sideBandImage = sideBand.GetComponent<Image>();
        sideBandImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, hudPanel ? 0.42f : 0.56f);
        sideBandImage.raycastTarget = false;

        GameObject cornerBand = new GameObject("CornerBand", typeof(RectTransform), typeof(Image));
        cornerBand.transform.SetParent(panel.transform, false);
        RectTransform cornerBandRect = cornerBand.GetComponent<RectTransform>();
        cornerBandRect.anchorMin = new Vector2(1f, 1f);
        cornerBandRect.anchorMax = new Vector2(1f, 1f);
        cornerBandRect.anchoredPosition = new Vector2(-34f, -28f);
        cornerBandRect.sizeDelta = new Vector2(84f, 14f);
        Image cornerBandImage = cornerBand.GetComponent<Image>();
        cornerBandImage.color = commercePanel
            ? new Color(1f, 0.82f, 0.24f, 0.64f)
            : new Color(1f, 0.28f, 0.76f, hudPanel ? 0.28f : 0.48f);
        cornerBandImage.raycastTarget = false;

        GameObject scanLine = new GameObject("ScanLine", typeof(RectTransform), typeof(Image));
        scanLine.transform.SetParent(panel.transform, false);
        RectTransform scanLineRect = scanLine.GetComponent<RectTransform>();
        scanLineRect.anchorMin = new Vector2(0f, 0.5f);
        scanLineRect.anchorMax = new Vector2(1f, 0.5f);
        scanLineRect.offsetMin = new Vector2(26f, -1f);
        scanLineRect.offsetMax = new Vector2(-26f, 1f);
        Image scanLineImage = scanLine.GetComponent<Image>();
        scanLineImage.color = new Color(1f, 1f, 1f, hudPanel ? 0.03f : 0.05f);
        scanLineImage.raycastTarget = false;

        GameObject notch = new GameObject("Notch", typeof(RectTransform), typeof(Image));
        notch.transform.SetParent(panel.transform, false);
        RectTransform notchRect = notch.GetComponent<RectTransform>();
        notchRect.anchorMin = new Vector2(1f, 1f);
        notchRect.anchorMax = new Vector2(1f, 1f);
        notchRect.anchoredPosition = new Vector2(-56f, -18f);
        notchRect.sizeDelta = new Vector2(48f, 10f);
        Image notchImage = notch.GetComponent<Image>();
        notchImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.24f);
        notchImage.raycastTarget = false;

        CreatePanelCorner(panel.transform, new Vector2(0f, 1f), new Vector2(18f, -18f), new Color(accentColor.r, accentColor.g, accentColor.b, hudPanel ? 0.5f : 0.74f));
        CreatePanelCorner(panel.transform, new Vector2(1f, 0f), new Vector2(-18f, 18f), new Color(1f, 0.28f, 0.76f, hudPanel ? 0.24f : 0.42f));
        return panel;
    }

    private static void CreateMenuBackdropUi(Transform parent)
    {
        GameObject glowTop = new GameObject("BackdropGlowTop", typeof(RectTransform), typeof(Image));
        glowTop.transform.SetParent(parent, false);
        RectTransform topRect = glowTop.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -180f);
        topRect.sizeDelta = new Vector2(1120f, 260f);
        Image glowTopImage = glowTop.GetComponent<Image>();
        glowTopImage.color = new Color(0.08f, 0.28f, 0.36f, 0.22f);
        glowTopImage.raycastTarget = false;

        GameObject glowBottom = new GameObject("BackdropGlowBottom", typeof(RectTransform), typeof(Image));
        glowBottom.transform.SetParent(parent, false);
        RectTransform bottomRect = glowBottom.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.5f, 0f);
        bottomRect.anchorMax = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = new Vector2(0f, 180f);
        bottomRect.sizeDelta = new Vector2(1120f, 260f);
        Image glowBottomImage = glowBottom.GetComponent<Image>();
        glowBottomImage.color = new Color(0.22f, 0.06f, 0.2f, 0.18f);
        glowBottomImage.raycastTarget = false;

        GameObject leftRail = new GameObject("BackdropLeftRail", typeof(RectTransform), typeof(Image));
        leftRail.transform.SetParent(parent, false);
        RectTransform leftRailRect = leftRail.GetComponent<RectTransform>();
        leftRailRect.anchorMin = new Vector2(0f, 0.5f);
        leftRailRect.anchorMax = new Vector2(0f, 0.5f);
        leftRailRect.anchoredPosition = new Vector2(116f, 0f);
        leftRailRect.sizeDelta = new Vector2(220f, 920f);
        Image leftRailImage = leftRail.GetComponent<Image>();
        leftRailImage.color = new Color(0.04f, 0.18f, 0.24f, 0.14f);
        leftRailImage.raycastTarget = false;

        GameObject rightRail = new GameObject("BackdropRightRail", typeof(RectTransform), typeof(Image));
        rightRail.transform.SetParent(parent, false);
        RectTransform rightRailRect = rightRail.GetComponent<RectTransform>();
        rightRailRect.anchorMin = new Vector2(1f, 0.5f);
        rightRailRect.anchorMax = new Vector2(1f, 0.5f);
        rightRailRect.anchoredPosition = new Vector2(-116f, 0f);
        rightRailRect.sizeDelta = new Vector2(220f, 920f);
        Image rightRailImage = rightRail.GetComponent<Image>();
        rightRailImage.color = new Color(0.16f, 0.04f, 0.18f, 0.12f);
        rightRailImage.raycastTarget = false;

        GameObject horizonBand = new GameObject("BackdropHorizonBand", typeof(RectTransform), typeof(Image));
        horizonBand.transform.SetParent(parent, false);
        RectTransform horizonRect = horizonBand.GetComponent<RectTransform>();
        horizonRect.anchorMin = new Vector2(0.5f, 0f);
        horizonRect.anchorMax = new Vector2(0.5f, 0f);
        horizonRect.anchoredPosition = new Vector2(0f, 240f);
        horizonRect.sizeDelta = new Vector2(1180f, 180f);
        Image horizonBandImage = horizonBand.GetComponent<Image>();
        horizonBandImage.color = new Color(0.3f, 0.05f, 0.24f, 0.12f);
        horizonBandImage.raycastTarget = false;

        GameObject centerGlow = new GameObject("CenterGlow", typeof(RectTransform), typeof(Image));
        centerGlow.transform.SetParent(parent, false);
        RectTransform centerGlowRect = centerGlow.GetComponent<RectTransform>();
        centerGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerGlowRect.anchoredPosition = new Vector2(100f, 0f);
        centerGlowRect.sizeDelta = new Vector2(860f, 1120f);
        Image centerGlowImage = centerGlow.GetComponent<Image>();
        centerGlowImage.color = new Color(0.12f, 0.3f, 0.42f, 0.07f);
        centerGlowImage.raycastTarget = false;

        GameObject skylineMatte = new GameObject("SkylineMatte", typeof(RectTransform), typeof(Image));
        skylineMatte.transform.SetParent(parent, false);
        RectTransform skylineMatteRect = skylineMatte.GetComponent<RectTransform>();
        skylineMatteRect.anchorMin = new Vector2(0.5f, 0f);
        skylineMatteRect.anchorMax = new Vector2(0.5f, 0f);
        skylineMatteRect.anchoredPosition = new Vector2(84f, 262f);
        skylineMatteRect.sizeDelta = new Vector2(720f, 220f);
        Image skylineMatteImage = skylineMatte.GetComponent<Image>();
        skylineMatteImage.color = new Color(0.02f, 0.08f, 0.12f, 0.18f);
        skylineMatteImage.raycastTarget = false;

        GameObject diagonalSweep = new GameObject("DiagonalSweep", typeof(RectTransform), typeof(Image));
        diagonalSweep.transform.SetParent(parent, false);
        RectTransform diagonalRect = diagonalSweep.GetComponent<RectTransform>();
        diagonalRect.anchorMin = new Vector2(0.5f, 0.5f);
        diagonalRect.anchorMax = new Vector2(0.5f, 0.5f);
        diagonalRect.anchoredPosition = new Vector2(220f, 80f);
        diagonalRect.sizeDelta = new Vector2(820f, 120f);
        diagonalRect.localRotation = Quaternion.Euler(0f, 0f, -14f);
        Image diagonalImage = diagonalSweep.GetComponent<Image>();
        diagonalImage.color = new Color(0.18f, 0.95f, 1f, 0.045f);
        diagonalImage.raycastTarget = false;

        GameObject lowerSweep = new GameObject("LowerSweep", typeof(RectTransform), typeof(Image));
        lowerSweep.transform.SetParent(parent, false);
        RectTransform lowerSweepRect = lowerSweep.GetComponent<RectTransform>();
        lowerSweepRect.anchorMin = new Vector2(0.5f, 0.5f);
        lowerSweepRect.anchorMax = new Vector2(0.5f, 0.5f);
        lowerSweepRect.anchoredPosition = new Vector2(-180f, -260f);
        lowerSweepRect.sizeDelta = new Vector2(980f, 132f);
        lowerSweepRect.localRotation = Quaternion.Euler(0f, 0f, 9f);
        Image lowerSweepImage = lowerSweep.GetComponent<Image>();
        lowerSweepImage.color = new Color(1f, 0.28f, 0.76f, 0.04f);
        lowerSweepImage.raycastTarget = false;
    }

    private static void CreatePanelCorner(Transform parent, Vector2 anchor, Vector2 anchoredPosition, Color color)
    {
        GameObject corner = new GameObject("Corner", typeof(RectTransform), typeof(Image));
        corner.transform.SetParent(parent, false);
        RectTransform rect = corner.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(18f, 18f);
        Image cornerImage = corner.GetComponent<Image>();
        cornerImage.color = color;
        cornerImage.raycastTarget = false;
    }

    private static void CreateMenuShowcase(BootstrapAssets assets)
    {
        GameObject root = new GameObject("MenuShowcase");
        Material floor = CreateMaterial("MenuShowcaseFloor", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0.96f, 1f) * 0.12f);
        Material trim = CreateMaterial("MenuShowcaseTrim", new Color(0.08f, 0.12f, 0.18f), new Color(0.18f, 0.95f, 1f));
        Material threat = CreateMaterial("MenuShowcaseThreat", new Color(0.18f, 0.04f, 0.14f), new Color(1f, 0.2f, 0.72f));

        CreateShowcaseRunway(root.transform, new Vector3(1.8f, -0.02f, 10.2f), new Vector3(8.8f, 0.06f, 10.4f), floor, trim, threat);
        CreateShowcaseRunway(root.transform, new Vector3(5.4f, -0.04f, 14.8f), new Vector3(4.8f, 0.04f, 9.8f), floor, threat, trim);
        CreateShowcaseTower(root.transform, new Vector3(-3.6f, 4.4f, 22f), new Vector3(3.1f, 8.8f, 3.1f), floor, trim);
        CreateShowcaseTower(root.transform, new Vector3(6.8f, 5.6f, 24f), new Vector3(4.2f, 10.8f, 3.8f), floor, threat);
        CreateShowcaseTower(root.transform, new Vector3(10.2f, 6.4f, 30f), new Vector3(4.8f, 12.6f, 4.4f), floor, trim);
        CreateShowcaseTower(root.transform, new Vector3(-6.8f, 3.6f, 18f), new Vector3(2.6f, 7.2f, 2.6f), floor, threat);
        CreateShowcaseTower(root.transform, new Vector3(-10.2f, 7.2f, 26f), new Vector3(4.2f, 13.8f, 3.2f), floor, trim);
        CreateShowcaseTower(root.transform, new Vector3(13.2f, 8.4f, 34f), new Vector3(5.4f, 16.2f, 4.8f), floor, threat);
        CreateShowcasePedestal(root.transform, new Vector3(0.4f, 0.18f, 8.4f), new Vector3(2.8f, 0.36f, 2.8f), floor, trim);
        CreateShowcasePedestal(root.transform, new Vector3(4.6f, 0.18f, 10.6f), new Vector3(2.2f, 0.32f, 2.2f), floor, threat);
        CreateShowcasePedestal(root.transform, new Vector3(7.8f, 0.16f, 13.2f), new Vector3(1.8f, 0.24f, 1.8f), floor, trim);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(assets.Player);
        PrepareShowcaseObject(player);
        player.transform.position = new Vector3(0.4f, 0.42f, 8.34f);
        player.transform.rotation = Quaternion.Euler(0f, 202f, 0f);
        player.transform.localScale = Vector3.one * 1.14f;
        player.name = "MenuCourier";

        GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(assets.Drone);
        PrepareShowcaseObject(drone);
        drone.transform.position = new Vector3(2.9f, 2.16f, 9.8f);
        drone.transform.rotation = Quaternion.Euler(4f, 222f, 0f);
        drone.transform.localScale *= 1.18f;
        drone.name = "MenuDrone";

        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(assets.Boss);
        PrepareShowcaseObject(boss);
        boss.transform.position = new Vector3(7.2f, 3.5f, 15.2f);
        boss.transform.rotation = Quaternion.Euler(0f, 212f, 0f);
        boss.transform.localScale *= 0.76f;
        boss.name = "MenuBossBackdrop";

        CreateShowcaseBeacon(root.transform, new Vector3(-1.6f, 2.2f, 11.4f), trim, 2.2f);
        CreateShowcaseBeacon(root.transform, new Vector3(5.8f, 2.8f, 12.6f), threat, 2.8f);
        CreateShowcaseBeacon(root.transform, new Vector3(8.8f, 3.4f, 16.4f), trim, 3.4f);
        CreateShowcaseBeacon(root.transform, new Vector3(-4.6f, 2.6f, 13.8f), threat, 2.6f);
        CreateHoloPanel(root.transform, new Vector3(6.2f, 5.1f, 16f), new Vector3(4.2f, 1.4f, 0.1f), trim);
        CreateHoloPanel(root.transform, new Vector3(2.2f, 3.2f, 12.8f), new Vector3(2.4f, 0.9f, 0.1f), threat);
        CreateHoloPanel(root.transform, new Vector3(-1.6f, 2.8f, 10.2f), new Vector3(1.8f, 0.7f, 0.1f), trim);
        CreateHoloPanel(root.transform, new Vector3(10.8f, 6.8f, 20.8f), new Vector3(5.8f, 1.5f, 0.1f), threat);
        CreateGroundStrip("MenuSkyBridge", new Vector3(3.6f, 8.2f, 19.6f), new Vector3(13.2f, 0.24f, 1.2f), floor);
        CreateGroundStrip("MenuSkyBridgeGlow", new Vector3(3.6f, 8.34f, 19.6f), new Vector3(10.8f, 0.06f, 0.14f), trim);
        CreateGroundStrip("MenuBackdropShelf", new Vector3(4.8f, 11.4f, 28.8f), new Vector3(24f, 1.4f, 12f), floor);
        CreateGroundStrip("MenuBackdropGlow", new Vector3(4.8f, 11.62f, 28.8f), new Vector3(16f, 0.1f, 4.4f), threat);
    }

    private static void CreateGameOverShowcase(BootstrapAssets assets)
    {
        GameObject root = new GameObject("GameOverShowcase");
        Material floor = CreateMaterial("DebriefFloor", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0.96f, 1f) * 0.08f);
        Material reward = CreateMaterial("DebriefReward", new Color(0.18f, 0.12f, 0.04f), new Color(1f, 0.82f, 0.22f));
        Material threat = CreateMaterial("DebriefThreat", new Color(0.18f, 0.03f, 0.12f), new Color(1f, 0.22f, 0.68f));

        CreateShowcasePedestal(root.transform, new Vector3(-4f, 0.2f, 8.8f), new Vector3(2.4f, 0.36f, 2.4f), floor, reward);
        CreateShowcasePedestal(root.transform, new Vector3(4.8f, 0.2f, 9.6f), new Vector3(2.8f, 0.42f, 2.8f), floor, threat);
        CreateShowcaseTower(root.transform, new Vector3(-7.6f, 4.6f, 21f), new Vector3(3.4f, 9.2f, 3.2f), floor, reward);
        CreateShowcaseTower(root.transform, new Vector3(8f, 5.2f, 24f), new Vector3(3.8f, 10.4f, 3.8f), floor, threat);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(assets.Player);
        PrepareShowcaseObject(player);
        player.transform.position = new Vector3(-4f, 0.42f, 8.9f);
        player.transform.rotation = Quaternion.Euler(0f, 146f, 0f);
        player.name = "DebriefCourier";

        GameObject rewardCrate = new GameObject("RewardCrate");
        CreateVisualPrimitive("CrateBase", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(1.3f, 0.9f, 1.3f), floor, rewardCrate.transform);
        CreateVisualPrimitive("CrateCore", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.28f), new Vector3(0.72f, 0.44f, 0.12f), reward, rewardCrate.transform);
        CreateVisualPrimitive("CrateBand", PrimitiveType.Cube, new Vector3(0f, 1.02f, 0f), new Vector3(1.44f, 0.08f, 1.44f), reward, rewardCrate.transform);
        rewardCrate.transform.position = new Vector3(4.6f, 0f, 9.8f);

        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(assets.Boss);
        PrepareShowcaseObject(boss);
        boss.transform.position = new Vector3(7.4f, 2.4f, 14.2f);
        boss.transform.rotation = Quaternion.Euler(0f, 222f, 0f);
        boss.transform.localScale *= 0.56f;
        boss.name = "DebriefBossBackdrop";

        CreateHoloPanel(root.transform, new Vector3(0.8f, 4.8f, 15.8f), new Vector3(4.6f, 1.2f, 0.1f), reward);
    }

    private static void CreateShowcasePedestal(Transform parent, Vector3 position, Vector3 scale, Material shell, Material accent)
    {
        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.name = "Pedestal";
        baseObject.transform.SetParent(parent, false);
        baseObject.transform.position = position;
        baseObject.transform.localScale = scale;
        baseObject.GetComponent<Renderer>().sharedMaterial = shell;
        Object.DestroyImmediate(baseObject.GetComponent<BoxCollider>());

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ring.name = "PedestalRing";
        ring.transform.SetParent(parent, false);
        ring.transform.position = position + new Vector3(0f, (scale.y * 0.55f), 0f);
        ring.transform.localScale = new Vector3(scale.x * 1.08f, 0.08f, scale.z * 1.08f);
        ring.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(ring.GetComponent<BoxCollider>());
    }

    private static void CreateShowcaseTower(Transform parent, Vector3 position, Vector3 scale, Material shell, Material accent)
    {
        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.name = "BackdropTower";
        tower.transform.SetParent(parent, false);
        tower.transform.position = position;
        tower.transform.localScale = scale;
        tower.GetComponent<Renderer>().sharedMaterial = shell;
        Object.DestroyImmediate(tower.GetComponent<BoxCollider>());

        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trim.name = "TowerTrim";
        trim.transform.SetParent(parent, false);
        trim.transform.position = position + new Vector3(scale.x * 0.42f, 0f, 0f);
        trim.transform.localScale = new Vector3(0.16f, scale.y * 0.9f, scale.z * 0.7f);
        trim.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(trim.GetComponent<BoxCollider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crown.name = "TowerCrown";
        crown.transform.SetParent(parent, false);
        crown.transform.position = position + new Vector3(0f, scale.y * 0.52f, 0f);
        crown.transform.localScale = new Vector3(scale.x * 0.82f, 0.14f, scale.z * 0.82f);
        crown.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(crown.GetComponent<BoxCollider>());

        GameObject spine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spine.name = "TowerSpine";
        spine.transform.SetParent(parent, false);
        spine.transform.position = position + new Vector3(-scale.x * 0.22f, 0f, scale.z * 0.22f);
        spine.transform.localScale = new Vector3(0.12f, scale.y * 0.88f, 0.18f);
        spine.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(spine.GetComponent<BoxCollider>());
    }

    private static void CreateHoloPanel(Transform parent, Vector3 position, Vector3 scale, Material accent)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "HoloPanel";
        panel.transform.SetParent(parent, false);
        panel.transform.position = position;
        panel.transform.localScale = scale;
        panel.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(panel.GetComponent<BoxCollider>());

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "HoloFrame";
        frame.transform.SetParent(parent, false);
        frame.transform.position = position + new Vector3(0f, 0f, -0.12f);
        frame.transform.localScale = new Vector3(scale.x * 1.08f, scale.y * 1.08f, 0.04f);
        frame.GetComponent<Renderer>().sharedMaterial = CreateMaterial("HoloBillboard", new Color(0.04f, 0.08f, 0.12f), accent.color);
        Object.DestroyImmediate(frame.GetComponent<BoxCollider>());
    }

    private static void CreateShowcaseRunway(Transform parent, Vector3 position, Vector3 scale, Material shell, Material cyanAccent, Material magentaAccent)
    {
        GameObject baseStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseStrip.name = "ShowcaseRunway";
        baseStrip.transform.SetParent(parent, false);
        baseStrip.transform.position = position;
        baseStrip.transform.localScale = scale;
        baseStrip.GetComponent<Renderer>().sharedMaterial = shell;
        Object.DestroyImmediate(baseStrip.GetComponent<BoxCollider>());

        CreateVisualPrimitive("RunwayCore", PrimitiveType.Cube, position + new Vector3(0f, 0.04f, 0f), new Vector3(scale.x * 0.12f, 0.02f, scale.z * 0.84f), cyanAccent, parent);
        CreateVisualPrimitive("RunwayEdgeLeft", PrimitiveType.Cube, position + new Vector3(-(scale.x * 0.42f), 0.05f, 0f), new Vector3(0.08f, 0.02f, scale.z * 0.92f), magentaAccent, parent);
        CreateVisualPrimitive("RunwayEdgeRight", PrimitiveType.Cube, position + new Vector3(scale.x * 0.42f, 0.05f, 0f), new Vector3(0.08f, 0.02f, scale.z * 0.92f), cyanAccent, parent);
    }

    private static void CreateShowcaseBeacon(Transform parent, Vector3 position, Material accent, float height)
    {
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "ShowcaseBeacon";
        beacon.transform.SetParent(parent, false);
        beacon.transform.position = position;
        beacon.transform.localScale = new Vector3(0.12f, height * 0.5f, 0.12f);
        beacon.GetComponent<Renderer>().sharedMaterial = accent;
        Object.DestroyImmediate(beacon.GetComponent<CapsuleCollider>());

        CreateVisualPrimitive("BeaconHead", PrimitiveType.Sphere, position + new Vector3(0f, height, 0f), Vector3.one * 0.26f, accent, parent);
    }

    private static void PrepareShowcaseObject(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Object.DestroyImmediate(behaviours[i]);
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            Object.DestroyImmediate(controllers[i]);
        }
    }

    private static void CreateHackRing(Transform parent, Color color)
    {
        GameObject ring = new GameObject("HackRing", typeof(RectTransform), typeof(Image));
        ring.transform.SetParent(parent, false);
        RectTransform rect = ring.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.08f);
        rect.anchorMax = new Vector2(0.92f, 0.92f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = ring.GetComponent<Image>();
        image.color = color;
        Outline outline = ring.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.96f, 1f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static LevelChunkSet[] BuildChunkSets(BootstrapAssets assets)
    {
        return new[]
        {
            new LevelChunkSet { Name = "Neon Gateway // Arrival Spine", StartDistance = 0f, ChunkPrefabs = assets.GatewayChunks },
            new LevelChunkSet { Name = "Market Strip // Commerce Veins", StartDistance = 850f, ChunkPrefabs = assets.CommerceChunks },
            new LevelChunkSet { Name = "Security Grid // Citadel Threshold", StartDistance = 1800f, ChunkPrefabs = assets.SecurityChunks }
        };
    }

    private static void CreateChunkShoulderModules(Transform parent, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent, bool addCanopies)
    {
        Material shell = CreateMaterial("ChunkShoulderShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        float[] zPositions = { 5.5f, 15f, 24.2f };
        for (int i = 0; i < zPositions.Length; i++)
        {
            float z = zPositions[i];
            float leftWidth = 1.15f + (i * 0.16f);
            float rightWidth = 1.05f + (i * 0.12f);
            CreateVisualPrimitive($"ShoulderPodLeft_{i}", PrimitiveType.Cube, new Vector3(-6.7f, 1.18f + (i * 0.12f), z), new Vector3(leftWidth, 2.1f + (i * 0.24f), 1.45f), shell, parent);
            CreateVisualPrimitive($"ShoulderPodRight_{i}", PrimitiveType.Cube, new Vector3(6.74f, 1.24f + (i * 0.14f), z + 1.4f), new Vector3(rightWidth, 2.25f + (i * 0.18f), 1.56f), shell, parent);
            CreateVisualPrimitive($"ShoulderGlowLeft_{i}", PrimitiveType.Cube, new Vector3(-6.1f, 1.92f + (i * 0.12f), z + 0.22f), new Vector3(0.1f, 1.42f, 1.02f), i % 2 == 0 ? primaryAccent : tertiaryAccent, parent);
            CreateVisualPrimitive($"ShoulderGlowRight_{i}", PrimitiveType.Cube, new Vector3(6.14f, 1.98f + (i * 0.14f), z + 1.54f), new Vector3(0.1f, 1.5f, 1.08f), i % 2 == 0 ? secondaryAccent : tertiaryAccent, parent);
            if (!addCanopies)
            {
                continue;
            }

            CreateVisualPrimitive($"ShoulderCanopyLeft_{i}", PrimitiveType.Cube, new Vector3(-6.64f, 2.45f + (i * 0.16f), z - 0.48f), new Vector3(1.68f, 0.08f, 1.84f), primaryAccent, parent);
            CreateVisualPrimitive($"ShoulderCanopyRight_{i}", PrimitiveType.Cube, new Vector3(6.68f, 2.52f + (i * 0.12f), z + 1.92f), new Vector3(1.52f, 0.08f, 1.74f), secondaryAccent, parent);
        }
    }

    private static void CreateChunkForegroundAnchors(Transform parent, Material primaryAccent, Material secondaryAccent, bool addCrossBeam)
    {
        Material shell = CreateMaterial("ChunkAnchorShell", new Color(0.05f, 0.06f, 0.09f), Color.black);
        CreateVisualPrimitive("AnchorLeft", PrimitiveType.Cube, new Vector3(-8.7f, 2.8f, 10.4f), new Vector3(1.5f, 5.6f, 1.8f), shell, parent);
        CreateVisualPrimitive("AnchorRight", PrimitiveType.Cube, new Vector3(8.7f, 3.1f, 19.8f), new Vector3(1.7f, 6.2f, 2f), shell, parent);
        CreateVisualPrimitive("AnchorGlowLeft", PrimitiveType.Cube, new Vector3(-8.02f, 3.4f, 10.4f), new Vector3(0.1f, 4.1f, 1.4f), primaryAccent, parent);
        CreateVisualPrimitive("AnchorGlowRight", PrimitiveType.Cube, new Vector3(8.02f, 3.72f, 19.8f), new Vector3(0.1f, 4.6f, 1.44f), secondaryAccent, parent);
        if (addCrossBeam)
        {
            CreateVisualPrimitive("AnchorBeam", PrimitiveType.Cube, new Vector3(0f, 5.1f, 15f), new Vector3(12.4f, 0.14f, 0.4f), secondaryAccent, parent);
        }
    }

    private static void CreateChunkTransitRibbon(Transform parent, Material accentMaterial, bool enclosed)
    {
        Material shell = CreateMaterial("TransitRibbonShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        float y = enclosed ? 4.2f : 5.2f;
        for (int i = 0; i < 3; i++)
        {
            float z = 6f + (i * 8.4f);
            float width = enclosed ? 9.6f - (i * 0.4f) : 12.8f - (i * 0.6f);
            CreateVisualPrimitive($"TransitTube_{i}", PrimitiveType.Cube, new Vector3(0f, y + (i * 0.2f), z), new Vector3(width, 0.2f, 0.5f), shell, parent);
            CreateVisualPrimitive($"TransitTubeGlow_{i}", PrimitiveType.Cube, new Vector3(0f, y + 0.12f + (i * 0.2f), z), new Vector3(width * 0.72f, 0.05f, 0.12f), accentMaterial, parent);
        }
    }

    private static void CreateChunkSecurityCheckpoint(Transform parent, Material primaryAccent, Material secondaryAccent, Material warningAccent)
    {
        Material shell = CreateMaterial("ChunkCheckpointShell", new Color(0.05f, 0.06f, 0.09f), Color.black);
        CreateVisualPrimitive("CheckpointTowerLeft", PrimitiveType.Cube, new Vector3(-6.3f, 2.7f, 14.8f), new Vector3(0.5f, 5.4f, 0.5f), shell, parent);
        CreateVisualPrimitive("CheckpointTowerRight", PrimitiveType.Cube, new Vector3(6.3f, 2.7f, 14.8f), new Vector3(0.5f, 5.4f, 0.5f), shell, parent);
        CreateVisualPrimitive("CheckpointBar", PrimitiveType.Cube, new Vector3(0f, 4.85f, 14.8f), new Vector3(12.8f, 0.12f, 0.22f), warningAccent, parent);
        CreateVisualPrimitive("CheckpointScanA", PrimitiveType.Cube, new Vector3(0f, 2.1f, 13.1f), new Vector3(10.8f, 0.04f, 0.08f), primaryAccent, parent);
        CreateVisualPrimitive("CheckpointScanB", PrimitiveType.Cube, new Vector3(0f, 2.9f, 16.3f), new Vector3(10.8f, 0.04f, 0.08f), secondaryAccent, parent);
    }

    private static void CreateChunkMarketStalls(Transform parent, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent)
    {
        Material shell = CreateMaterial("ChunkMarketShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        CreateVisualPrimitive("MarketHubLeft", PrimitiveType.Cube, new Vector3(-7.2f, 1.4f, 8.6f), new Vector3(1.9f, 2.4f, 2f), shell, parent);
        CreateVisualPrimitive("MarketHubRight", PrimitiveType.Cube, new Vector3(7.1f, 1.4f, 21.2f), new Vector3(1.9f, 2.4f, 2f), shell, parent);
        CreateVisualPrimitive("MarketCanopyLeft", PrimitiveType.Cube, new Vector3(-7.2f, 2.7f, 8.4f), new Vector3(2.4f, 0.08f, 2.6f), primaryAccent, parent);
        CreateVisualPrimitive("MarketCanopyRight", PrimitiveType.Cube, new Vector3(7.1f, 2.7f, 21f), new Vector3(2.4f, 0.08f, 2.6f), secondaryAccent, parent);
        CreateVisualPrimitive("MarketBannerLeft", PrimitiveType.Cube, new Vector3(-7.2f, 2f, 9.6f), new Vector3(1.6f, 0.7f, 0.06f), tertiaryAccent, parent);
        CreateVisualPrimitive("MarketBannerRight", PrimitiveType.Cube, new Vector3(7.1f, 2f, 19.8f), new Vector3(1.6f, 0.7f, 0.06f), tertiaryAccent, parent);
    }

    private static void CreateChunkDistrictSignature(Transform parent, ChunkVisualStyle style, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent, Material warningAccent)
    {
        Material shell = CreateMaterial("ChunkTowerShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        switch (style)
        {
            case ChunkVisualStyle.Gateway:
                CreateVisualPrimitive("GatewayPortalRing", PrimitiveType.Cylinder, new Vector3(0f, 5.6f, 14.8f), new Vector3(4.2f, 0.06f, 4.2f), primaryAccent, parent);
                CreateVisualPrimitive("GatewayPortalCore", PrimitiveType.Cube, new Vector3(0f, 4.92f, 14.8f), new Vector3(7.2f, 0.08f, 0.16f), tertiaryAccent, parent);
                CreateVisualPrimitive("GatewayPylonLeft", PrimitiveType.Cube, new Vector3(-7.9f, 3.8f, 12.2f), new Vector3(1.1f, 7.6f, 1.6f), shell, parent);
                CreateVisualPrimitive("GatewayPylonRight", PrimitiveType.Cube, new Vector3(7.9f, 3.8f, 17.6f), new Vector3(1.1f, 7.6f, 1.6f), shell, parent);
                break;
            case ChunkVisualStyle.Billboard:
                CreateVisualPrimitive("AdBladeLeft", PrimitiveType.Cube, new Vector3(-8.2f, 4.8f, 12.4f), new Vector3(2.6f, 2.2f, 0.14f), primaryAccent, parent);
                CreateVisualPrimitive("AdBladeRight", PrimitiveType.Cube, new Vector3(8.2f, 5.2f, 18.6f), new Vector3(2.6f, 2.2f, 0.14f), warningAccent, parent);
                CreateVisualPrimitive("AdSpine", PrimitiveType.Cube, new Vector3(0f, 0.14f, 15f), new Vector3(8.2f, 0.05f, 0.12f), tertiaryAccent, parent);
                break;
            case ChunkVisualStyle.Bridge:
                CreateVisualPrimitive("BridgeRibLeft", PrimitiveType.Cube, new Vector3(-4.6f, 3.9f, 10f), new Vector3(0.16f, 5.6f, 0.3f), secondaryAccent, parent);
                CreateVisualPrimitive("BridgeRibRight", PrimitiveType.Cube, new Vector3(4.6f, 3.9f, 20f), new Vector3(0.16f, 5.6f, 0.3f), tertiaryAccent, parent);
                CreateVisualPrimitive("BridgeRibbon", PrimitiveType.Cube, new Vector3(0f, 6.6f, 15f), new Vector3(11.2f, 0.06f, 0.14f), primaryAccent, parent);
                break;
            case ChunkVisualStyle.Tunnel:
                CreateVisualPrimitive("TunnelMouthLeft", PrimitiveType.Cube, new Vector3(-4.9f, 2.4f, 4.2f), new Vector3(0.14f, 4.8f, 0.24f), primaryAccent, parent);
                CreateVisualPrimitive("TunnelMouthRight", PrimitiveType.Cube, new Vector3(4.9f, 2.4f, 4.2f), new Vector3(0.14f, 4.8f, 0.24f), secondaryAccent, parent);
                CreateVisualPrimitive("TunnelMouthTop", PrimitiveType.Cube, new Vector3(0f, 4.86f, 4.2f), new Vector3(10.1f, 0.14f, 0.24f), tertiaryAccent, parent);
                break;
            case ChunkVisualStyle.Security:
                CreateVisualPrimitive("SecurityScanWallLeft", PrimitiveType.Cube, new Vector3(-6.4f, 2f, 14.2f), new Vector3(0.16f, 3.2f, 5.6f), primaryAccent, parent);
                CreateVisualPrimitive("SecurityScanWallRight", PrimitiveType.Cube, new Vector3(6.4f, 2f, 14.2f), new Vector3(0.16f, 3.2f, 5.6f), secondaryAccent, parent);
                CreateVisualPrimitive("SecurityThreatBar", PrimitiveType.Cube, new Vector3(0f, 5.4f, 14.2f), new Vector3(11.4f, 0.12f, 0.2f), warningAccent, parent);
                break;
            case ChunkVisualStyle.Plaza:
                CreateVisualPrimitive("PlazaLanternLeft", PrimitiveType.Cylinder, new Vector3(-6.8f, 3.4f, 11.2f), new Vector3(0.42f, 0.16f, 0.42f), warningAccent, parent);
                CreateVisualPrimitive("PlazaLanternRight", PrimitiveType.Cylinder, new Vector3(6.8f, 3.6f, 19.2f), new Vector3(0.42f, 0.16f, 0.42f), tertiaryAccent, parent);
                CreateVisualPrimitive("PlazaConcourse", PrimitiveType.Cube, new Vector3(0f, 0.18f, 15f), new Vector3(8.6f, 0.05f, 3.4f), primaryAccent, parent);
                break;
            case ChunkVisualStyle.Transit:
                CreateVisualPrimitive("TransitCarrierLeft", PrimitiveType.Cube, new Vector3(-7.6f, 5.1f, 9.2f), new Vector3(1.3f, 0.22f, 2.8f), secondaryAccent, parent);
                CreateVisualPrimitive("TransitCarrierRight", PrimitiveType.Cube, new Vector3(7.6f, 5.3f, 20.8f), new Vector3(1.3f, 0.22f, 2.8f), tertiaryAccent, parent);
                CreateVisualPrimitive("TransitSignal", PrimitiveType.Cube, new Vector3(0f, 6.1f, 15f), new Vector3(9.2f, 0.08f, 0.16f), primaryAccent, parent);
                break;
            case ChunkVisualStyle.Citadel:
                CreateVisualPrimitive("CitadelButtressLeft", PrimitiveType.Cube, new Vector3(-7.8f, 3.8f, 9.8f), new Vector3(1.4f, 7.4f, 2.4f), shell, parent);
                CreateVisualPrimitive("CitadelButtressRight", PrimitiveType.Cube, new Vector3(7.8f, 4f, 20.2f), new Vector3(1.4f, 7.8f, 2.4f), shell, parent);
                CreateVisualPrimitive("CitadelEye", PrimitiveType.Cube, new Vector3(0f, 5.8f, 15f), new Vector3(8.8f, 0.16f, 0.2f), warningAccent, parent);
                CreateVisualPrimitive("CitadelSpine", PrimitiveType.Cube, new Vector3(0f, 6.6f, 15f), new Vector3(5.2f, 0.06f, 0.14f), tertiaryAccent, parent);
                break;
        }
    }

    private static void CreateDistrictGatewayCluster(float zPosition, BootstrapAssets assets)
    {
        CreateStreetGate(zPosition, assets.AccentMaterial, CreateMaterial("GatewayClusterShell", new Color(0.05f, 0.06f, 0.09f), Color.black));
        CreateDataSpire(zPosition + 10f, -11.2f, assets.TertiaryAccentMaterial);
        CreateDataSpire(zPosition + 18f, 11.2f, assets.AccentMaterial);
    }

    private static void CreateDistrictMarketCluster(float zPosition, BootstrapAssets assets)
    {
        CreateBillboard(zPosition, -9.6f, assets.AlternateAccentMaterial);
        CreateBillboard(zPosition + 14f, 9.4f, assets.WarningMaterial);
        CreateGroundStrip($"MarketConcourse_{zPosition:0}", new Vector3(0f, 4.8f, zPosition + 6f), new Vector3(12.4f, 0.18f, 1.1f), CreateMaterial("MarketConcourseShell", new Color(0.04f, 0.05f, 0.08f), Color.black));
        CreateGroundStrip($"MarketConcourseGlow_{zPosition:0}", new Vector3(0f, 4.96f, zPosition + 6f), new Vector3(9.4f, 0.05f, 0.1f), assets.WarningMaterial);
    }

    private static void CreateDistrictSecurityCluster(float zPosition, BootstrapAssets assets)
    {
        CreateSecurityArray(zPosition, assets.WarningMaterial, assets.TertiaryAccentMaterial);
        CreateStreetGate(zPosition + 12f, assets.TertiaryAccentMaterial, CreateMaterial("SecurityGateShell", new Color(0.05f, 0.06f, 0.1f), Color.black));
        CreateDataSpire(zPosition + 16f, -10.6f, assets.WarningMaterial);
        CreateDataSpire(zPosition + 20f, 10.6f, assets.AccentMaterial);
    }

    private static void CreateSecurityArray(float zPosition, Material primaryAccent, Material secondaryAccent)
    {
        Material shell = CreateMaterial("SecurityArrayShell", new Color(0.05f, 0.06f, 0.09f), Color.black);
        CreateGroundStrip($"SecurityArrayLeft_{zPosition:0}", new Vector3(-10.2f, 3.8f, zPosition), new Vector3(1.4f, 7.6f, 1.8f), shell);
        CreateGroundStrip($"SecurityArrayRight_{zPosition:0}", new Vector3(10.2f, 3.8f, zPosition + 4f), new Vector3(1.4f, 7.6f, 1.8f), shell);
        CreateGroundStrip($"SecurityArrayGlowLeft_{zPosition:0}", new Vector3(-9.6f, 4.6f, zPosition), new Vector3(0.1f, 5.2f, 1.1f), primaryAccent);
        CreateGroundStrip($"SecurityArrayGlowRight_{zPosition:0}", new Vector3(9.6f, 4.6f, zPosition + 4f), new Vector3(0.1f, 5.2f, 1.1f), secondaryAccent);
    }

    private static void CreateBossApproachLandmarks(BootstrapAssets assets)
    {
        Material shell = CreateMaterial("BossLandmarkShell", new Color(0.05f, 0.06f, 0.1f), Color.black);
        Material[] accents = { assets.AccentMaterial, assets.WarningMaterial, assets.TertiaryAccentMaterial, assets.AlternateAccentMaterial };
        for (int tier = 1; tier <= 4; tier++)
        {
            float z = (tier * 1000f) - 54f;
            Material accent = accents[(tier - 1) % accents.Length];
            CreateStreetGate(z, accent, shell);
            CreateGroundStrip($"BossApproachPanel_{tier}", new Vector3(0f, 8.8f, z + 6f), new Vector3(10.8f, 0.22f, 2.2f), shell);
            CreateGroundStrip($"BossApproachGlow_{tier}", new Vector3(0f, 8.98f, z + 6f), new Vector3(8.2f, 0.06f, 1.2f), accent);
            CreateDataSpire(z + 10f, -12f, accent);
            CreateDataSpire(z + 18f, 12f, assets.WarningMaterial);
        }
    }

    private static void CreateChunkLaneLights(Transform parent, Material accentMaterial)
    {
        CreateVisualPrimitive("LaneLightLeft", PrimitiveType.Cube, new Vector3(-2.5f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
        CreateVisualPrimitive("LaneLightCenter", PrimitiveType.Cube, new Vector3(0f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
        CreateVisualPrimitive("LaneLightRight", PrimitiveType.Cube, new Vector3(2.5f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
        CreateVisualPrimitive("EnergySpine", PrimitiveType.Cube, new Vector3(0f, -0.01f, 15f), new Vector3(1.1f, 0.03f, 30f), CreateMaterial("ChunkEnergySpine", new Color(0.06f, 0.08f, 0.12f), Color.black), parent);
        for (int i = 0; i < 6; i++)
        {
            float z = 2.5f + (i * 5f);
            CreateVisualPrimitive($"EnergyNode_{i}", PrimitiveType.Cube, new Vector3(0f, 0.02f, z), new Vector3(0.9f, 0.03f, 1.1f), accentMaterial, parent);
        }
    }

    private static void CreateChunkArchSeries(Transform parent, Material primaryAccent, Material secondaryAccent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float z = 5f + (i * 6f);
            Material accent = i % 2 == 0 ? primaryAccent : secondaryAccent;
            CreateVisualPrimitive($"ArchBraceLeft_{i}", PrimitiveType.Cube, new Vector3(-4.76f + (i * 0.18f), 0.74f, z), new Vector3(0.42f, 1.48f, 0.3f), CreateMaterial("ChunkArchShell", new Color(0.05f, 0.06f, 0.09f), Color.black), parent);
            CreateVisualPrimitive($"ArchBraceRight_{i}", PrimitiveType.Cube, new Vector3(4.76f - (i * 0.18f), 0.74f, z), new Vector3(0.42f, 1.48f, 0.3f), CreateMaterial("ChunkArchShell", new Color(0.05f, 0.06f, 0.09f), Color.black), parent);
            CreateVisualPrimitive($"ArchLeft_{i}", PrimitiveType.Cube, new Vector3(-4.4f + (i * 0.18f), 2.1f, z), new Vector3(0.14f, 4.2f, 0.18f), accent, parent);
            CreateVisualPrimitive($"ArchRight_{i}", PrimitiveType.Cube, new Vector3(4.4f - (i * 0.18f), 2.1f, z), new Vector3(0.14f, 4.2f, 0.18f), accent, parent);
            CreateVisualPrimitive($"ArchTop_{i}", PrimitiveType.Cube, new Vector3(0f, 4.2f - (i * 0.18f), z), new Vector3(9f - (i * 0.35f), 0.14f, 0.18f), accent, parent);
            CreateVisualPrimitive($"ArchTopGlow_{i}", PrimitiveType.Cube, new Vector3(0f, 3.72f - (i * 0.18f), z), new Vector3(4.6f - (i * 0.18f), 0.05f, 0.1f), accent, parent);
        }
    }

    private static void CreateChunkBillboards(Transform parent, Material primaryAccent, Material warningAccent)
    {
        Material holoBoard = CreateMaterial("ChunkHoloBoard", new Color(0.05f, 0.12f, 0.18f), primaryAccent.color);
        Material warningBoard = CreateMaterial("ChunkWarningBoard", new Color(0.18f, 0.08f, 0.04f), warningAccent.color);
        CreateVisualPrimitive("BillboardPostLeft", PrimitiveType.Cube, new Vector3(-7f, 2.2f, 9f), new Vector3(0.3f, 4.4f, 0.3f), primaryAccent, parent);
        CreateVisualPrimitive("BillboardFrameLeft", PrimitiveType.Cube, new Vector3(-7f, 4.8f, 9f), new Vector3(3.8f, 2.1f, 0.16f), CreateMaterial("ChunkBillboardFrame", new Color(0.05f, 0.06f, 0.09f), Color.black), parent);
        CreateVisualPrimitive("BillboardFaceLeft", PrimitiveType.Cube, new Vector3(-7f, 4.8f, 9f), new Vector3(3.5f, 1.8f, 0.14f), holoBoard, parent);
        CreateVisualPrimitive("BillboardScanLeft", PrimitiveType.Cube, new Vector3(-7f, 4.24f, 9.08f), new Vector3(2.7f, 0.08f, 0.04f), primaryAccent, parent);
        CreateVisualPrimitive("BillboardPostRight", PrimitiveType.Cube, new Vector3(7f, 2.2f, 20f), new Vector3(0.3f, 4.4f, 0.3f), warningAccent, parent);
        CreateVisualPrimitive("BillboardFrameRight", PrimitiveType.Cube, new Vector3(7f, 4.8f, 20f), new Vector3(3.8f, 2.1f, 0.16f), CreateMaterial("ChunkBillboardFrame", new Color(0.05f, 0.06f, 0.09f), Color.black), parent);
        CreateVisualPrimitive("BillboardFaceRight", PrimitiveType.Cube, new Vector3(7f, 4.8f, 20f), new Vector3(3.5f, 1.8f, 0.14f), warningBoard, parent);
        CreateVisualPrimitive("BillboardScanRight", PrimitiveType.Cube, new Vector3(7f, 5.34f, 20.08f), new Vector3(2.7f, 0.08f, 0.04f), warningAccent, parent);
    }

    private static void CreateChunkSideStructures(Transform parent, Material shellAccent, Material lineAccent, bool addSecurityPosts)
    {
        Material towerShell = CreateMaterial("ChunkTowerShell", new Color(0.04f, 0.05f, 0.08f), Color.black);
        CreateVisualPrimitive("TowerLeft", PrimitiveType.Cube, new Vector3(-8f, 3f, 8f), new Vector3(2.2f, 6f, 2.8f), towerShell, parent);
        CreateVisualPrimitive("TowerRight", PrimitiveType.Cube, new Vector3(8f, 3.4f, 18f), new Vector3(2.4f, 6.8f, 3f), towerShell, parent);
        CreateVisualPrimitive("TowerTrimLeft", PrimitiveType.Cube, new Vector3(-6.85f, 3f, 8f), new Vector3(0.12f, 5.2f, 2.1f), shellAccent, parent);
        CreateVisualPrimitive("TowerTrimRight", PrimitiveType.Cube, new Vector3(6.75f, 3.4f, 18f), new Vector3(0.12f, 5.8f, 2.1f), lineAccent, parent);
        CreateVisualPrimitive("OverheadBridge", PrimitiveType.Cube, new Vector3(0f, 5.8f, 14f), new Vector3(12f, 0.24f, 1.5f), lineAccent, parent);
        CreateVisualPrimitive("BridgeUnderBrace", PrimitiveType.Cube, new Vector3(0f, 5.32f, 14f), new Vector3(10.8f, 0.1f, 0.48f), towerShell, parent);
        CreateVisualPrimitive("TowerWindowLeft", PrimitiveType.Cube, new Vector3(-8.1f, 3.8f, 7.62f), new Vector3(0.08f, 3.2f, 1.8f), lineAccent, parent);
        CreateVisualPrimitive("TowerWindowRight", PrimitiveType.Cube, new Vector3(8.08f, 4.12f, 18.44f), new Vector3(0.08f, 3.8f, 2f), shellAccent, parent);
        if (!addSecurityPosts) return;
        CreateVisualPrimitive("SecurityPostLeft", PrimitiveType.Cube, new Vector3(-5.6f, 1.2f, 14f), new Vector3(0.2f, 2.4f, 0.2f), shellAccent, parent);
        CreateVisualPrimitive("SecurityPostRight", PrimitiveType.Cube, new Vector3(5.6f, 1.2f, 14f), new Vector3(0.2f, 2.4f, 0.2f), shellAccent, parent);
        CreateVisualPrimitive("SecurityBeam", PrimitiveType.Cube, new Vector3(0f, 1.9f, 14f), new Vector3(11.2f, 0.06f, 0.08f), lineAccent, parent);
    }

    private static Material CreateMaterial(string name, Color albedo, Color emission)
    {
        string path = $"{MaterialsRoot}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        string lowerName = name.ToLowerInvariant();
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.color = albedo;
        Texture2D baseTexture = GeneratedArtBootstrapper.EnsureTexture(name, albedo, emission);
        ApplyBaseTexture(material, baseTexture, lowerName);
        ApplySurfaceColors(material, albedo, emission, lowerName);
        ApplySurfaceProperties(material, lowerName);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyBaseTexture(Material material, Texture2D baseTexture, string lowerName)
    {
        if (baseTexture == null)
        {
            return;
        }

        Vector2 tiling = GetMaterialTiling(lowerName);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", baseTexture);
            material.SetTextureScale("_BaseMap", tiling);
        }

        material.mainTexture = baseTexture;
        material.mainTextureScale = tiling;
        if (IsEmissiveSurface(lowerName) && material.HasProperty("_EmissionMap"))
        {
            material.SetTexture("_EmissionMap", baseTexture);
            material.SetTextureScale("_EmissionMap", tiling);
        }
    }

    private static void ApplySurfaceColors(Material material, Color albedo, Color emission, string lowerName)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", albedo);
        }

        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
        {
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", emission * GetEmissionBoost(lowerName));
        }
    }

    private static void ApplySurfaceProperties(Material material, string lowerName)
    {
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", GetMaterialSmoothness(lowerName));
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", GetMaterialMetallic(lowerName));
        }

        if (material.HasProperty("_OcclusionStrength"))
        {
            material.SetFloat("_OcclusionStrength", lowerName.Contains("road") || lowerName.Contains("sidewalk") ? 1f : 0.7f);
        }
    }

    private static bool IsEmissiveSurface(string lowerName)
    {
        return lowerName.Contains("neon")
            || lowerName.Contains("lane")
            || lowerName.Contains("warning")
            || lowerName.Contains("laser")
            || lowerName.Contains("visor")
            || lowerName.Contains("glass")
            || lowerName.Contains("holo")
            || lowerName.Contains("billboard")
            || lowerName.Contains("core")
            || lowerName.Contains("thruster")
            || lowerName.Contains("trim")
            || lowerName.Contains("accent");
    }

    private static float GetEmissionBoost(string lowerName)
    {
        if (lowerName.Contains("holo") || lowerName.Contains("billboard"))
        {
            return 4.2f;
        }

        if (lowerName.Contains("visor") || lowerName.Contains("glass") || lowerName.Contains("core"))
        {
            return 3.5f;
        }

        if (lowerName.Contains("warning") || lowerName.Contains("lane") || lowerName.Contains("laser") || lowerName.Contains("thruster"))
        {
            return 3f;
        }

        if (lowerName.Contains("hero") || lowerName.Contains("trim") || lowerName.Contains("accent"))
        {
            return 3.2f;
        }

        return 2.3f;
    }

    private static float GetMaterialSmoothness(string lowerName)
    {
        if (lowerName.Contains("glass") || lowerName.Contains("visor"))
        {
            return 0.95f;
        }

        if (lowerName.Contains("road"))
        {
            return 0.88f;
        }

        if (lowerName.Contains("hero") || lowerName.Contains("traffic"))
        {
            return 0.84f;
        }

        if (lowerName.Contains("tower") || lowerName.Contains("wall") || lowerName.Contains("metal") || lowerName.Contains("shell"))
        {
            return 0.78f;
        }

        return 0.74f;
    }

    private static float GetMaterialMetallic(string lowerName)
    {
        if (lowerName.Contains("glass") || lowerName.Contains("visor"))
        {
            return 0.08f;
        }

        if (lowerName.Contains("hero") || lowerName.Contains("metal") || lowerName.Contains("tower") || lowerName.Contains("drone") || lowerName.Contains("shell"))
        {
            return 0.4f;
        }

        if (lowerName.Contains("road") || lowerName.Contains("sidewalk"))
        {
            return 0.12f;
        }

        return 0.2f;
    }

    private static Vector2 GetMaterialTiling(string lowerName)
    {
        if (lowerName.Contains("road"))
        {
            return new Vector2(1.2f, 6f);
        }

        if (lowerName.Contains("sidewalk") || lowerName.Contains("shoulder"))
        {
            return new Vector2(1.5f, 4f);
        }

        if (lowerName.Contains("tower") || lowerName.Contains("wall") || lowerName.Contains("backdrop"))
        {
            return new Vector2(1f, 3f);
        }

        if (lowerName.Contains("billboard") || lowerName.Contains("holo"))
        {
            return new Vector2(1f, 1f);
        }

        return new Vector2(1f, 1f);
    }

    private static GameObject SavePrefab(GameObject root, string assetName)
    {
        string path = $"{PrefabsRoot}/{assetName}";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? prefab;
    }

    private static Font GetBuiltinFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesRoot}/{SceneNames.MainMenu}.unity", true),
            new EditorBuildSettingsScene($"{ScenesRoot}/{SceneNames.GameScene}.unity", true),
            new EditorBuildSettingsScene($"{ScenesRoot}/{SceneNames.GameOver}.unity", true)
        };
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void SetFloatField(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetVector3Field(Object target, string propertyName, Vector3 value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetEnumField(Object target, string propertyName, PowerUpType powerUpType)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.enumValueIndex = (int)powerUpType;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetTextField(Component target, string propertyName, Text textComponent)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = textComponent;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedField(Component target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        FieldInfo field = target.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }

        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
    }

    private static GameObject FindNamedChild(Transform parent, string childName)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == childName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private static Color ResolveThemeColor(System.Func<UiVisualTheme, Color> selector, Color fallback)
    {
        return s_uiTheme != null ? selector(s_uiTheme) : fallback;
    }

    private static BootstrapAssets ApplyCatalogOverrides(BootstrapAssets fallbackAssets, VisualAssetCatalog catalog)
    {
        if (catalog == null)
        {
            return fallbackAssets;
        }

        fallbackAssets.Player = ResolvePrefab(catalog.PlayerPrefab, fallbackAssets.Player);
        fallbackAssets.Projectile = ResolvePrefab(catalog.ProjectilePrefab, fallbackAssets.Projectile);
        fallbackAssets.Barrier = ResolvePrefab(catalog.BarrierPrefab, fallbackAssets.Barrier);
        fallbackAssets.Car = ResolvePrefab(catalog.CarPrefab, fallbackAssets.Car);
        fallbackAssets.Drone = ResolvePrefab(catalog.DronePrefab, fallbackAssets.Drone);
        fallbackAssets.Boss = ResolvePrefab(catalog.BossPrefab, fallbackAssets.Boss);
        fallbackAssets.BossHazard = ResolvePrefab(catalog.BossHazardPrefab, fallbackAssets.BossHazard);
        fallbackAssets.BossStage = ResolvePrefab(catalog.BossStagePrefab, fallbackAssets.BossStage);
        fallbackAssets.Credit = ResolvePrefab(catalog.CreditPrefab, fallbackAssets.Credit);
        fallbackAssets.PowerUps = ResolvePrefabs(catalog.PowerUpPrefabs, fallbackAssets.PowerUps, catalog.AllowGeneratedFallbacks);
        fallbackAssets.GatewayChunks = ResolvePrefabs(catalog.GatewayChunks, fallbackAssets.GatewayChunks, catalog.AllowGeneratedFallbacks);
        fallbackAssets.CommerceChunks = ResolvePrefabs(catalog.CommerceChunks, fallbackAssets.CommerceChunks, catalog.AllowGeneratedFallbacks);
        fallbackAssets.SecurityChunks = ResolvePrefabs(catalog.SecurityChunks, fallbackAssets.SecurityChunks, catalog.AllowGeneratedFallbacks);
        fallbackAssets.RoadMaterial = ResolveMaterial(catalog.RoadMaterial, fallbackAssets.RoadMaterial);
        fallbackAssets.AccentMaterial = ResolveMaterial(catalog.AccentMaterial, fallbackAssets.AccentMaterial);
        fallbackAssets.AlternateAccentMaterial = ResolveMaterial(catalog.AlternateAccentMaterial, fallbackAssets.AlternateAccentMaterial);
        fallbackAssets.TertiaryAccentMaterial = ResolveMaterial(catalog.TertiaryAccentMaterial, fallbackAssets.TertiaryAccentMaterial);
        fallbackAssets.WarningMaterial = ResolveMaterial(catalog.WarningMaterial, fallbackAssets.WarningMaterial);
        return fallbackAssets;
    }

    private static GameObject ResolvePrefab(GameObject authoredPrefab, GameObject fallbackPrefab)
    {
        return authoredPrefab != null ? authoredPrefab : fallbackPrefab;
    }

    private static Material ResolveMaterial(Material authoredMaterial, Material fallbackMaterial)
    {
        return authoredMaterial != null ? authoredMaterial : fallbackMaterial;
    }

    private static GameObject[] ResolvePrefabs(GameObject[] authoredPrefabs, GameObject[] fallbackPrefabs, bool allowGeneratedFallbacks)
    {
        if (authoredPrefabs != null && authoredPrefabs.Length > 0)
        {
            return authoredPrefabs;
        }

        return allowGeneratedFallbacks ? fallbackPrefabs : System.Array.Empty<GameObject>();
    }

    private static void ValidateBootstrapAssets(BootstrapAssets assets, VisualAssetCatalog catalog)
    {
        ValidateAssetReference(assets.Player, "Player prefab");
        ValidateAssetReference(assets.Projectile, "Projectile prefab");
        ValidateAssetReference(assets.Barrier, "Barrier prefab");
        ValidateAssetReference(assets.Car, "Car prefab");
        ValidateAssetReference(assets.Drone, "Drone prefab");
        ValidateAssetReference(assets.Boss, "Boss prefab");
        ValidateAssetReference(assets.BossHazard, "Boss hazard prefab");
        ValidateAssetReference(assets.BossStage, "Boss stage prefab");
        ValidateAssetReference(assets.Credit, "Credit prefab");
        ValidateArrayReference(assets.PowerUps, "Power-up prefabs");
        ValidateArrayReference(assets.GatewayChunks, "Neon Gateway chunk prefabs");
        ValidateArrayReference(assets.CommerceChunks, "Market Strip chunk prefabs");
        ValidateArrayReference(assets.SecurityChunks, "Security Corridor chunk prefabs");
        ValidateMaterialReference(assets.RoadMaterial, "Road material");
        ValidateMaterialReference(assets.AccentMaterial, "Accent material");
        ValidateMaterialReference(assets.AlternateAccentMaterial, "Alternate accent material");
        ValidateMaterialReference(assets.TertiaryAccentMaterial, "Tertiary accent material");
        ValidateMaterialReference(assets.WarningMaterial, "Warning material");

        if (catalog != null && catalog.RequireAuthoredAssets)
        {
            ValidateArrayReference(BuildChunkSets(assets), "District chunk sets");
        }
    }

    private static void ValidateAssetReference(Object asset, string label)
    {
        if (asset == null)
        {
            throw new System.InvalidOperationException($"{label} is missing. Refusing to generate scenes with an incomplete visual pipeline.");
        }
    }

    private static void ValidateMaterialReference(Material material, string label)
    {
        if (material == null)
        {
            throw new System.InvalidOperationException($"{label} is missing. Refusing to generate scenes with an incomplete visual pipeline.");
        }
    }

    private static void ValidateArrayReference<T>(T[] values, string label)
    {
        if (values == null || values.Length == 0)
        {
            throw new System.InvalidOperationException($"{label} are missing. Refusing to generate scenes with an incomplete visual pipeline.");
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
            {
                throw new System.InvalidOperationException($"{label} contains a null entry at index {i}. Refusing to generate scenes with an incomplete visual pipeline.");
            }
        }
    }

    private sealed class BootstrapAssets
    {
        public GameObject Player;
        public GameObject Projectile;
        public GameObject Barrier;
        public GameObject Car;
        public GameObject Drone;
        public GameObject Boss;
        public GameObject BossHazard;
        public GameObject BossStage;
        public GameObject Credit;
        public GameObject[] PowerUps;
        public GameObject[] GatewayChunks;
        public GameObject[] CommerceChunks;
        public GameObject[] SecurityChunks;
        public Material RoadMaterial;
        public Material AccentMaterial;
        public Material AlternateAccentMaterial;
        public Material TertiaryAccentMaterial;
        public Material WarningMaterial;
    }

    private enum ChunkVisualStyle
    {
        Gateway,
        Billboard,
        Bridge,
        Tunnel,
        Security,
        Plaza,
        Transit,
        Citadel
    }
}
