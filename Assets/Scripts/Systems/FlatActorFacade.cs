using UnityEngine;

public static class FlatActorFacade
{
    public static void EnsureObstacleFacade(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.transform.Find("FlatObstacleFacade");
        if (!ShouldUseFallbackFacade(root.transform, 4))
        {
            RestoreAuthoredVisuals(root.transform, existing);
            return;
        }

        if (existing == null)
        {
            BuildObstacleFacade(root.transform, root.name.ToLowerInvariant().Contains("car"));
            existing = root.transform.Find("FlatObstacleFacade");
        }

        DisableLegacyRenderers(root.transform, existing);
    }

    public static void EnsureDroneFacade(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.transform.Find("FlatDroneFacade");
        if (!ShouldUseFallbackFacade(root.transform, 4))
        {
            RestoreAuthoredVisuals(root.transform, existing);
            return;
        }

        if (existing == null)
        {
            BuildDroneFacade(root.transform);
            existing = root.transform.Find("FlatDroneFacade");
        }

        DisableLegacyRenderers(root.transform, existing);
    }

    public static void EnsureBossFacade(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.transform.Find("FlatBossFacade");
        if (!ShouldUseFallbackFacade(root.transform, 5))
        {
            RestoreAuthoredVisuals(root.transform, existing);
            return;
        }

        if (existing == null)
        {
            BuildBossFacade(root.transform);
            existing = root.transform.Find("FlatBossFacade");
        }

        DisableLegacyRenderers(root.transform, existing);
    }

    public static void EnsurePickupFacade(GameObject root, Color primary, Color accent, bool isPowerUp)
    {
        if (root == null)
        {
            return;
        }

        Transform existing = root.transform.Find("FlatPickupFacade");
        if (!ShouldUseFallbackFacade(root.transform, 3))
        {
            RestoreAuthoredVisuals(root.transform, existing);
            return;
        }

        if (existing == null)
        {
            BuildPickupFacade(root.transform, primary, accent, isPowerUp);
            existing = root.transform.Find("FlatPickupFacade");
        }

        DisableLegacyRenderers(root.transform, existing);
    }

    public static void EnsureHazardFacade(GameObject root, out Renderer telegraph, out Renderer active)
    {
        telegraph = null;
        active = null;
        if (root == null)
        {
            return;
        }

        Transform existing = root.transform.Find("FlatHazardFacade");
        if (!ShouldUseFallbackFacade(root.transform, 2))
        {
            telegraph = FindRenderer(root.transform, "Telegraph");
            active = FindRenderer(root.transform, "ActiveHazard") ?? FindRenderer(root.transform, "Active");
            RestoreAuthoredVisuals(root.transform, existing);
            return;
        }

        if (existing == null)
        {
            BuildHazardFacade(root.transform, out telegraph, out active);
            existing = root.transform.Find("FlatHazardFacade");
        }
        else
        {
            Transform telegraphTransform = existing.Find("Telegraph");
            Transform activeTransform = existing.Find("Active");
            telegraph = telegraphTransform != null ? telegraphTransform.GetComponent<Renderer>() : null;
            active = activeTransform != null ? activeTransform.GetComponent<Renderer>() : null;
        }

        DisableLegacyRenderers(root.transform, existing);
    }

    private static bool ShouldUseFallbackFacade(Transform root, int minimumDetailedRenderers)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        int detailedRendererCount = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
            {
                continue;
            }

            if (renderer.transform.name.ToLowerInvariant().Contains("flat"))
            {
                continue;
            }

