using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RenderPipelineBootstrapper
{
    private const string SettingsRoot = "Assets/Settings";
    private const string RenderingRoot = "Assets/Settings/Rendering";
    private const string RendererPath = "Assets/Settings/Rendering/CyberDriftRunner_Renderer.asset";
    private const string PipelinePath = "Assets/Settings/Rendering/CyberDriftRunner_URP.asset";

    public static UniversalRenderPipelineAsset EnsurePipelineAsset()
    {
        EnsureFolders();

        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, RendererPath);
        }

        UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (pipelineAsset == null)
        {
            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
        }

        ConfigurePipelineAsset(pipelineAsset);
        GraphicsSettings.defaultRenderPipeline = pipelineAsset;

        int previousQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }
        QualitySettings.SetQualityLevel(previousQuality, false);
        QualitySettings.renderPipeline = pipelineAsset;

        AssetDatabase.SaveAssets();
        return pipelineAsset;
    }

    private static void ConfigurePipelineAsset(UniversalRenderPipelineAsset pipelineAsset)
    {
        SerializedObject serializedObject = new SerializedObject(pipelineAsset);
        SetFloat(serializedObject, "m_RenderScale", 1f);
        SetInt(serializedObject, "m_MSAA", 2);
        SetBool(serializedObject, "m_RequiresDepthTextureOption", true);
        SetBool(serializedObject, "m_RequiresOpaqueTextureOption", false);
        SetBool(serializedObject, "m_SupportsHDR", true);
        SetBool(serializedObject, "m_MainLightShadowsSupported", true);
        SetBool(serializedObject, "m_AdditionalLights", true);
        SetBool(serializedObject, "m_AdditionalLightShadowsSupported", true);
        SetFloat(serializedObject, "m_ShadowDistance", 55f);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipelineAsset);
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(SettingsRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        if (!AssetDatabase.IsValidFolder(RenderingRoot))
        {
            AssetDatabase.CreateFolder(SettingsRoot, "Rendering");
        }
    }
}
