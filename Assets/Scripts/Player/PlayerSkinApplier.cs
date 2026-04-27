using UnityEngine;

public sealed class PlayerSkinApplier : MonoBehaviour
{
    private const string StylizedRigName = "StylizedRunnerRig";

    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Renderer primaryRenderer;

    public Renderer PrimaryRenderer => primaryRenderer;

    private void Awake()
    {
        EnsureRunnerPresentation();
        CacheTargetRenderers();
    }

    private void Start()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        ApplySkin(ProgressionManager.Instance.GetSelectedSkin());
    }

    public void ApplySkin(SkinDefinition skin)
    {
        if (skin == null || targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            ApplyRendererStyle(targetRenderers[i], skin);
        }
    }

    private void EnsureRunnerPresentation()
    {
        if (HasAuthoredPresentation())
        {
            RestoreAuthoredPresentation();
            return;
        }

        Transform existingRig = transform.Find(StylizedRigName);
        if (existingRig == null)
        {
            existingRig = BuildStylizedRunnerRig().transform;
        }

        DisablePlaceholderVisuals(existingRig);
    }

    private void CacheTargetRenderers()
    {
        if (HasAuthoredPresentation())
        {
            targetRenderers = CollectAuthoredRenderers();
            primaryRenderer = FindPreferredRenderer(targetRenderers, "TorsoPlate", "ChestCore", "HipRig");
            return;
        }

        Transform rig = transform.Find(StylizedRigName);
        if (rig == null)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
            primaryRenderer = targetRenderers != null && targetRenderers.Length > 0 ? targetRenderers[0] : null;
            return;
        }

        targetRenderers = rig.GetComponentsInChildren<Renderer>(true);
        Transform torso = rig.Find("Torso");
        primaryRenderer = torso != null ? torso.GetComponent<Renderer>() : (targetRenderers.Length > 0 ? targetRenderers[0] : null);
    }

    private bool HasAuthoredPresentation()
    {
        if (transform.Find("TorsoPlate") != null || transform.Find("ChestCore") != null)
        {
            return true;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Transform stylizedRig = transform.Find(StylizedRigName);
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.transform == transform)
            {
                continue;
            }

            if (renderer is TrailRenderer || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (stylizedRig != null && renderer.transform.IsChildOf(stylizedRig))
            {
                continue;
            }

            count++;
        }

        return count >= 8;
    }

    private void RestoreAuthoredPresentation()
    {
        Transform stylizedRig = transform.Find(StylizedRigName);
        if (stylizedRig != null)
        {
            stylizedRig.gameObject.SetActive(false);
        }

        Transform projectilePool = transform.Find("ProjectilePool");
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.transform == transform)
            {
                renderer.enabled = false;
                continue;
            }

            if (projectilePool != null && renderer.transform.IsChildOf(projectilePool))
            {
                continue;
            }

            if (renderer is TrailRenderer || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderer.enabled = true;
        }
    }

    private GameObject BuildStylizedRunnerRig()
    {
        GameObject rig = new GameObject(StylizedRigName);
        rig.transform.SetParent(transform, false);
        rig.transform.localPosition = new Vector3(0f, 0.88f, 0.2f);

        CreateFlatPiece("Torso", rig.transform, new Vector3(0f, -0.02f, 0f), new Vector3(0.62f, 0.9f, 0.22f));
        CreateFlatPiece("ChestCore", rig.transform, new Vector3(0f, 0.08f, 0.12f), new Vector3(0.2f, 0.3f, 0.05f));
        CreateFlatPiece("Head", rig.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.36f, 0.36f, 0.22f));
        CreateFlatPiece("VisorBand", rig.transform, new Vector3(0f, 0.7f, 0.13f), new Vector3(0.28f, 0.09f, 0.05f));
        CreateFlatPiece("ShoulderLeft", rig.transform, new Vector3(-0.34f, 0.28f, 0.01f), new Vector3(0.16f, 0.2f, 0.18f));
        CreateFlatPiece("ShoulderRight", rig.transform, new Vector3(0.34f, 0.28f, 0.01f), new Vector3(0.16f, 0.2f, 0.18f));
        CreateFlatPiece("HipRig", rig.transform, new Vector3(0f, -0.5f, 0f), new Vector3(0.5f, 0.18f, 0.18f));
        CreateFlatPiece("ArmBladeLeft", rig.transform, new Vector3(-0.42f, -0.06f, 0.02f), new Vector3(0.12f, 0.64f, 0.14f));
        CreateFlatPiece("ArmBladeRight", rig.transform, new Vector3(0.42f, -0.06f, 0.02f), new Vector3(0.12f, 0.64f, 0.14f));
        CreateFlatPiece("LegLeft", rig.transform, new Vector3(-0.12f, -0.96f, 0f), new Vector3(0.14f, 0.8f, 0.16f));
        CreateFlatPiece("LegRight", rig.transform, new Vector3(0.12f, -0.96f, 0f), new Vector3(0.14f, 0.8f, 0.16f));
        CreateFlatPiece("BootLeft", rig.transform, new Vector3(-0.14f, -1.38f, 0.08f), new Vector3(0.22f, 0.1f, 0.26f));
        CreateFlatPiece("BootRight", rig.transform, new Vector3(0.14f, -1.38f, 0.08f), new Vector3(0.22f, 0.1f, 0.26f));
        CreateFlatPiece("CoatTailLeft", rig.transform, new Vector3(-0.18f, -0.18f, -0.12f), new Vector3(0.16f, 0.66f, 0.05f));
        CreateFlatPiece("CoatTailRight", rig.transform, new Vector3(0.18f, -0.18f, -0.12f), new Vector3(0.16f, 0.66f, 0.05f));
        CreateFlatPiece("SpineLight", rig.transform, new Vector3(0f, 0.12f, -0.11f), new Vector3(0.08f, 0.82f, 0.04f));
        return rig;
    }

    private void DisablePlaceholderVisuals(Transform stylizedRig)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Transform projectilePool = transform.Find("ProjectilePool");
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (renderer.transform.IsChildOf(stylizedRig))
            {
                renderer.enabled = true;
                continue;
            }

            if (projectilePool != null && renderer.transform.IsChildOf(projectilePool))
            {
                continue;
            }

            if (renderer is TrailRenderer || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            renderer.enabled = false;
        }

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }
    }

    private static GameObject CreateFlatPiece(string name, Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = localScale;
        Object.Destroy(piece.GetComponent<BoxCollider>());
        return piece;
    }

    private Renderer[] CollectAuthoredRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> filtered = new System.Collections.Generic.List<Renderer>(renderers.Length);
        Transform stylizedRig = transform.Find(StylizedRigName);
        Transform projectilePool = transform.Find("ProjectilePool");
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.transform == transform)
            {
                continue;
            }

            if (stylizedRig != null && renderer.transform.IsChildOf(stylizedRig))
            {
                continue;
            }

            if (projectilePool != null && renderer.transform.IsChildOf(projectilePool))
            {
                continue;
            }

            if (renderer is TrailRenderer || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            filtered.Add(renderer);
        }

        return filtered.ToArray();
    }

    private static Renderer FindPreferredRenderer(Renderer[] renderers, params string[] preferredNames)
    {
        for (int i = 0; i < preferredNames.Length; i++)
        {
            for (int j = 0; j < renderers.Length; j++)
            {
                if (renderers[j] != null && renderers[j].name == preferredNames[i])
                {
                    return renderers[j];
                }
            }
        }

        return renderers.Length > 0 ? renderers[0] : null;
    }

    private static void ApplyRendererStyle(Renderer renderer, SkinDefinition skin)
    {
        if (renderer == null)
        {
            return;
        }

        string lowerName = renderer.name.ToLowerInvariant();
        Material material = renderer.material;

        Color baseColor = skin.BaseColor;
        Color accentColor = skin.EmissionColor;
        Color albedo = baseColor;
        Color emission = accentColor * 1.2f;

        if (lowerName.Contains("visor"))
        {
            albedo = Color.Lerp(baseColor, accentColor, 0.55f);
            emission = accentColor * 1.8f;
        }
        else if (lowerName.Contains("glow") || lowerName.Contains("core") || lowerName.Contains("light"))
        {
            albedo = Color.Lerp(baseColor * 0.55f, accentColor, 0.72f);
            emission = accentColor * 2f;
        }
        else if (lowerName.Contains("trim") || lowerName.Contains("blade") || lowerName.Contains("collar") || lowerName.Contains("boot"))
        {
            albedo = Color.Lerp(baseColor, accentColor, 0.35f);
            emission = accentColor * 1.35f;
        }
        else if (lowerName.Contains("mantle") || lowerName.Contains("banner") || lowerName.Contains("thruster") || lowerName.Contains("signal") || lowerName.Contains("scarf"))
        {
            albedo = Color.Lerp(baseColor * 0.72f, accentColor, 0.62f);
            emission = accentColor * 2.15f;
        }
        else if (lowerName.Contains("tail"))
        {
            albedo = Color.Lerp(baseColor, accentColor, 0.22f) * 0.92f;
            emission = accentColor * 1.1f;
        }
        else if (lowerName.Contains("hip") || lowerName.Contains("torso") || lowerName.Contains("head") || lowerName.Contains("leg") || lowerName.Contains("arm"))
        {
            albedo = baseColor * 0.9f;
            emission = accentColor * 0.9f;
        }

        material.color = albedo;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", albedo);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }
}