            detailedRendererCount++;
            if (detailedRendererCount >= minimumDetailedRenderers)
            {
                return false;
            }
        }

        return true;
    }

    private static void BuildObstacleFacade(Transform parent, bool isCar)
    {
        GameObject facade = new GameObject("FlatObstacleFacade");
        facade.transform.SetParent(parent, false);
        facade.transform.localPosition = new Vector3(0f, isCar ? 0.35f : 0.2f, 0.72f);

        if (isCar)
        {
            CreatePiece("Body", facade.transform, new Vector3(0f, 0f, 0f), new Vector3(1.78f, 0.72f, 0.08f), new Color(0.12f, 0.14f, 0.2f), Color.black);
            CreatePiece("Cabin", facade.transform, new Vector3(0f, 0.34f, 0.02f), new Vector3(0.9f, 0.26f, 0.03f), new Color(0.16f, 0.34f, 0.42f), new Color(0.12f, 0.58f, 0.8f) * 0.4f);
            CreatePiece("FrontBar", facade.transform, new Vector3(0f, -0.16f, 0.03f), new Vector3(1.28f, 0.06f, 0.02f), new Color(0.2f, 0.76f, 0.96f), new Color(0.2f, 0.76f, 0.96f) * 0.6f);
            CreatePiece("TailBar", facade.transform, new Vector3(0f, 0.18f, 0.03f), new Vector3(1.28f, 0.06f, 0.02f), new Color(1f, 0.35f, 0.75f), new Color(1f, 0.35f, 0.75f) * 0.55f);
        }
        else
        {
            CreatePiece("Frame", facade.transform, new Vector3(0f, 0f, 0f), new Vector3(1.94f, 1.8f, 0.08f), new Color(0.16f, 0.14f, 0.18f), Color.black);
            CreatePiece("WarningPanel", facade.transform, new Vector3(0f, 0f, 0.02f), new Vector3(1.42f, 1.28f, 0.03f), new Color(0.86f, 0.74f, 0.22f), new Color(0.8f, 0.62f, 0.16f) * 0.35f);
            CreatePiece("AccentTop", facade.transform, new Vector3(0f, 0.72f, 0.03f), new Vector3(1.62f, 0.08f, 0.02f), new Color(1f, 0.3f, 0.7f), new Color(1f, 0.3f, 0.7f) * 0.45f);
            CreatePiece("AccentBottom", facade.transform, new Vector3(0f, -0.72f, 0.03f), new Vector3(1.62f, 0.08f, 0.02f), new Color(0.14f, 0.76f, 0.94f), new Color(0.14f, 0.76f, 0.94f) * 0.45f);
            CreatePiece("Core", facade.transform, new Vector3(0f, 0f, 0.04f), new Vector3(0.86f, 0.86f, 0.02f), new Color(0.08f, 0.08f, 0.1f), Color.black);
        }
    }

    private static void BuildDroneFacade(Transform parent)
    {
        GameObject facade = new GameObject("FlatDroneFacade");
        facade.transform.SetParent(parent, false);
        facade.transform.localPosition = new Vector3(0f, 0f, 0.52f);

        CreatePiece("WingLeft", facade.transform, new Vector3(-0.56f, 0f, 0f), new Vector3(0.72f, 0.12f, 0.03f), new Color(0.1f, 0.12f, 0.16f), Color.black);
        CreatePiece("WingRight", facade.transform, new Vector3(0.56f, 0f, 0f), new Vector3(0.72f, 0.12f, 0.03f), new Color(0.1f, 0.12f, 0.16f), Color.black);
        CreatePiece("Body", facade.transform, new Vector3(0f, 0f, 0f), new Vector3(0.72f, 0.42f, 0.06f), new Color(0.14f, 0.16f, 0.2f), Color.black);
        CreatePiece("Eye", facade.transform, new Vector3(0f, 0f, 0.03f), new Vector3(0.22f, 0.08f, 0.02f), new Color(1f, 0.34f, 0.38f), new Color(1f, 0.26f, 0.32f) * 0.45f);
        CreatePiece("EngineLeft", facade.transform, new Vector3(-0.34f, -0.12f, 0.02f), new Vector3(0.12f, 0.12f, 0.02f), new Color(0.9f, 0.54f, 0.2f), new Color(0.9f, 0.54f, 0.2f) * 0.3f);
        CreatePiece("EngineRight", facade.transform, new Vector3(0.34f, -0.12f, 0.02f), new Vector3(0.12f, 0.12f, 0.02f), new Color(0.9f, 0.54f, 0.2f), new Color(0.9f, 0.54f, 0.2f) * 0.3f);
    }

    private static void BuildBossFacade(Transform parent)
    {
        GameObject facade = new GameObject("FlatBossFacade");
        facade.transform.SetParent(parent, false);
        facade.transform.localPosition = new Vector3(0f, 0f, 1.2f);

        CreatePiece("Hull", facade.transform, new Vector3(0f, 0f, 0f), new Vector3(2.6f, 1.2f, 0.08f), new Color(0.12f, 0.12f, 0.16f), Color.black);
        CreatePiece("Ring", facade.transform, new Vector3(0f, -0.08f, -0.02f), new Vector3(3.1f, 1.46f, 0.02f), new Color(0.18f, 0.28f, 0.38f), new Color(0.2f, 0.74f, 0.92f) * 0.28f);
        CreatePiece("Core", facade.transform, new Vector3(0f, -0.02f, 0.03f), new Vector3(0.9f, 0.7f, 0.02f), new Color(1f, 0.28f, 0.34f), new Color(1f, 0.24f, 0.32f) * 0.32f);
        CreatePiece("Eye", facade.transform, new Vector3(0f, 0.22f, 0.04f), new Vector3(1.2f, 0.12f, 0.02f), new Color(0.18f, 0.84f, 0.96f), new Color(0.18f, 0.84f, 0.96f) * 0.28f);
        CreatePiece("WingLeft", facade.transform, new Vector3(-1.95f, 0f, 0f), new Vector3(1.5f, 0.16f, 0.03f), new Color(0.16f, 0.18f, 0.24f), Color.black);
        CreatePiece("WingRight", facade.transform, new Vector3(1.95f, 0f, 0f), new Vector3(1.5f, 0.16f, 0.03f), new Color(0.16f, 0.18f, 0.24f), Color.black);
    }

    private static void BuildPickupFacade(Transform parent, Color primary, Color accent, bool isPowerUp)
    {
        GameObject facade = new GameObject("FlatPickupFacade");
        facade.transform.SetParent(parent, false);
        facade.transform.localPosition = new Vector3(0f, 0f, 0.28f);

        if (isPowerUp)
        {
            CreatePiece("Aura", facade.transform, Vector3.zero, new Vector3(0.9f, 0.9f, 0.02f), primary * 0.6f, accent * 0.22f);
            CreatePiece("Core", facade.transform, Vector3.zero, new Vector3(0.48f, 0.48f, 0.04f), primary, accent * 0.28f);
            CreatePiece("GlyphVertical", facade.transform, Vector3.zero, new Vector3(0.1f, 0.56f, 0.01f), accent, accent * 0.18f);
            CreatePiece("GlyphHorizontal", facade.transform, Vector3.zero, new Vector3(0.56f, 0.1f, 0.01f), accent, accent * 0.18f);
        }
        else
        {
            CreatePiece("CoinOuter", facade.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 0.03f), primary, accent * 0.15f);
            CreatePiece("CoinInner", facade.transform, new Vector3(0f, 0f, 0.01f), new Vector3(0.42f, 0.42f, 0.02f), accent, accent * 0.1f);
            CreatePiece("CoinMark", facade.transform, new Vector3(0f, 0f, 0.02f), new Vector3(0.14f, 0.32f, 0.01f), new Color(0.2f, 0.16f, 0.06f), Color.black);
        }
    }

    private static void BuildHazardFacade(Transform parent, out Renderer telegraph, out Renderer active)
    {
        GameObject facade = new GameObject("FlatHazardFacade");
        facade.transform.SetParent(parent, false);
        facade.transform.localPosition = new Vector3(0f, 0.02f, 0f);

        GameObject telegraphObject = CreatePiece("Telegraph", facade.transform, new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0.26f, 0.02f), new Color(0.98f, 0.82f, 0.2f, 0.55f), new Color(0.72f, 0.28f, 0.14f, 0f));
        GameObject activeObject = CreatePiece("Active", facade.transform, new Vector3(0f, 0f, 0.02f), new Vector3(2.2f, 0.32f, 0.02f), new Color(1f, 0.28f, 0.4f, 0.9f), new Color(1f, 0.2f, 0.34f) * 0.28f);
        telegraph = telegraphObject.GetComponent<Renderer>();
        active = activeObject.GetComponent<Renderer>();
    }

    private static void DisableLegacyRenderers(Transform root, Transform facadeRoot)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.transform.IsChildOf(facadeRoot) || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
            {
                renderer.enabled = true;
                continue;
            }

            renderer.enabled = false;
        }
    }

    private static void RestoreAuthoredVisuals(Transform root, Transform facadeRoot)
    {
        if (facadeRoot != null)
        {
            facadeRoot.gameObject.SetActive(false);
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
            {
                continue;
            }

            if (facadeRoot != null && renderer.transform.IsChildOf(facadeRoot))
            {
                renderer.enabled = false;
                continue;
            }

            renderer.enabled = true;
        }
    }

    private static Renderer FindRenderer(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        return child != null ? child.GetComponent<Renderer>() : null;
    }

    private static GameObject CreatePiece(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color albedo, Color emission)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = localScale;
        Object.Destroy(piece.GetComponent<BoxCollider>());

        Renderer renderer = piece.GetComponent<Renderer>();
        bool transparent = albedo.a < 0.99f || emission.a < 0.99f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.sharedMaterial = RuntimeArtFactory.GetMaterial($"{parent.name}_{name}", albedo, emission, transparent);
        return piece;
    }
}
