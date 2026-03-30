using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        BootstrapAssets assets = CreateBootstrapAssets();
        CreateMainMenuScene();
        CreateGameScene(assets);
        CreateGameOverScene();
        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene($"{ScenesRoot}/{SceneNames.MainMenu}.unity");
        EditorUtility.DisplayDialog("Cyber Drift Runner", "Scenes and prefabs are ready. Open MainMenu and press Play.", "OK");
    }

    private static BootstrapAssets CreateBootstrapAssets()
    {
        Material neonBlue = CreateMaterial("NeonBlue", new Color(0.06f, 0.75f, 1f), new Color(0.15f, 1f, 1f));
        Material neonPink = CreateMaterial("NeonPink", new Color(1f, 0.15f, 0.7f), new Color(1f, 0.2f, 1f));
        Material neonYellow = CreateMaterial("NeonYellow", new Color(1f, 0.85f, 0.18f), new Color(1f, 0.8f, 0.2f));
        Material roadMaterial = CreateMaterial("RoadDark", new Color(0.08f, 0.08f, 0.14f), new Color(0f, 0f, 0f));

        BootstrapAssets assets = new BootstrapAssets
        {
            Projectile = CreateProjectilePrefab(neonBlue),
            Barrier = CreateBarrierPrefab(neonPink),
            Car = CreateCarPrefab(neonYellow),
            Drone = CreateDronePrefab(neonBlue),
            Credit = CreateCreditPrefab(neonYellow)
        };

        assets.Player = CreatePlayerPrefab(neonBlue, assets.Projectile);
        assets.PowerUps = new[]
        {
            CreatePowerUpPrefab("ShieldPowerUp", PrimitiveType.Capsule, neonBlue, PowerUpType.Shield, 6f),
            CreatePowerUpPrefab("DoubleScorePowerUp", PrimitiveType.Cube, neonPink, PowerUpType.DoubleScore, 8f),
            CreatePowerUpPrefab("SlowMotionPowerUp", PrimitiveType.Sphere, neonYellow, PowerUpType.SlowMotion, 5f),
            CreatePowerUpPrefab("EmpPowerUp", PrimitiveType.Cylinder, neonPink, PowerUpType.EmpBlast, 0f)
        };

        assets.RoadMaterial = roadMaterial;
        assets.AccentMaterial = neonBlue;
        return assets;
    }

    private static GameObject CreatePlayerPrefab(Material material, GameObject projectilePrefab)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Player";
        root.transform.position = Vector3.up;
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());
        root.GetComponent<Renderer>().sharedMaterial = material;

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
        root.transform.localScale = new Vector3(2.3f, 2.2f, 1.5f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        root.GetComponent<Renderer>().sharedMaterial = material;
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "Barrier.prefab");
    }

    private static GameObject CreateCarPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "CarObstacle";
        root.transform.localScale = new Vector3(2f, 1f, 4.2f);
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        root.GetComponent<Renderer>().sharedMaterial = material;
        root.AddComponent<RunnerObstacle>();
        return SavePrefab(root, "CarObstacle.prefab");
    }

    private static GameObject CreateDronePrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Drone";
        root.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        SphereCollider collider = root.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        root.GetComponent<Renderer>().sharedMaterial = material;
        root.AddComponent<EnemyDrone>();
        return SavePrefab(root, "Drone.prefab");
    }

    private static GameObject CreateCreditPrefab(Material material)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = "Credit";
        root.transform.localScale = new Vector3(0.35f, 0.1f, 0.35f);
        CapsuleCollider collider = root.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        root.GetComponent<Renderer>().sharedMaterial = material;
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

    private static void CreateGameScene(BootstrapAssets assets)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.GameScene;

        CreatePersistentSystems();
        CreateDirectionalLight();
        CreateEnvironment(assets);
        CreateEventSystem();

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(assets.Player);
        player.transform.position = new Vector3(0f, 0.2f, 0f);

        Camera camera = CreateCamera(player.transform);
        CreateHudCanvas(player.GetComponent<PlayerController>(), camera, out HoldButton holdButton);
        holdButton.Bind(player.GetComponent<PlayerController>());

        GameObject spawnerObject = new GameObject("ObstacleSpawner");
        ObstacleSpawner spawner = spawnerObject.AddComponent<ObstacleSpawner>();
        GameObject poolsRoot = new GameObject("Pools");
        poolsRoot.transform.SetParent(spawnerObject.transform, false);
        spawner.Configure(
            player.GetComponent<PlayerController>(),
            new[] { assets.Barrier, assets.Car },
            assets.Drone,
            assets.PowerUps,
            assets.Credit,
            poolsRoot.transform);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameScene}.unity");
    }

    private static void CreateMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.MainMenu;

        CreatePersistentSystems();
        CreateDirectionalLight();
        CreateStandaloneCamera(new Vector3(0f, 2f, -8f), new Vector3(15f, 0f, 0f));
        CreateEventSystem();

        GameObject menuRoot = new GameObject("MainMenuUI");
        MainMenuController controller = menuRoot.AddComponent<MainMenuController>();
        Canvas canvas = CreateCanvas("MainMenuCanvas");
        Font font = GetBuiltinFont();

        CreateText(canvas.transform, font, "Cyber Drift Runner", new Vector2(0f, 280f), new Vector2(900f, 120f), 52, TextAnchor.MiddleCenter, Color.white);
        CreateText(canvas.transform, font, "Neon courier. Hack. Dodge. Survive.", new Vector2(0f, 210f), new Vector2(700f, 60f), 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.9f, 1f));

        Button playButton = CreateButton(canvas.transform, font, "Play", new Vector2(0f, 60f), new Vector2(260f, 80f));
        Button shopButton = CreateButton(canvas.transform, font, "Skins", new Vector2(0f, -40f), new Vector2(260f, 80f));
        Button settingsButton = CreateButton(canvas.transform, font, "Settings", new Vector2(0f, -40f), new Vector2(260f, 80f));
        settingsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -140f);
        Button quitButton = CreateButton(canvas.transform, font, "Quit", new Vector2(0f, -240f), new Vector2(260f, 80f));

        GameObject shopPanel = CreatePanel(canvas.transform, "ShopPanel", new Vector2(0f, 20f), new Vector2(640f, 540f));
        Text bankText = CreateText(shopPanel.transform, font, "Bank 0", new Vector2(0f, 210f), new Vector2(240f, 40f), 28, TextAnchor.MiddleCenter, Color.cyan);
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(shopPanel.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, -35f);
        contentRect.sizeDelta = new Vector2(600f, 440f);
        SkinShopController shopController = shopPanel.AddComponent<SkinShopController>();
        shopController.Configure(shopPanel, bankText, contentRect, font);
        controller.BindShop(shopController);
        shopPanel.SetActive(false);

        GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(0f, -10f), new Vector2(420f, 260f));
        settingsPanel.SetActive(false);

        Text soundLabel = CreateText(settingsPanel.transform, font, "Sound", new Vector2(-110f, 60f), new Vector2(200f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text vibrationLabel = CreateText(settingsPanel.transform, font, "Vibration", new Vector2(-110f, 0f), new Vector2(200f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text soundValue = CreateText(settingsPanel.transform, font, "On", new Vector2(100f, 60f), new Vector2(100f, 40f), 24, TextAnchor.MiddleCenter, Color.cyan);
        Text vibrationValue = CreateText(settingsPanel.transform, font, "On", new Vector2(100f, 0f), new Vector2(100f, 40f), 24, TextAnchor.MiddleCenter, Color.cyan);
        Button soundButton = CreateButton(settingsPanel.transform, font, "Toggle", new Vector2(0f, 60f), new Vector2(120f, 40f));
        Button vibrationButton = CreateButton(settingsPanel.transform, font, "Toggle", new Vector2(0f, 0f), new Vector2(120f, 40f));
        Button closeButton = CreateButton(settingsPanel.transform, font, "Close", new Vector2(0f, -80f), new Vector2(180f, 50f));

        controller.Configure(settingsPanel, soundValue, vibrationValue);
        UnityEventTools.AddPersistentListener(playButton.onClick, controller.Play);
        UnityEventTools.AddPersistentListener(shopButton.onClick, controller.ToggleShop);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.ToggleSettings);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);
        UnityEventTools.AddPersistentListener(soundButton.onClick, controller.ToggleSound);
        UnityEventTools.AddPersistentListener(vibrationButton.onClick, controller.ToggleVibration);
        UnityEventTools.AddPersistentListener(closeButton.onClick, controller.ToggleSettings);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.MainMenu}.unity");
    }

    private static void CreateGameOverScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneNames.GameOver;

        CreatePersistentSystems();
        CreateDirectionalLight();
        CreateStandaloneCamera(new Vector3(0f, 2f, -8f), new Vector3(15f, 0f, 0f));
        CreateEventSystem();

        GameObject controllerObject = new GameObject("GameOverUI");
        GameOverController controller = controllerObject.AddComponent<GameOverController>();
        Canvas canvas = CreateCanvas("GameOverCanvas");
        Font font = GetBuiltinFont();

        CreateText(canvas.transform, font, "Run Terminated", new Vector2(0f, 240f), new Vector2(700f, 100f), 48, TextAnchor.MiddleCenter, Color.white);
        Text scoreText = CreateText(canvas.transform, font, "Score 000000", new Vector2(0f, 120f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text distanceText = CreateText(canvas.transform, font, "Distance 0m", new Vector2(0f, 70f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text creditsText = CreateText(canvas.transform, font, "Credits 0", new Vector2(0f, 20f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Text survivalText = CreateText(canvas.transform, font, "Survival 0.0s", new Vector2(0f, -30f), new Vector2(500f, 50f), 28, TextAnchor.MiddleCenter, Color.cyan);
        Button retryButton = CreateButton(canvas.transform, font, "Retry", new Vector2(0f, -130f), new Vector2(220f, 70f));
        Button menuButton = CreateButton(canvas.transform, font, "Main Menu", new Vector2(0f, -220f), new Vector2(220f, 70f));

        controller.Configure(scoreText, distanceText, creditsText, survivalText);
        UnityEventTools.AddPersistentListener(retryButton.onClick, controller.Retry);
        UnityEventTools.AddPersistentListener(menuButton.onClick, controller.BackToMenu);

        EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/{SceneNames.GameOver}.unity");
    }

    private static void CreatePersistentSystems()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
        new GameObject("MonetizationManager").AddComponent<MonetizationManager>();
        new GameObject("AudioManager").AddComponent<AudioManager>();
        new GameObject("MobilePerformanceManager").AddComponent<MobilePerformanceManager>();
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
        road.transform.localScale = new Vector3(11f, 0.5f, 10000f);
        road.GetComponent<Renderer>().sharedMaterial = assets.RoadMaterial;

        CreateLaneMarker(-3f, assets.AccentMaterial);
        CreateLaneMarker(0f, assets.AccentMaterial);
        CreateLaneMarker(3f, assets.AccentMaterial);
    }

    private static void CreateLaneMarker(float xPosition, Material material)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = $"LaneMarker_{xPosition:0}";
        marker.transform.position = new Vector3(xPosition, 0.02f, 5000f);
        marker.transform.localScale = new Vector3(0.08f, 0.04f, 10000f);
        marker.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
    }

    private static Camera CreateCamera(Transform target)
    {
        Camera camera = CreateStandaloneCamera(new Vector3(0f, 7f, -9f), new Vector3(30f, 0f, 0f));
        CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(target);
        return camera;
    }

    private static Camera CreateStandaloneCamera(Vector3 position, Vector3 rotation)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(0.02f, 0.02f, 0.08f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = position;
        cameraObject.transform.rotation = Quaternion.Euler(rotation);
        return camera;
    }

    private static void CreateHudCanvas(PlayerController player, Camera camera, out HoldButton holdButton)
    {
        Canvas canvas = CreateCanvas("HUDCanvas");
        canvas.worldCamera = camera;
        Font font = GetBuiltinFont();

        GameObject hudRoot = new GameObject("HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        HUDController hudController = hudRoot.AddComponent<HUDController>();

        Text scoreText = CreateText(canvas.transform, font, "Score 000000", new Vector2(150f, -40f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text distanceText = CreateText(canvas.transform, font, "Distance 0m", new Vector2(150f, -80f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text creditsText = CreateText(canvas.transform, font, "Credits 0", new Vector2(150f, -120f), new Vector2(280f, 40f), 24, TextAnchor.MiddleLeft, Color.white);
        Text powerUpText = CreateText(canvas.transform, font, "Ready", new Vector2(150f, -160f), new Vector2(360f, 40f), 24, TextAnchor.MiddleLeft, Color.cyan);
        hudController.Configure(scoreText, distanceText, creditsText, powerUpText);

        GameObject hackButtonObject = new GameObject("HackButton", typeof(RectTransform), typeof(Image), typeof(HoldButton));
        hackButtonObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = hackButtonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-120f, 120f);
        rect.sizeDelta = new Vector2(160f, 160f);

        Image image = hackButtonObject.GetComponent<Image>();
        image.color = new Color(0.1f, 0.8f, 1f, 0.8f);
        holdButton = hackButtonObject.GetComponent<HoldButton>();
        holdButton.Bind(player);

        CreateText(hackButtonObject.transform, font, "HACK", Vector2.zero, new Vector2(140f, 40f), 28, TextAnchor.MiddleCenter, Color.black);
        CreateReviveOverlay(canvas.transform, font);
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

    private static EventSystem CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return eventSystemObject.GetComponent<EventSystem>();
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
        image.color = new Color(0.12f, 0.85f, 1f, 0.85f);

        CreateText(buttonObject.transform, font, label, Vector2.zero, size, 28, TextAnchor.MiddleCenter, Color.black);
        return buttonObject.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, Font font, string content, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject($"{content}Text", typeof(RectTransform), typeof(Text));
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
        image.color = new Color(0.02f, 0.04f, 0.1f, 0.95f);
        return panel;
    }

    private static Material CreateMaterial(string name, Color albedo, Color emission)
    {
        string path = $"{MaterialsRoot}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = albedo;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission * 1.6f);
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
        serializedObject.FindProperty(propertyName).floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnumField(Object target, string propertyName, PowerUpType powerUpType)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).enumValueIndex = (int)powerUpType;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private sealed class BootstrapAssets
    {
        public GameObject Player;
        public GameObject Projectile;
        public GameObject Barrier;
        public GameObject Car;
        public GameObject Drone;
        public GameObject Credit;
        public GameObject[] PowerUps;
        public Material RoadMaterial;
        public Material AccentMaterial;
    }
}
