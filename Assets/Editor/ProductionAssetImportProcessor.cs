using UnityEditor;
using UnityEngine;

public sealed class ProductionAssetImportProcessor : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Art/";
    private const string AudioRoot = "Assets/Audio/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.mipmapEnabled = !assetPath.Contains("/UI/");
        importer.alphaIsTransparency = assetPath.Contains("/UI/") || assetPath.Contains("/VFX/");
        importer.textureType = assetPath.Contains("/UI/") ? TextureImporterType.Sprite : TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.maxTextureSize = assetPath.Contains("/Characters/") ? 2048 : 1024;
        ApplyTexturePlatform(importer, "Android");
        ApplyTexturePlatform(importer, "iPhone");
    }

    private static void ApplyTexturePlatform(TextureImporter importer, string platformName)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        settings.overridden = true;
        settings.maxTextureSize = importer.maxTextureSize;
        settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        settings.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(settings);
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ArtRoot))
        {
            return;
        }

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.importCameras = false;
        importer.importLights = false;
        importer.meshCompression = ModelImporterMeshCompression.Medium;
        importer.isReadable = false;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(AudioRoot))
        {
            return;
        }

        AudioImporter importer = (AudioImporter)assetImporter;
        importer.loadInBackground = true;
        importer.forceToMono = assetPath.Contains("/SFX/");

        AudioImporterSampleSettings settings = BuildAudioSettings(assetPath.Contains("/Music/"));
        importer.defaultSampleSettings = settings;
        ApplyAudioPlatform(importer, "Android", settings);
        ApplyAudioPlatform(importer, "iOS", settings);
        ApplyAudioPlatform(importer, "Standalone", settings);
    }

    private static AudioImporterSampleSettings BuildAudioSettings(bool isMusic)
    {
        AudioImporterSampleSettings settings = new AudioImporterSampleSettings
        {
            loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad,
            compressionFormat = AudioCompressionFormat.Vorbis,
            quality = isMusic ? 0.62f : 0.7f,
            sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
            preloadAudioData = !isMusic
        };
        return settings;
    }

    private static void ApplyAudioPlatform(AudioImporter importer, string platformName, AudioImporterSampleSettings settings)
    {
        importer.SetOverrideSampleSettings(platformName, settings);
    }
}
