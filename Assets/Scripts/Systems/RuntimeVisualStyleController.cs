using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class RuntimeVisualStyleController : MonoBehaviour
{
    [SerializeField] private bool flattenPalette = true;
    [SerializeField] private float emissionScale = 0.78f;
    [SerializeField] private float saturationScale = 0.92f;
    [SerializeField] private float valueScale = 0.94f;
    [SerializeField] private Color backgroundColor = new Color(0.008f, 0.012f, 0.04f);
    [SerializeField] private bool hideWorldClutter;

    private bool _applied;

    private void Start()
    {
        ApplyStyleOnce();
    }

    public void ApplyStyleOnce()
    {
        if (_applied)
        {
            return;
        }

        _applied = true;
        Camera camera = GetComponent<Camera>();
        if (camera != null)
        {
            camera.backgroundColor = backgroundColor;
        }

        RenderSettings.ambientIntensity = 0.96f;
        RenderSettings.reflectionIntensity = 0.74f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.02f, 0.026f, 0.076f);
        RenderSettings.fogStartDistance = 16f;
        RenderSettings.fogEndDistance = 108f;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                lights[i].intensity = 1.08f;
                lights[i].color = new Color(0.66f, 0.82f, 1f);
            }
        }

        if (hideWorldClutter)
        {
            SimplifySceneClutter();
        }

        if (!flattenPalette)
        {
            return;
        }

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        HashSet<Material> touched = new HashSet<Material>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
            {
                continue;
            }

            Material[] materials = renderer.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null || !touched.Add(material))
                {
                    continue;
                }

                ToneMaterial(material);
            }
        }
    }

    private static void SimplifySceneClutter()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null)
            {
                continue;
            }

            string lowerName = current.name.ToLowerInvariant();
            bool hide =
                lowerName.Contains("runtime2dbackdrop");

            if (hide)
            {
                current.gameObject.SetActive(false);
            }
        }
    }

    private void ToneMaterial(Material material)
    {
        string lowerName = material.name.ToLowerInvariant();

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", ToneColor(material.GetColor("_BaseColor"), lowerName));
        }
        else
        {
            material.color = ToneColor(material.color, lowerName);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            Color emission = ToneEmission(material.GetColor("_EmissionColor"), lowerName);
            material.SetColor("_EmissionColor", emission);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", lowerName.Contains("road") ? 0.2f : 0.35f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", lowerName.Contains("visor") ? 0.1f : 0f);
        }
    }

    private Color ToneColor(Color source, string lowerName)
    {
        Color.RGBToHSV(source, out float h, out float s, out float v);
        s *= saturationScale;
        v *= valueScale;

        if (lowerName.Contains("road") || lowerName.Contains("tower") || lowerName.Contains("shell") || lowerName.Contains("metal"))
        {
            s *= 0.8f;
            v *= 0.86f;
        }

        if (lowerName.Contains("warning") || lowerName.Contains("gold"))
        {
            s = Mathf.Min(0.88f, s * 1.05f);
            v = Mathf.Min(0.98f, v * 1.04f);
        }

        return Color.HSVToRGB(h, Mathf.Clamp01(s), Mathf.Clamp01(v));
    }

    private Color ToneEmission(Color source, string lowerName)
    {
        Color toned = ToneColor(source, lowerName) * emissionScale;
        if (lowerName.Contains("visor") || lowerName.Contains("eye"))
        {
            toned *= 1.25f;
        }

        if (lowerName.Contains("lane"))
        {
            toned *= 0.85f;
        }

        return toned;
    }
}
