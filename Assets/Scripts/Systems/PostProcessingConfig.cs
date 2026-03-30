using UnityEngine;

/// <summary>
/// URP post-processing configuration helper.
/// Attach to the camera and configure bloom/vignette/chromatic aberration.
/// Requires Universal Render Pipeline and Volume component.
/// When URP is active in the project, uncomment the URP-specific code below.
/// </summary>
public sealed class PostProcessingConfig : MonoBehaviour
{
    [Header("Bloom")]
    [SerializeField] private float bloomIntensity = 2.5f;
    [SerializeField] private float bloomThreshold = 0.8f;
    [SerializeField] private float bloomScatter = 0.7f;
    [SerializeField] private Color bloomTint = new Color(0.6f, 0.85f, 1f);

    [Header("Vignette")]
    [SerializeField] private float vignetteIntensity = 0.35f;
    [SerializeField] private Color vignetteColor = new Color(0.02f, 0f, 0.15f);

    [Header("Chromatic Aberration")]
    [SerializeField] private float chromaticIntensity = 0.15f;

    [Header("Color Adjustments")]
    [SerializeField] private float contrast = 10f;
    [SerializeField] private float saturation = 15f;

    /*
    // URP Volume Setup — uncomment when URP is active in the project:
    
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    
    private Volume _volume;
    
    private void Awake()
    {
        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 1;
        
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.profile = profile;
        
        // Bloom
        Bloom bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        bloom.tint.Override(bloomTint);
        
        // Vignette  
        Vignette vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(vignetteIntensity);
        vignette.color.Override(vignetteColor);
        
        // Chromatic Aberration
        ChromaticAberration ca = profile.Add<ChromaticAberration>();
        ca.active = true;
        ca.intensity.Override(chromaticIntensity);
        
        // Color Adjustments
        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.contrast.Override(contrast);
        colorAdj.saturation.Override(saturation);
    }
    */

    private void Awake()
    {
        // Placeholder — URP Volume setup goes here when URP is activated.
        // See commented code above for full implementation.
        Debug.Log("[PostProcessing] URP Volume config ready. Activate URP to enable bloom/vignette/CA.");
    }
}
