using System.Collections.Generic;
using UnityEngine;

public static class RuntimeArtFactory
{
    private const int TextureSize = 128;

    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    public static Material GetMaterial(string styleName, Color primary, Color accent, bool transparent = false)
    {
        string key = $"{styleName}|{ColorUtility.ToHtmlStringRGBA(primary)}|{ColorUtility.ToHtmlStringRGBA(accent)}|{transparent}";
        if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Shader shader = Shader.Find(transparent ? "Sprites/Default" : "Universal Render Pipeline/Lit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = $"Runtime_{styleName}"
        };

        Texture2D texture = BuildTexture(styleName, primary, accent, transparent);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        material.mainTexture = texture;

        Color tint = transparent ? Color.white : new Color(0.98f, 0.98f, 0.98f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }
        else
        {
            material.color = tint;
        }

        if (!transparent && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            float boost = styleName.ToLowerInvariant().Contains("eye") || styleName.ToLowerInvariant().Contains("core") ? 1.8f : 1.2f;
            material.SetColor("_EmissionColor", accent * boost);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", styleName.ToLowerInvariant().Contains("glass") ? 0.82f : 0.18f);
        }

        MaterialCache[key] = material;
        return material;
    }

    private static Texture2D BuildTexture(string styleName, Color primary, Color accent, bool transparent)
    {
        string lowerName = styleName.ToLowerInvariant();
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = $"Runtime_{styleName}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = x / (float)(TextureSize - 1);
                float v = y / (float)(TextureSize - 1);
                pixels[(y * TextureSize) + x] = BuildPixel(lowerName, primary, accent, u, v, transparent);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Color BuildPixel(string lowerName, Color primary, Color accent, float u, float v, bool transparent)
    {
        if (lowerName.Contains("hazard") || lowerName.Contains("telegraph") || lowerName.Contains("active"))
        {
            return BuildHazardPixel(primary, accent, u, v, transparent);
        }

        if (lowerName.Contains("coin") || lowerName.Contains("pickup"))
        {
            return BuildPickupPixel(primary, accent, u, v);
        }

        if (lowerName.Contains("boss"))
        {
            return BuildBossPixel(primary, accent, u, v);
        }

        if (lowerName.Contains("drone"))
        {
            return BuildDronePixel(primary, accent, u, v);
        }

        if (lowerName.Contains("sky") || lowerName.Contains("city") || lowerName.Contains("backdrop") || lowerName.Contains("tower"))
        {
            return BuildBackdropPixel(primary, accent, u, v);
        }

        if (lowerName.Contains("hero"))
        {
            return BuildTechPixel(primary, accent, u, v, 18f, 10f);
        }

        return BuildTechPixel(primary, accent, u, v, 12f, 6f);
    }

    private static Color BuildHazardPixel(Color primary, Color accent, float u, float v, bool transparent)
    {
        float stripe = Mathf.Abs(Mathf.Sin((u * Mathf.PI * 10f) + (v * Mathf.PI * 5f)));
        float core = Mathf.SmoothStep(0.12f, 0f, Mathf.Abs(v - 0.5f));
        float edge = Mathf.SmoothStep(0.04f, 0f, Mathf.Min(v, 1f - v));
        float alpha = transparent
            ? Mathf.Clamp01((stripe * 0.32f) + (core * 0.7f) + (edge * 0.4f))
            : 1f;
        Color color = Color.Lerp(primary * 0.65f, accent, Mathf.Clamp01((stripe * 0.45f) + core));
        color.a = alpha;
        return color;
    }

    private static Color BuildPickupPixel(Color primary, Color accent, float u, float v)
    {
        float dx = u - 0.5f;
        float dy = v - 0.5f;
        float radius = Mathf.Sqrt((dx * dx) + (dy * dy)) * 2f;
        float ring = Mathf.SmoothStep(0.92f, 0.52f, radius);
        float glint = Mathf.SmoothStep(0.16f, 0f, Mathf.Abs((u + v) - 0.82f));
        Color color = Color.Lerp(primary * 0.72f, accent, Mathf.Clamp01((ring * 0.8f) + (glint * 0.35f)));
        color.a = Mathf.Clamp01(ring + 0.1f);
        return color;
    }

    private static Color BuildBossPixel(Color primary, Color accent, float u, float v)
    {
        float ring = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)) * 0.22f;
        float scan = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 12f)) * 0.1f;
        float core = Mathf.SmoothStep(0.28f, 0f, Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.46f)));
        return Color.Lerp(primary * 0.78f, accent, Mathf.Clamp01(ring + scan + core * 0.55f));
    }

    private static Color BuildDronePixel(Color primary, Color accent, float u, float v)
    {
        float wing = Mathf.SmoothStep(0.92f, 0.98f, Mathf.Abs(u - 0.5f) * 2f) * 0.18f;
        float eye = Mathf.SmoothStep(0.16f, 0f, Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.48f)));
        float grid = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 10f)) * 0.09f;
        return Color.Lerp(primary * 0.82f, accent, Mathf.Clamp01(wing + eye * 0.75f + grid));
    }

    private static Color BuildBackdropPixel(Color primary, Color accent, float u, float v)
    {
        float vertical = Mathf.Lerp(1.08f, 0.62f, v);
        float windows = Mathf.SmoothStep(0.94f, 1f, Mathf.Abs(Mathf.Sin((u * Mathf.PI * 10f) + (v * 0.75f)))) * 0.22f;
        float haze = Mathf.PerlinNoise((u * 4f) + 3f, (v * 6f) + 8f) * 0.18f;
        return primary * vertical + (accent * (windows + haze) * 0.38f);
    }

    private static Color BuildTechPixel(Color primary, Color accent, float u, float v, float seamScale, float panelScale)
    {
        float seam = Mathf.SmoothStep(0.92f, 1f, Mathf.Abs(Mathf.Sin((u * Mathf.PI * seamScale) + (v * 2f)))) * 0.18f;
        float panel = Mathf.PerlinNoise((u * panelScale) + 4f, (v * panelScale) + 9f) * 0.18f;
        float edge = Mathf.SmoothStep(0.1f, 0f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v))) * 0.12f;
        return primary * (0.82f + panel) + (accent * 0.1f) + new Color(seam + edge, seam + edge, seam + edge, 0f);
    }
}
