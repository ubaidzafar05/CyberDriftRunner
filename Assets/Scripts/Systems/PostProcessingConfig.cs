using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public sealed class PostProcessingConfig : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private VolumeProfile baseProfile;
    [SerializeField] private bool cloneProfileAtRuntime = true;
    [SerializeField] private VisualQualityConfig qualityConfig;

    [Header("Bloom")]
    [SerializeField] private float bloomIntensity = 2.7f;
    [SerializeField] private float bloomThreshold = 0.7f;
    [SerializeField] private float bloomScatter = 0.84f;
    [SerializeField] private Color bloomTint = new Color(0.62f, 0.88f, 1f);

    [Header("Vignette")]
    [SerializeField] private float vignetteIntensity = 0.32f;
    [SerializeField] private Color vignetteColor = new Color(0.04f, 0f, 0.14f);

    [Header("Chromatic Aberration")]
    [SerializeField] private float chromaticIntensity = 0.045f;

    [Header("Color Adjustments")]
    [SerializeField] private float contrast = 24f;
    [SerializeField] private float saturation = 8f;
    [SerializeField] private float postExposure = 0.08f;

    [Header("Atmosphere")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogColor = new Color(0.018f, 0.024f, 0.076f);
    [SerializeField] private float fogStartDistance = 16f;
    [SerializeField] private float fogEndDistance = 118f;

    private Volume _volume;
    private VolumeProfile _runtimeProfile;
    private Bloom _bloom;
    private Vignette _vignette;
    private ChromaticAberration _chromaticAberration;
    private ColorAdjustments _colorAdjustments;
    private Tonemapping _tonemapping;
    private DistrictPresentationProfile _activeProfile;
    private bool _bossPresentation;

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        if (_volume == null)
        {
            _volume = gameObject.AddComponent<Volume>();
        }

        _volume.isGlobal = true;
        _volume.priority = 10f;
        _runtimeProfile = ResolveProfile();
        _volume.profile = _runtimeProfile;
        EnsureOverrides(_runtimeProfile);
        ApplyQuality(SettingsManager.Instance != null ? SettingsManager.Instance.QualityLevel : 1);
        ApplyAtmosphere();
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
        }
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
        }
    }

    private VolumeProfile ResolveProfile()
    {
        if (baseProfile == null)
        {
            return ScriptableObject.CreateInstance<VolumeProfile>();
        }

        return cloneProfileAtRuntime ? Instantiate(baseProfile) : baseProfile;
    }

    private void EnsureOverrides(VolumeProfile profile)
    {
        _bloom = profile.TryGet(out Bloom bloom) ? bloom : profile.Add<Bloom>(true);
        _vignette = profile.TryGet(out Vignette vignette) ? vignette : profile.Add<Vignette>(true);
        _chromaticAberration = profile.TryGet(out ChromaticAberration ca) ? ca : profile.Add<ChromaticAberration>(true);
        _colorAdjustments = profile.TryGet(out ColorAdjustments adjustments) ? adjustments : profile.Add<ColorAdjustments>(true);
        _tonemapping = profile.TryGet(out Tonemapping tonemapping) ? tonemapping : profile.Add<Tonemapping>(true);
        _tonemapping.mode.Override(TonemappingMode.ACES);
    }

    private void HandleSettingsChanged()
    {
        ApplyQuality(SettingsManager.Instance != null ? SettingsManager.Instance.QualityLevel : 1);
        ApplyAtmosphere();
    }

    public void ApplyDistrictProfile(DistrictPresentationProfile profile, bool bossPresentation)
    {
        _activeProfile = profile;
        _bossPresentation = bossPresentation;
        ApplyQuality(SettingsManager.Instance != null ? SettingsManager.Instance.QualityLevel : 1);
        ApplyAtmosphere();
    }

    private void ApplyQuality(int qualityLevel)
    {
        VisualQualityConfig.Tier tier = qualityConfig != null ? qualityConfig.GetTier(qualityLevel) : null;
        float effectScale = tier != null ? Mathf.Clamp01(tier.postProcessScale) : qualityLevel == 0 ? 0f : qualityLevel == 1 ? 0.65f : 1f;
        float bloomMultiplier = _activeProfile != null ? _activeProfile.BloomIntensityMultiplier : 1f;
        float vignetteMultiplier = _activeProfile != null ? _activeProfile.VignetteIntensityMultiplier : 1f;
        float chromaticBoost = _activeProfile != null ? _activeProfile.ChromaticBoost : 0f;
        float contrastBias = _activeProfile != null ? _activeProfile.ContrastBias : 0f;
        float saturationBias = _activeProfile != null ? _activeProfile.SaturationBias : 0f;
        float exposureBias = _activeProfile != null ? _activeProfile.ExposureBias : 0f;
        _volume.weight = effectScale <= 0f ? 0f : 1f;

        _bloom.active = effectScale > 0f;
        _bloom.intensity.Override(bloomIntensity * bloomMultiplier * effectScale * (_bossPresentation ? 1.08f : 1f));
        _bloom.threshold.Override(bloomThreshold);
        _bloom.scatter.Override(bloomScatter);
        _bloom.tint.Override(_activeProfile != null ? _activeProfile.PrimaryAccent : bloomTint);

        _vignette.active = effectScale > 0f;
        _vignette.intensity.Override(vignetteIntensity * vignetteMultiplier * Mathf.Lerp(0.7f, 1f, effectScale));
        _vignette.color.Override(_activeProfile != null ? _activeProfile.SecondaryAccent : vignetteColor);

        _chromaticAberration.active = qualityLevel >= 2;
        _chromaticAberration.intensity.Override((chromaticIntensity + chromaticBoost) * effectScale);

        _colorAdjustments.active = effectScale > 0f;
        _colorAdjustments.contrast.Override((contrast + contrastBias) * effectScale);
        _colorAdjustments.saturation.Override((saturation + saturationBias) * effectScale);
        _colorAdjustments.postExposure.Override((postExposure + exposureBias + (_bossPresentation ? -0.02f : 0f)) * effectScale);
    }

    private void ApplyAtmosphere()
    {
        VisualQualityConfig.Tier tier = qualityConfig != null && SettingsManager.Instance != null
            ? qualityConfig.GetTier(SettingsManager.Instance.QualityLevel)
            : null;
        bool allowFog = enableFog && (tier == null ? SettingsManager.Instance == null || SettingsManager.Instance.QualityLevel > 0 : tier.fogEnabled);
        RenderSettings.fog = allowFog;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = _activeProfile != null ? _activeProfile.FogColor : fogColor;
        RenderSettings.fogStartDistance = _activeProfile != null ? _activeProfile.FogStartDistance : fogStartDistance;
        RenderSettings.fogEndDistance = _activeProfile != null ? _activeProfile.FogEndDistance : fogEndDistance;
    }
}
