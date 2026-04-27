using UnityEngine;

public sealed class PresentationDirector : MonoBehaviour
{
    [SerializeField] private DistrictPresentationLibrary districtLibrary;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private PostProcessingConfig postProcessingConfig;
    [SerializeField] private Light keyLight;
    [SerializeField] private Light fillLight;

    private int _appliedDistrictIndex = -1;
    private bool _appliedBossState;
    private bool _loggedMissingBindings;

    private void Awake()
    {
        ResolveBindings();
        DisableLegacyFallback();
        ApplyPresentation(force: true);
    }

    private void LateUpdate()
    {
        ApplyPresentation(force: false);
    }

    private void ResolveBindings()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (postProcessingConfig == null && targetCamera != null)
        {
            postProcessingConfig = targetCamera.GetComponent<PostProcessingConfig>();
        }

        if (keyLight == null || fillLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || light.type != LightType.Directional)
                {
                    continue;
                }

                if (keyLight == null)
                {
                    keyLight = light;
                    continue;
                }

                if (fillLight == null && light != keyLight)
                {
                    fillLight = light;
                    break;
                }
            }
        }

        ValidateBindings();
    }

    private void DisableLegacyFallback()
    {
        if (targetCamera == null)
        {
            return;
        }

        RuntimeVisualStyleController legacyStyle = targetCamera.GetComponent<RuntimeVisualStyleController>();
        if (legacyStyle != null)
        {
            legacyStyle.enabled = false;
        }
    }

    private void ApplyPresentation(bool force)
    {
        if (districtLibrary == null)
        {
            LogMissingBinding("DistrictPresentationLibrary is missing.");
            return;
        }

        float distance = GameManager.Instance != null ? GameManager.Instance.Distance : 0f;
        bool bossActive = GameManager.Instance != null && GameManager.Instance.IsBossEncounterActive;
        DistrictPresentationProfile profile = districtLibrary.GetProfile(distance);
        if (profile == null)
        {
            LogMissingBinding($"No district presentation profile matches distance {distance:0.0}.");
            return;
        }

        if (!force && profile.DistrictIndex == _appliedDistrictIndex && bossActive == _appliedBossState)
        {
            return;
        }

        _appliedDistrictIndex = profile.DistrictIndex;
        _appliedBossState = bossActive;
        ApplyRenderSettings(profile, bossActive);
        ApplyLighting(profile, bossActive);
        ApplyShaderGlobals(profile);
        if (postProcessingConfig != null)
        {
            postProcessingConfig.ApplyDistrictProfile(profile, bossActive);
        }
    }

    private void ValidateBindings()
    {
        if (targetCamera == null)
        {
            LogMissingBinding("Target camera is missing.");
        }

        if (postProcessingConfig == null)
        {
            LogMissingBinding("PostProcessingConfig is missing on the target camera.");
        }

        if (keyLight == null)
        {
            LogMissingBinding("Key light is missing.");
        }

        if (fillLight == null)
        {
            LogMissingBinding("Fill light is missing.");
        }
    }

    private void LogMissingBinding(string message)
    {
        if (_loggedMissingBindings)
        {
            return;
        }

        Debug.LogError($"PresentationDirector configuration error: {message}", this);
        _loggedMissingBindings = true;
    }

    private void ApplyRenderSettings(DistrictPresentationProfile profile, bool bossActive)
    {
        if (targetCamera != null)
        {
            targetCamera.backgroundColor = bossActive
                ? Color.Lerp(profile.CameraBackground, new Color(0.08f, 0.01f, 0.02f), 0.35f)
                : profile.CameraBackground;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = profile.AmbientColor;
        RenderSettings.reflectionIntensity = bossActive ? 0.88f : 0.74f;
    }

    private void ApplyLighting(DistrictPresentationProfile profile, bool bossActive)
    {
        if (keyLight != null)
        {
            keyLight.color = bossActive ? Color.Lerp(profile.KeyLightColor, new Color(1f, 0.34f, 0.42f), 0.3f) : profile.KeyLightColor;
            keyLight.intensity = bossActive ? profile.KeyLightIntensity * 1.1f : profile.KeyLightIntensity;
        }

        if (fillLight != null)
        {
            fillLight.color = bossActive ? Color.Lerp(profile.FillLightColor, new Color(1f, 0.06f, 0.28f), 0.45f) : profile.FillLightColor;
            fillLight.intensity = bossActive ? profile.FillLightIntensity * 1.15f : profile.FillLightIntensity;
        }
    }

    private static void ApplyShaderGlobals(DistrictPresentationProfile profile)
    {
        Shader.SetGlobalColor("_CyberAccentPrimary", profile.PrimaryAccent);
        Shader.SetGlobalColor("_CyberAccentSecondary", profile.SecondaryAccent);
        Shader.SetGlobalColor("_CyberAccentTertiary", profile.TertiaryAccent);
    }
}
