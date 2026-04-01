using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public static class SceneBootstrapper
{
    private const string AssetsRoot = "Assets";
    private const string ScenesRoot = "Assets/Scenes";
    private const string PrefabsRoot = "Assets/Prefabs";
    private const string MaterialsRoot = "Assets/Materials";

    [MenuItem("Cyber Drift Runner/Create Demo Scenes and Prefabs")]
    public static void CreateDemoScenesAndPrefabs()
    {
        EnsureFolder(AssetsRoot, "Scenes");
        EnsureFolder(AssetsRoot, "Prefabs");
        EnsureFolder(AssetsRoot, "Materials");
        RenderPipelineBootstrapper.EnsurePipelineAsset();
        GameplayConfigBootstrapper.ConfigBundle configs = GameplayConfigBootstrapper.EnsureConfigs();

        BootstrapAssets assets = CreateBootstrapAssets();
        CreateMainMenuScene(configs);
        CreateGameScene(assets, configs);
        CreateGameOverScene(configs);
        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene($"{ScenesRoot}/{SceneNames.MainMenu}.unity");
        EditorUtility.DisplayDialog("Cyber Drift Runner", "Scenes and prefabs are ready. Open MainMenu and press Play.", "OK");
    }

    private static BootstrapAssets CreateBootstrapAssets()
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
            CreateChunkPrefab("Chunk_Gateway_B.prefab", roadMaterial, neonBlue, neonPink, neonPurple, neonYellow, ChunkVisualStyle.Billboard)
        };
        assets.CommerceChunks = new[]
        {
            CreateChunkPrefab("Chunk_Commerce_A.prefab", roadMaterial, neonPink, neonBlue, neonPurple, neonYellow, ChunkVisualStyle.Billboard),
            CreateChunkPrefab("Chunk_Commerce_B.prefab", roadMaterial, neonPink, neonYellow, neonBlue, neonPurple, ChunkVisualStyle.Bridge)
        };
        assets.SecurityChunks = new[]
        {
            CreateChunkPrefab("Chunk_Security_A.prefab", roadMaterial, neonPurple, neonBlue, neonPink, neonYellow, ChunkVisualStyle.Security),
            CreateChunkPrefab("Chunk_Security_B.prefab", roadMaterial, neonPurple, neonPink, neonBlue, neonYellow, ChunkVisualStyle.Tunnel)
        };

        return assets;
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
        CreateVisualPrimitive("ArmBladeLeft", PrimitiveType.Cube, new Vector3(-0.5f, 0.52f, 0.12f), new Vector3(0.09f, 0.55f, 0.11f), material, root.transform);
        CreateVisualPrimitive("ArmBladeRight", PrimitiveType.Cube, new Vector3(0.5f, 0.52f, 0.12f), new Vector3(0.09f, 0.55f, 0.11f), material, root.transform);
        CreateVisualPrimitive("ForearmGlowLeft", PrimitiveType.Cube, new Vector3(-0.44f, 0.32f, 0.2f), new Vector3(0.05f, 0.36f, 0.06f), heroTrim, root.transform);
        CreateVisualPrimitive("ForearmGlowRight", PrimitiveType.Cube, new Vector3(0.44f, 0.32f, 0.2f), new Vector3(0.05f, 0.36f, 0.06f), heroTrim, root.transform);
        CreateVisualPrimitive("LegLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.34f, 0.04f), new Vector3(0.18f, 0.96f, 0.18f), heroShell, root.transform);
        CreateVisualPrimitive("LegRight", PrimitiveType.Cube, new Vector3(0.16f, -0.34f, 0.04f), new Vector3(0.18f, 0.96f, 0.18f), heroShell, root.transform);
        CreateVisualPrimitive("ThighLineLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.1f, 0.14f), new Vector3(0.07f, 0.74f, 0.05f), heroAccent, root.transform);
        CreateVisualPrimitive("ThighLineRight", PrimitiveType.Cube, new Vector3(0.16f, -0.1f, 0.14f), new Vector3(0.07f, 0.74f, 0.05f), heroAccent, root.transform);
        CreateVisualPrimitive("BootLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.9f, 0.2f), new Vector3(0.22f, 0.18f, 0.38f), heroTrim, root.transform);
        CreateVisualPrimitive("BootRight", PrimitiveType.Cube, new Vector3(0.16f, -0.9f, 0.2f), new Vector3(0.22f, 0.18f, 0.38f), heroTrim, root.transform);
        CreateVisualPrimitive("HeelLightLeft", PrimitiveType.Cube, new Vector3(-0.16f, -0.96f, -0.04f), new Vector3(0.16f, 0.06f, 0.08f), heroAccent, root.transform);
        CreateVisualPrimitive("HeelLightRight", PrimitiveType.Cube, new Vector3(0.16f, -0.96f, -0.04f), new Vector3(0.16f, 0.06f, 0.08f), heroAccent, root.transform);
        CreateVisualPrimitive("CoatTailLeft", PrimitiveType.Cube, new Vector3(-0.18f, -0.04f, -0.24f), new Vector3(0.16f, 0.74f, 0.06f), heroAccentPurple, root.transform);
        CreateVisualPrimitive("CoatTailRight", PrimitiveType.Cube, new Vector3(0.18f, -0.04f, -0.24f), new Vector3(0.16f, 0.74f, 0.06f), heroAccentPurple, root.transform);

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
        root.transform.localScale = new Vector3(2.3f, 2.35f, 1.4f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Material shell = CreateMaterial("BarrierMetal", new Color(0.06f, 0.06f, 0.08f), new Color(0f, 0f, 0f));
        root.GetComponent<Renderer>().sharedMaterial = shell;
        CreateVisualPrimitive("BarrierFrame", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0.7f), new Vector3(2.45f, 0.12f, 0.09f), material, root.transform);
        CreateVisualPrimitive("BarrierCore", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0.72f), new Vector3(1.58f, 1.5f, 0.08f), CreateMaterial("BarrierWarning", new Color(0.32f, 0.18f, 0.04f), new Color(1f, 0.68f, 0.15f)), root.transform);
        CreateVisualPrimitive("BarrierStripeL", PrimitiveType.Cube, new Vector3(-0.62f, -0.18f, 0.72f), new Vector3(0.15f, 1.7f, 0.08f), material, root.transform);
        CreateVisualPrimitive("BarrierStripeR", PrimitiveType.Cube, new Vector3(0.62f, -0.18f, 0.72f), new Vector3(0.15f, 1.7f, 0.08f), material, root.transform);
        CreateVisualPrimitive("BarrierCapLeft", PrimitiveType.Cylinder, new Vector3(-1.04f, -0.72f, 0.02f), new Vector3(0.16f, 0.28f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierCapRight", PrimitiveType.Cylinder, new Vector3(1.04f, -0.72f, 0.02f), new Vector3(0.16f, 0.28f, 0.16f), material, root.transform);
        CreateVisualPrimitive("BarrierLaserTop", PrimitiveType.Cube, new Vector3(0f, 0.84f, 0.74f), new Vector3(1.8f, 0.06f, 0.04f), CreateMaterial("BarrierLaser", new Color(0.14f, 0.03f, 0.14f), new Color(1f, 0f, 0.78f)), root.transform);
        CreateVisualPrimitive("BarrierLaserBottom", PrimitiveType.Cube, new Vector3(0f, -0.52f, 0.74f), new Vector3(1.8f, 0.05f, 0.04f), CreateMaterial("BarrierLaser", new Color(0.14f, 0.03f, 0.14f), new Color(1f, 0f, 0.78f)), root.transform);
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "Barrier.prefab");
    }

    private static GameObject CreateCarPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "CarObstacle";
        root.transform.localScale = new Vector3(2.1f, 0.92f, 4.2f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Material hull = CreateMaterial("CarHullDark", new Color(0.04f, 0.05f, 0.08f), new Color(0f, 0f, 0f));
        root.GetComponent<Renderer>().sharedMaterial = hull;
        CreateVisualPrimitive("SideBladeLeft", PrimitiveType.Cube, new Vector3(-1.08f, -0.06f, 0f), new Vector3(0.12f, 0.18f, 3.5f), material, root.transform);
        CreateVisualPrimitive("SideBladeRight", PrimitiveType.Cube, new Vector3(1.08f, -0.06f, 0f), new Vector3(0.12f, 0.18f, 3.5f), material, root.transform);
        CreateVisualPrimitive("Cabin", PrimitiveType.Cube, new Vector3(0f, 0.48f, -0.12f), new Vector3(1.28f, 0.5f, 1.95f), CreateMaterial("TrafficGlass", new Color(0.08f, 0.18f, 0.22f), new Color(0.18f, 0.95f, 1f)), root.transform);
        CreateVisualPrimitive("CanopyGlow", PrimitiveType.Cube, new Vector3(0f, 0.74f, -0.08f), new Vector3(1.06f, 0.06f, 1.5f), material, root.transform);
        CreateVisualPrimitive("ThrusterL", PrimitiveType.Cylinder, new Vector3(-0.76f, -0.22f, -1.76f), new Vector3(0.16f, 0.3f, 0.16f), CreateMaterial("NeonOrange", new Color(0.46f, 0.18f, 0.04f), new Color(1f, 0.5f, 0.18f)), root.transform);
        CreateVisualPrimitive("ThrusterR", PrimitiveType.Cylinder, new Vector3(0.76f, -0.22f, -1.76f), new Vector3(0.16f, 0.3f, 0.16f), CreateMaterial("NeonOrange", new Color(0.46f, 0.18f, 0.04f), new Color(1f, 0.5f, 0.18f)), root.transform);
        CreateVisualPrimitive("NoseLight", PrimitiveType.Cube, new Vector3(0f, 0.08f, 1.95f), new Vector3(1.1f, 0.08f, 0.08f), material, root.transform);
        CreateVisualPrimitive("TailRibbon", PrimitiveType.Cube, new Vector3(0f, 0.02f, -2.08f), new Vector3(0.8f, 0.04f, 0.12f), CreateMaterial("TrafficRibbon", new Color(0.08f, 0.03f, 0.16f), new Color(0.42f, 0f, 1f)), root.transform);
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "CarObstacle.prefab");
    }

    private static GameObject CreateDronePrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Drone";
        root.transform.localScale = new Vector3(1.18f, 0.72f, 1.18f);
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        Material shell = CreateMaterial("DroneShell", new Color(0.03f, 0.05f, 0.07f), new Color(0f, 0f, 0f));
        root.GetComponent<Renderer>().sharedMaterial = shell;
        CreateVisualPrimitive("WingLeft", PrimitiveType.Cube, new Vector3(-0.92f, 0f, 0f), new Vector3(0.95f, 0.07f, 0.28f), shell, root.transform);
        CreateVisualPrimitive("WingRight", PrimitiveType.Cube, new Vector3(0.92f, 0f, 0f), new Vector3(0.95f, 0.07f, 0.28f), shell, root.transform);
        CreateVisualPrimitive("EngineLeft", PrimitiveType.Cylinder, new Vector3(-0.62f, 0.06f, -0.08f), new Vector3(0.12f, 0.18f, 0.12f), CreateMaterial("DroneEngine", new Color(0.16f, 0.02f, 0.05f), new Color(1f, 0.22f, 0.26f)), root.transform);
        CreateVisualPrimitive("EngineRight", PrimitiveType.Cylinder, new Vector3(0.62f, 0.06f, -0.08f), new Vector3(0.12f, 0.18f, 0.12f), CreateMaterial("DroneEngine", new Color(0.16f, 0.02f, 0.05f), new Color(1f, 0.22f, 0.26f)), root.transform);
        CreateVisualPrimitive("Core", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.34f, CreateMaterial("DroneCore", new Color(0.14f, 0.03f, 0.05f), new Color(1f, 0.18f, 0.22f)), root.transform);
        CreateVisualPrimitive("TargetEye", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.44f), Vector3.one * 0.14f, CreateMaterial("DroneEye", new Color(0.2f, 0.04f, 0.06f), new Color(1f, 0.32f, 0.28f)), root.transform);
        CreateVisualPrimitive("AntennaTop", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.08f), new Vector3(0.06f, 0.26f, 0.06f), material, root.transform);
        root.AddComponent<EnemyDrone>();
        return SavePrefab(root, "Drone.prefab");
    }

    private static GameObject CreateBossPrefab(Material primaryAccent, Material secondaryAccent, Material tertiaryAccent)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "BossDrone";
        root.transform.localScale = new Vector3(3.2f, 1.75f, 3.2f);
        Object.DestroyImmediate(root.GetComponent<SphereCollider>());
        root.GetComponent<Renderer>().sharedMaterial = CreateMaterial("BossShell", new Color(0.03f, 0.04f, 0.06f), new Color(0.08f, 0.02f, 0.04f));
        CreateVisualPrimitive("BossRing", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(2.9f, 0.06f, 2.9f), tertiaryAccent, root.transform);
        CreateVisualPrimitive("BossCore", PrimitiveType.Sphere, new Vector3(0f, -0.05f, 0.62f), Vector3.one * 0.86f, CreateMaterial("BossCore", new Color(0.18f, 0.03f, 0.05f), new Color(1f, 0.18f, 0.24f)), root.transform);
        CreateVisualPrimitive("BossEye", PrimitiveType.Cube, new Vector3(0f, 0.18f, 1.42f), new Vector3(1.55f, 0.26f, 0.18f), CreateMaterial("BossEye", new Color(0.06f, 0.14f, 0.18f), new Color(0f, 0.96f, 1f)), root.transform);
        CreateVisualPrimitive("WingLeft", PrimitiveType.Cube, new Vector3(-2.18f, 0f, 0f), new Vector3(1.7f, 0.14f, 0.54f), primaryAccent, root.transform);
        CreateVisualPrimitive("WingRight", PrimitiveType.Cube, new Vector3(2.18f, 0f, 0f), new Vector3(1.7f, 0.14f, 0.54f), primaryAccent, root.transform);
        CreateVisualPrimitive("CannonLeft", PrimitiveType.Cylinder, new Vector3(-1.2f, -0.44f, 1.05f), new Vector3(0.2f, 0.46f, 0.2f), secondaryAccent, root.transform);
        CreateVisualPrimitive("CannonRight", PrimitiveType.Cylinder, new Vector3(1.2f, -0.44f, 1.05f), new Vector3(0.2f, 0.46f, 0.2f), secondaryAccent, root.transform);
        CreateVisualPrimitive("RearThruster", PrimitiveType.Cube, new Vector3(0f, -0.08f, -1.42f), new Vector3(1.4f, 0.18f, 0.18f), CreateMaterial("BossThruster", new Color(0.18f, 0.1f, 0.03f), new Color(1f, 0.62f, 0.18f)), root.transform);
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
        CreateVisualPrimitive("CreditHalo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.02f, 0.5f), CreateMaterial("NeonGold", new Color(0.44f, 0.34f, 0.08f), new Color(1f, 0.88f, 0.2f)), root.transform);
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

        PowerUpPickup pickup = root.AddComponent<PowerUpPickup>();
        SetEnumField(pickup, "powerUpType", powerUpType);
        SetFloatField(pickup, "duration", duration);

        return SavePrefab(root, $"{assetName}.prefab");
    }

    private static GameObject CreateChunkPrefab(string assetName, Material roadMaterial, Material primaryAccent, Material secondaryAccent, Material tertiaryAccent, Material warningAccent, ChunkVisualStyle style)
    {
        GameObject root = new GameObject(assetName.Replace(".prefab", string.Empty));
        CreateVisualPrimitive("ChunkRoad", PrimitiveType.Cube, new Vector3(0f, -0.16f, 15f), new Vector3(10.2f, 0.08f, 30f), roadMaterial, root.transform);
        CreateVisualPrimitive("ChunkLeftRail", PrimitiveType.Cube, new Vector3(-5.1f, 0.5f, 15f), new Vector3(0.08f, 1f, 30f), primaryAccent, root.transform);
        CreateVisualPrimitive("ChunkRightRail", PrimitiveType.Cube, new Vector3(5.1f, 0.5f, 15f), new Vector3(0.08f, 1f, 30f), secondaryAccent, root.transform);
        CreateChunkLaneLights(root.transform, tertiaryAccent);

        if (style == ChunkVisualStyle.Gateway || style == ChunkVisualStyle.Tunnel)
        {
            CreateChunkArchSeries(root.transform, primaryAccent, secondaryAccent, style == ChunkVisualStyle.Tunnel ? 5 : 3);
        }

        if (style == ChunkVisualStyle.Billboard || style == ChunkVisualStyle.Bridge)
        {
            CreateChunkBillboards(root.transform, primaryAccent, warningAccent);
        }

        if (style == ChunkVisualStyle.Bridge || style == ChunkVisualStyle.Security)
        {
            CreateChunkSideStructures(root.transform, secondaryAccent, tertiaryAccent, style == ChunkVisualStyle.Security);
        }

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
        new GameObject("DynamicMusic").AddComponent<DynamicMusicController>().SetupProceduralLayers();
        EnvironmentQualityController environmentQuality = new GameObject("EnvironmentQualityController").AddComponent<EnvironmentQualityController>();
        SetSerializedField(environmentQuality, "qualityConfig", configs.VisualQuality);

        GameObject pauseObject = new GameObject("PauseController");
        PauseController pauseCtrl = pauseObject.AddComponent<PauseController>();
        CreateHudCanvas(player.GetComponent<PlayerController>(), camera, pauseCtrl, out HoldButton holdButton);
        holdButton.Bind(player.GetComponent<PlayerController>());

        player.AddComponent<MagnetField>();
        camera.gameObject.AddComponent<DynamicCameraController>();

        GameObject spawnerObject = new GameObject("ObstacleSpawner");
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        GameObject poolsRoot = new GameObject("Pools");
        poolsRoot.transform.SetParent(spawnerObject.transform, false);
        spawner.Configure(player.GetComponent<PlayerController>(), new[] { assets.Barrier, assets.Car }, assets.Drone, assets.PowerUps, assets.Credit, poolsRoot.transform);
        SetSerializedField(spawner, "encounterConfig", configs.EncounterTuning);

        BossEncounterManager bossEncounter = new GameObject("BossEncounterManager").AddComponent<BossEncounterManager>();
        bossEncounter.Configure(player.GetComponent<PlayerController>(), spawner, assets.Boss, assets.BossHazard);

        LevelChunkGenerator chunkGenerator = new GameObject("LevelChunkGenerator").AddComponent<LevelChunkGenerator>();
        Transform chunkPoolRoot = new GameObject("ChunkPools").transform;
        chunkPoolRoot.SetParent(chunkGenerator.transform, false);
        chunkGenerator.Configure(player.GetComponent<PlayerController>(), BuildChunkSets(assets), chunkPoolRoot);
        EditorUtility.SetDirty(chunkGenerator);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameScene}.unity");
    }

    private static void CreateMainMenuScene(GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.MainMenu;

        CreatePersistentSystems(configs);
        CreateDirectionalLight();
        CreateStandaloneCamera(new Vector3(0f, 2f, -8f), new Vector3(15f, 0f, 0f));
        CreateEventSystem();

        GameObject menuRoot = new GameObject("MainMenuUI");
        MainMenuController controller = menuRoot.AddComponent<MainMenuController>();
        Canvas canvas = CreateCanvas("MainMenuCanvas");
        Font font = GetBuiltinFont();

        CreatePanel(canvas.transform, "MenuPanel", new Vector2(0f, -40f), new Vector2(420f, 640f));
        CreatePanel(canvas.transform, "TitlePanel", new Vector2(0f, 270f), new Vector2(920f, 140f));
        CreateText(canvas.transform, font, "Cyber Drift Runner", new Vector2(0f, 300f), new Vector2(900f, 120f), 54, TextAnchor.MiddleCenter, Color.white);
        CreateText(canvas.transform, font, "Neon courier. Hack. Dodge. Survive.", new Vector2(0f, 225f), new Vector2(760f, 60f), 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.9f, 1f));

        Button playButton = CreateButton(canvas.transform, font, "Play", new Vector2(0f, 70f), new Vector2(280f, 84f));
        Button skinsButton = CreateButton(canvas.transform, font, "Skins", new Vector2(0f, -28f), new Vector2(280f, 70f));
        Button upgradesButton = CreateButton(canvas.transform, font, "Upgrades", new Vector2(0f, -112f), new Vector2(280f, 70f));
        Button leaderboardButton = CreateButton(canvas.transform, font, "Leaderboard", new Vector2(0f, -196f), new Vector2(280f, 70f));
        Button settingsButton = CreateButton(canvas.transform, font, "Settings", new Vector2(0f, -280f), new Vector2(280f, 70f));
        Button quitButton = CreateButton(canvas.transform, font, "Quit", new Vector2(0f, -364f), new Vector2(280f, 70f));

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

        GameObject shopPanel = CreatePanel(canvas.transform, "ShopPanel", new Vector2(0f, 0f), new Vector2(680f, 560f));
        Text bankText = CreateText(shopPanel.transform, font, "Bank 0", new Vector2(0f, 220f), new Vector2(260f, 40f), 28, TextAnchor.MiddleCenter, Color.cyan);
        RectTransform shopContent = CreateContentRoot(shopPanel.transform, new Vector2(620f, 440f), -40f);
        SkinShopController shopController = shopPanel.AddComponent<SkinShopController>();
        shopController.Configure(shopPanel, bankText, shopContent, font);
        controller.BindShop(shopController);
        shopPanel.SetActive(false);

        GameObject leaderboardPanel = CreatePanel(canvas.transform, "LeaderboardPanel", new Vector2(0f, 0f), new Vector2(680f, 560f));
        CreateText(leaderboardPanel.transform, font, "LEADERBOARD", new Vector2(0f, 230f), new Vector2(420f, 44f), 32, TextAnchor.MiddleCenter, Color.cyan);
        RectTransform leaderboardContent = CreateContentRoot(leaderboardPanel.transform, new Vector2(620f, 440f), -46f);
        LeaderboardPanel lbController = leaderboardPanel.AddComponent<LeaderboardPanel>();
        lbController.Configure(leaderboardPanel, leaderboardContent, font);
        controller.BindLeaderboard(lbController);
        leaderboardPanel.SetActive(false);

        GameObject upgradePanel = CreatePanel(canvas.transform, "UpgradeShopPanel", new Vector2(0f, 0f), new Vector2(680f, 660f));
        Text upgradeCurrency = CreateText(upgradePanel.transform, font, "Credits: 0", new Vector2(0f, 280f), new Vector2(320f, 40f), 24, TextAnchor.MiddleCenter, Color.yellow);
        CreateText(upgradePanel.transform, font, "UPGRADES", new Vector2(0f, 236f), new Vector2(320f, 44f), 32, TextAnchor.MiddleCenter, Color.cyan);
        RectTransform upgradeContent = CreateContentRoot(upgradePanel.transform, new Vector2(620f, 550f), -36f);
        UpgradeShopController upgradeShop = upgradePanel.AddComponent<UpgradeShopController>();
        upgradeShop.Configure(upgradePanel, upgradeContent, upgradeCurrency, font);
        Button upgradeClose = CreateButton(upgradePanel.transform, font, "Close", new Vector2(0f, -294f), new Vector2(180f, 52f));
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

    private static void CreateGameOverScene(GameplayConfigBootstrapper.ConfigBundle configs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.GameOver;

        CreatePersistentSystems(configs);
        CreateDirectionalLight();
        CreateStandaloneCamera(new Vector3(0f, 2f, -8f), new Vector3(15f, 0f, 0f));
        CreateEventSystem();

        GameObject controllerObject = new GameObject("GameOverUI");
        GameOverController controller = controllerObject.AddComponent<GameOverController>();
        Canvas canvas = CreateCanvas("GameOverCanvas");
        Font font = GetBuiltinFont();

        CreatePanel(canvas.transform, "GameOverPanel", new Vector2(0f, -40f), new Vector2(700f, 760f));
        CreateText(canvas.transform, font, "Run Terminated", new Vector2(0f, 340f), new Vector2(700f, 100f), 48, TextAnchor.MiddleCenter, Color.white);
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

        Button retryButton = CreateButton(canvas.transform, font, "Retry", new Vector2(0f, -310f), new Vector2(220f, 70f));
        Button menuButton = CreateButton(canvas.transform, font, "Main Menu", new Vector2(0f, -394f), new Vector2(220f, 70f));
        Button shareButton = CreateButton(canvas.transform, font, "Share", new Vector2(0f, -478f), new Vector2(220f, 60f));

        controller.Configure(scoreText, distanceText, creditsText, survivalText);
        SetTextField(controller, "highScoreText", highScoreText);
        SetTextField(controller, "bestDistanceText", bestDistText);
        SetTextField(controller, "newHighScoreLabel", newHighLabel);
        SetTextField(controller, "nearBestText", nearBestText);
        SetTextField(controller, "leaderboardRankText", rankText);
        SetTextField(controller, "xpGainText", xpText);
        SetTextField(controller, "dailyChallengeText", challengeText);
        SetTextField(controller, "tipText", tipText);

        UnityEventTools.AddPersistentListener(retryButton.onClick, controller.Retry);
        UnityEventTools.AddPersistentListener(menuButton.onClick, controller.BackToMenu);
        UnityEventTools.AddPersistentListener(shareButton.onClick, controller.ShareScore);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameOver}.unity");
    }

    private static void CreatePersistentSystems(GameplayConfigBootstrapper.ConfigBundle configs)
    {
        GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
        new GameObject("MonetizationManager").AddComponent<MonetizationManager>();
        new GameObject("AudioManager").AddComponent<AudioManager>();
        new GameObject("MobilePerformanceManager").AddComponent<MobilePerformanceManager>();
        new GameObject("XpLevelSystem").AddComponent<XpLevelSystem>();
        new GameObject("DailyRewardSystem").AddComponent<DailyRewardSystem>();
        new GameObject("DailyChallengeSystem").AddComponent<DailyChallengeSystem>();
        new GameObject("MissionSystem").AddComponent<MissionSystem>();
        new GameObject("AchievementSystem").AddComponent<AchievementSystem>();
        new GameObject("UpgradeSystem").AddComponent<UpgradeSystem>();
        LeaderboardSystem leaderboard = new GameObject("LeaderboardSystem").AddComponent<LeaderboardSystem>();
        MockLeaderboardTransport leaderboardTransport = leaderboard.gameObject.AddComponent<MockLeaderboardTransport>();
        new GameObject("SeasonPassSystem").AddComponent<SeasonPassSystem>();
        new GameObject("MonetizationV2").AddComponent<MonetizationV2>();
        new GameObject("AnalyticsManager").AddComponent<AnalyticsManager>();
        new GameObject("ShareManager").AddComponent<ShareManager>();
        SettingsManager settingsManager = new GameObject("SettingsManager").AddComponent<SettingsManager>();
        new GameObject("GooglePlayManager").AddComponent<GooglePlayManager>();
        new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
        new GameObject("NotificationScheduler").AddComponent<NotificationScheduler>();
        new GameObject("TipSystem").AddComponent<TipSystem>();
        new GameObject("ConsentManager").AddComponent<ConsentManager>();
        new GameObject("SceneLoader").AddComponent<SceneLoader>();
        new GameObject("RateAppPrompt").AddComponent<RateAppPrompt>();
        SetSerializedField(gameManager, "balanceConfig", configs.RunnerBalance);
        SetSerializedField(settingsManager, "qualityConfig", configs.VisualQuality);
        SetSerializedField(leaderboard, "transportBehaviour", leaderboardTransport);
    }

    private static void CreateDirectionalLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(0.6f, 0.8f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
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
        CreateBackdropBand(-34f, 26f, assets.TertiaryAccentMaterial);
        CreateBackdropBand(34f, 29f, assets.AccentMaterial);
        CreateGroundStrip("CenterEnergySpine", new Vector3(0f, -0.04f, 5000f), new Vector3(0.22f, 0.02f, 10000f), assets.TertiaryAccentMaterial);

        CreateLaneMarker(-2.5f, assets.AccentMaterial);
        CreateLaneMarker(0f, assets.AccentMaterial);
        CreateLaneMarker(2.5f, assets.AccentMaterial);

        for (int i = 0; i < 22; i++)
        {
            float z = 120f + (i * 180f);
            CreateStreetGate(z, assets.AccentMaterial, CreateMaterial("GateMetal", new Color(0.08f, 0.08f, 0.14f), new Color(0f, 0f, 0f)));
            CreateSkylineCluster(z, -15.5f, assets.AccentMaterial);
            CreateSkylineCluster(z + 50f, 15.5f, assets.AlternateAccentMaterial);
            CreateBillboard(z + 26f, -11.8f, assets.AccentMaterial);
            CreateBillboard(z + 82f, 11.8f, assets.WarningMaterial);
            CreateSideStructure(z + 18f, -8.6f, assets.AccentMaterial, false);
            CreateSideStructure(z + 96f, 8.6f, assets.AlternateAccentMaterial, true);
            CreateDataSpire(z + 42f, -13.4f, assets.TertiaryAccentMaterial);
            CreateDataSpire(z + 122f, 13.4f, assets.WarningMaterial);
            if (i % 2 == 0)
            {
                CreateSkyBridge(z + 54f, assets.AlternateAccentMaterial);
            }

            if (i % 3 == 1)
            {
                CreateNeonTunnelSegment(z + 108f, assets.AccentMaterial, assets.TertiaryAccentMaterial);
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

    private static Camera CreateCamera(Transform target)
    {
        return CreateStandaloneCamera(new Vector3(0f, 6.8f, -9.2f), new Vector3(26f, 0f, 0f));
    }

    private static Camera CreateStandaloneCamera(Vector3 position, Vector3 rotation)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(0.01f, 0.015f, 0.04f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.farClipPlane = 280f;
        camera.fieldOfView = 64f;
        UniversalAdditionalCameraData urpData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = true;
        urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        cameraObject.transform.position = position;
        cameraObject.transform.rotation = Quaternion.Euler(rotation);
        return camera;
    }

    private static void CreateHudCanvas(PlayerController player, Camera camera, PauseController pauseCtrl, out HoldButton holdButton)
    {
        Canvas canvas = CreateCanvas("HUDCanvas");
        canvas.worldCamera = camera;
        Font font = GetBuiltinFont();

        GameObject hudRoot = new GameObject("HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        HUDController hudController = hudRoot.AddComponent<HUDController>();

        GameObject infoPanel = CreatePanel(canvas.transform, "HudInfoPanel", Vector2.zero, new Vector2(420f, 200f));
        RectTransform infoRect = infoPanel.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0f, 1f);
        infoRect.anchorMax = new Vector2(0f, 1f);
        infoRect.pivot = new Vector2(0f, 1f);
        infoRect.anchoredPosition = new Vector2(24f, -24f);
        CreatePanel(canvas.transform, "PowerTrayPanel", new Vector2(0f, 0f), new Vector2(540f, 120f)).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 820f);

        Text scoreText = CreateText(canvas.transform, font, "Score 000000", new Vector2(150f, -40f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text distanceText = CreateText(canvas.transform, font, "Distance 0m", new Vector2(150f, -80f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text creditsText = CreateText(canvas.transform, font, "Credits 0", new Vector2(150f, -120f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text powerUpText = CreateText(canvas.transform, font, "Ready", new Vector2(150f, -160f), new Vector2(360f, 40f), 24, TextAnchor.MiddleLeft, Color.cyan);
        Text missionText = CreateText(canvas.transform, font, "Mission", new Vector2(200f, -200f), new Vector2(420f, 40f), 22, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f));
        Text bossText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 110f), new Vector2(420f, 50f), 30, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.35f));
        Text comboText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 200f), new Vector2(300f, 60f), 36, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.15f));
        Text zoneText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 260f), new Vector2(600f, 50f), 28, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.5f));
        Text feverText = CreateText(canvas.transform, font, string.Empty, new Vector2(0f, 320f), new Vector2(440f, 60f), 30, TextAnchor.MiddleCenter, new Color(1f, 0.35f, 0.15f));
        Text fpsText = CreateText(canvas.transform, font, string.Empty, new Vector2(110f, -40f), new Vector2(180f, 40f), 22, TextAnchor.MiddleLeft, Color.green);
        RectTransform fpsRect = fpsText.GetComponent<RectTransform>();
        fpsRect.anchorMin = new Vector2(0f, 1f);
        fpsRect.anchorMax = new Vector2(0f, 1f);
        comboText.gameObject.SetActive(false);
        bossText.gameObject.SetActive(false);
        zoneText.gameObject.SetActive(false);
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

        Button pauseButton = CreateButton(canvas.transform, font, "| |", new Vector2(0f, 0f), new Vector2(80f, 60f));
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = new Vector2(1f, 1f);
        pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-60f, -40f);

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
    }

    private static void CreateSkylineCluster(float zPosition, float xBase, Material accentMaterial)
    {
        Material towerMaterial = CreateMaterial("TowerDark", new Color(0.04f, 0.04f, 0.08f), new Color(0f, 0f, 0f));
        for (int i = 0; i < 4; i++)
        {
            float width = 1.8f + (i * 0.55f);
            float height = 11f + (i * 4.6f);
            float x = xBase + (i * 2.8f * Mathf.Sign(xBase));
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

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.03f, 0.06f, 0.11f, 0.95f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.95f, 1f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(buttonObject.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = new Vector2(10f, -8f);
        accentRect.offsetMax = new Vector2(-10f, -2f);
        accent.GetComponent<Image>().color = label.Contains("Quit") || label.Contains("Skip")
            ? new Color(1f, 0.28f, 0.35f, 0.78f)
            : new Color(0.18f, 0.95f, 1f, 0.85f);

        CreateText(buttonObject.transform, font, label, Vector2.zero, size, 28, TextAnchor.MiddleCenter, Color.white);
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
        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
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

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.02f, 0.03f, 0.09f, 0.93f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.85f, 1f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(4f, -4f);

        GameObject topLine = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
        topLine.transform.SetParent(panel.transform, false);
        RectTransform topRect = topLine.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.offsetMin = new Vector2(14f, -10f);
        topRect.offsetMax = new Vector2(-14f, -4f);
        topLine.GetComponent<Image>().color = new Color(0.16f, 0.95f, 1f, 0.85f);
        return panel;
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
            new LevelChunkSet { Name = "Gateway", StartDistance = 0f, ChunkPrefabs = assets.GatewayChunks },
            new LevelChunkSet { Name = "Commerce", StartDistance = 850f, ChunkPrefabs = assets.CommerceChunks },
            new LevelChunkSet { Name = "Security", StartDistance = 1800f, ChunkPrefabs = assets.SecurityChunks }
        };
    }

    private static void CreateChunkLaneLights(Transform parent, Material accentMaterial)
    {
        CreateVisualPrimitive("LaneLightLeft", PrimitiveType.Cube, new Vector3(-2.5f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
        CreateVisualPrimitive("LaneLightCenter", PrimitiveType.Cube, new Vector3(0f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
        CreateVisualPrimitive("LaneLightRight", PrimitiveType.Cube, new Vector3(2.5f, 0.03f, 15f), new Vector3(0.2f, 0.02f, 30f), accentMaterial, parent);
    }

    private static void CreateChunkArchSeries(Transform parent, Material primaryAccent, Material secondaryAccent, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float z = 5f + (i * 6f);
            Material accent = i % 2 == 0 ? primaryAccent : secondaryAccent;
            CreateVisualPrimitive($"ArchLeft_{i}", PrimitiveType.Cube, new Vector3(-4.4f + (i * 0.18f), 2.1f, z), new Vector3(0.14f, 4.2f, 0.18f), accent, parent);
            CreateVisualPrimitive($"ArchRight_{i}", PrimitiveType.Cube, new Vector3(4.4f - (i * 0.18f), 2.1f, z), new Vector3(0.14f, 4.2f, 0.18f), accent, parent);
            CreateVisualPrimitive($"ArchTop_{i}", PrimitiveType.Cube, new Vector3(0f, 4.2f - (i * 0.18f), z), new Vector3(9f - (i * 0.35f), 0.14f, 0.18f), accent, parent);
        }
    }

    private static void CreateChunkBillboards(Transform parent, Material primaryAccent, Material warningAccent)
    {
        CreateVisualPrimitive("BillboardPostLeft", PrimitiveType.Cube, new Vector3(-7f, 2.2f, 9f), new Vector3(0.3f, 4.4f, 0.3f), primaryAccent, parent);
        CreateVisualPrimitive("BillboardFaceLeft", PrimitiveType.Cube, new Vector3(-7f, 4.8f, 9f), new Vector3(3.5f, 1.8f, 0.14f), CreateMaterial("ChunkHoloBoard", new Color(0.05f, 0.12f, 0.18f), primaryAccent.color), parent);
        CreateVisualPrimitive("BillboardPostRight", PrimitiveType.Cube, new Vector3(7f, 2.2f, 20f), new Vector3(0.3f, 4.4f, 0.3f), warningAccent, parent);
        CreateVisualPrimitive("BillboardFaceRight", PrimitiveType.Cube, new Vector3(7f, 4.8f, 20f), new Vector3(3.5f, 1.8f, 0.14f), CreateMaterial("ChunkWarningBoard", new Color(0.18f, 0.08f, 0.04f), warningAccent.color), parent);
    }

    private static void CreateChunkSideStructures(Transform parent, Material shellAccent, Material lineAccent, bool addSecurityPosts)
    {
        CreateVisualPrimitive("TowerLeft", PrimitiveType.Cube, new Vector3(-8f, 3f, 8f), new Vector3(2.2f, 6f, 2.8f), CreateMaterial("ChunkTowerShell", new Color(0.04f, 0.05f, 0.08f), Color.black), parent);
        CreateVisualPrimitive("TowerRight", PrimitiveType.Cube, new Vector3(8f, 3.4f, 18f), new Vector3(2.4f, 6.8f, 3f), CreateMaterial("ChunkTowerShell", new Color(0.04f, 0.05f, 0.08f), Color.black), parent);
        CreateVisualPrimitive("TowerTrimLeft", PrimitiveType.Cube, new Vector3(-6.85f, 3f, 8f), new Vector3(0.12f, 5.2f, 2.1f), shellAccent, parent);
        CreateVisualPrimitive("TowerTrimRight", PrimitiveType.Cube, new Vector3(6.75f, 3.4f, 18f), new Vector3(0.12f, 5.8f, 2.1f), lineAccent, parent);
        CreateVisualPrimitive("OverheadBridge", PrimitiveType.Cube, new Vector3(0f, 5.8f, 14f), new Vector3(12f, 0.24f, 1.5f), lineAccent, parent);
        if (!addSecurityPosts) return;
        CreateVisualPrimitive("SecurityPostLeft", PrimitiveType.Cube, new Vector3(-5.6f, 1.2f, 14f), new Vector3(0.2f, 2.4f, 0.2f), shellAccent, parent);
        CreateVisualPrimitive("SecurityPostRight", PrimitiveType.Cube, new Vector3(5.6f, 1.2f, 14f), new Vector3(0.2f, 2.4f, 0.2f), shellAccent, parent);
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
        if (baseTexture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseTexture);
            }

            material.mainTexture = baseTexture;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", albedo);
        }

        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor"))
        {
            float emissionBoost = lowerName.Contains("hero") || lowerName.Contains("visor") ? 3.1f
                : lowerName.Contains("holo") || lowerName.Contains("billboard") ? 3.6f
                : lowerName.Contains("warning") || lowerName.Contains("lane") || lowerName.Contains("laser") ? 2.8f
                : 2.2f;
            material.SetColor("_EmissionColor", emission * emissionBoost);
        }

        if (material.HasProperty("_Smoothness"))
        {
            float smoothness = lowerName.Contains("road") || lowerName.Contains("sidewalk") ? 0.86f
                : lowerName.Contains("glass") || lowerName.Contains("visor") ? 0.92f
                : lowerName.Contains("hero") || lowerName.Contains("traffic") ? 0.82f
                : 0.74f;
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            float metallic = lowerName.Contains("hero") || lowerName.Contains("metal") || lowerName.Contains("tower") || lowerName.Contains("drone") ? 0.35f : 0.18f;
            material.SetFloat("_Metallic", metallic);
        }

        EditorUtility.SetDirty(material);
        return material;
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
        Security
    }
}
