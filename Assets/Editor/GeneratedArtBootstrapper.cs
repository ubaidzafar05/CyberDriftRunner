using System.IO;
using UnityEditor;
using UnityEngine;

public static class GeneratedArtBootstrapper
{
    private const string ArtRoot = "Assets/Art";
    private const string TexturesRoot = "Assets/Art/Textures";
    private const string GeneratedRoot = "Assets/Art/Textures/Generated";
    private const int TextureSize = 256;

    public static Texture2D EnsureTexture(string materialName, Color primary, Color secondary)
    {
        EnsureFolders();
        string fileName = Sanitize(materialName) + ".png";
        string assetPath = $"{GeneratedRoot}/{fileName}";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture != null)
        {
            return texture;
        }

        Texture2D generated = BuildTexture(materialName, primary, secondary);
        File.WriteAllBytes(assetPath, generated.EncodeToPNG());
        Object.DestroyImmediate(generated);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(assetPath, materialName);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static Texture2D BuildTexture(string materialName, Color primary, Color secondary)
    {
        string lowerName = materialName.ToLowerInvariant();
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = materialName
        };

        Color[] pixels = new Color[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = x / (float)(TextureSize - 1);
                float v = y / (float)(TextureSize - 1);
                Color color = SelectPixelBuilder(lowerName, primary, secondary, u, v);
                pixels[(y * TextureSize) + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Color SelectPixelBuilder(string lowerName, Color primary, Color secondary, float u, float v)
    {
        if (lowerName.Contains("road"))
        {
            return BuildRoadPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("glass") || lowerName.Contains("visor"))
        {
            return BuildGlassPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("warning") || lowerName.Contains("stripe"))
        {
            return BuildWarningPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("hero") || lowerName.Contains("drone"))
        {
            return BuildHeroTechPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("tower") || lowerName.Contains("backdrop") || lowerName.Contains("spire"))
        {
            return BuildTowerPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("holo") || lowerName.Contains("billboard"))
        {
            return BuildHologramPixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("lane") || lowerName.Contains("neon") || lowerName.Contains("core"))
        {
            return BuildEmissivePixel(primary, secondary, u, v);
        }

        if (lowerName.Contains("sidewalk"))
        {
            return BuildSidewalkPixel(primary, secondary, u, v);
        }

        return BuildMetalPixel(primary, secondary, u, v);
    }

    private static Color BuildRoadPixel(Color primary, Color secondary, float u, float v)
    {
        float rainNoise = Mathf.PerlinNoise((u * 10f) + 3f, (v * 18f) + 9f);
        float streaks = Mathf.Abs(Mathf.Sin((v * Mathf.PI * 18f) + (u * 2.3f)));
        float wornLines = Mathf.SmoothStep(0.7f, 1f, Mathf.Abs(Mathf.Sin(v * Mathf.PI * 32f)));
        Color wetBase = Color.Lerp(primary * 0.72f, primary * 1.04f, rainNoise * 0.35f);
        Color reflection = Color.Lerp(wetBase, secondary * 0.38f + primary * 0.55f, streaks * 0.08f);
        return Color.Lerp(reflection, secondary * 0.22f + primary, wornLines * 0.12f);
    }

    private static Color BuildGlassPixel(Color primary, Color secondary, float u, float v)
    {
        float verticalGradient = Mathf.Lerp(1.1f, 0.55f, v);
        float scanline = Mathf.Abs(Mathf.Sin((u * 20f) + (v * 4f))) * 0.18f;
        float hotspot = Mathf.SmoothStep(0.45f, 1f, 1f - Mathf.Abs((u - 0.5f) * 1.8f));
        Color baseColor = primary * verticalGradient;
        Color litColor = secondary * (0.35f + hotspot * 0.4f);
        return Color.Lerp(baseColor, litColor, scanline + hotspot * 0.15f);
    }

    private static Color BuildWarningPixel(Color primary, Color secondary, float u, float v)
    {
        float chevron = Mathf.Repeat((u * 3f) + (v * 2f), 1f);
        float mask = chevron > 0.5f ? 1f : 0f;
        float grime = Mathf.PerlinNoise(u * 6f, v * 14f) * 0.12f;
        return Color.Lerp(primary * (0.85f + grime), secondary, mask * 0.85f);
    }

    private static Color BuildHologramPixel(Color primary, Color secondary, float u, float v)
    {
        float column = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 10f));
        float rows = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 18f));
        float glitch = Mathf.PerlinNoise((u * 24f) + 7f, (v * 8f) + 11f);
        float mask = Mathf.Clamp01((column * 0.45f) + (rows * 0.25f) + (glitch * 0.5f));
        return Color.Lerp(primary * 0.5f, secondary, mask);
    }

    private static Color BuildHeroTechPixel(Color primary, Color secondary, float u, float v)
    {
        float seam = Mathf.SmoothStep(0.92f, 1f, Mathf.Abs(Mathf.Sin((u * Mathf.PI * 6f) + (v * 1.5f)))) * 0.32f;
        float panel = Mathf.SmoothStep(0.78f, 1f, Mathf.Abs(Mathf.Sin(v * Mathf.PI * 14f))) * 0.12f;
        float circuit = Mathf.PerlinNoise((u * 26f) + 4f, (v * 26f) + 8f) * 0.22f;
        return primary * (0.8f + circuit) + (secondary * 0.08f) + new Color(seam + panel, seam + panel, seam + panel, 0f);
    }

    private static Color BuildEmissivePixel(Color primary, Color secondary, float u, float v)
    {
        float bars = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 8f));
        float grid = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 14f)) * 0.35f;
        float pulse = Mathf.PerlinNoise((u * 4f) + 2f, (v * 18f) + 5f) * 0.3f;
        float mask = Mathf.Clamp01((bars * 0.55f) + grid + pulse);
        return Color.Lerp(primary * 0.48f, secondary, mask);
    }

    private static Color BuildTowerPixel(Color primary, Color secondary, float u, float v)
    {
        float windowBand = Mathf.SmoothStep(0.95f, 1f, Mathf.Abs(Mathf.Sin(v * Mathf.PI * 18f))) * 0.24f;
        float columns = Mathf.SmoothStep(0.88f, 1f, Mathf.Abs(Mathf.Sin(u * Mathf.PI * 9f))) * 0.16f;
        float grime = Mathf.PerlinNoise((u * 10f) + 9f, (v * 14f) + 7f) * 0.12f;
        return primary * (0.74f + grime) + (secondary * (windowBand + columns) * 0.4f);
    }

    private static Color BuildSidewalkPixel(Color primary, Color secondary, float u, float v)
    {
        float tile = Mathf.SmoothStep(0.9f, 1f, Mathf.Abs(Mathf.Sin(u * Mathf.PI * 12f))) * 0.12f;
        float crack = Mathf.PerlinNoise((u * 18f) + 5f, (v * 18f) + 2f) * 0.18f;
        return primary * (0.9f + crack) + (secondary * 0.05f) + new Color(tile, tile, tile, 0f);
    }

    private static Color BuildMetalPixel(Color primary, Color secondary, float u, float v)
    {
        float brushed = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 26f)) * 0.08f;
        float panels = Mathf.SmoothStep(0.92f, 1f, Mathf.Abs(Mathf.Sin(u * Mathf.PI * 5f))) * 0.14f;
        float grime = Mathf.PerlinNoise((u * 8f) + 13f, (v * 8f) + 3f) * 0.18f;
        return primary * (0.78f + grime) + (secondary * 0.04f) + new Color(brushed + panels, brushed + panels, brushed + panels, 0f);
    }

    private static void ConfigureImporter(string assetPath, string materialName)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = materialName.ToLowerInvariant().Contains("billboard") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.anisoLevel = 2;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(' ', '_');
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ArtRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Art");
        }

        if (!AssetDatabase.IsValidFolder(TexturesRoot))
        {
            AssetDatabase.CreateFolder(ArtRoot, "Textures");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedRoot))
        {
            AssetDatabase.CreateFolder(TexturesRoot, "Generated");
        }
    }
}
