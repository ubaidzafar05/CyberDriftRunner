using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/District Presentation Profile", fileName = "DistrictPresentationProfile")]
public sealed class DistrictPresentationProfile : ScriptableObject
{
    [SerializeField] private int districtIndex;
    [SerializeField] private string districtName = "Neon Gateway";
    [SerializeField] private Color cameraBackground = new Color(0.01f, 0.015f, 0.04f);
    [SerializeField] private Color ambientColor = new Color(0.04f, 0.08f, 0.14f);
    [SerializeField] private Color fogColor = new Color(0.018f, 0.024f, 0.076f);
    [SerializeField] private float fogStartDistance = 18f;
    [SerializeField] private float fogEndDistance = 118f;
    [SerializeField] private Color keyLightColor = new Color(0.64f, 0.82f, 1f);
    [SerializeField] private float keyLightIntensity = 1.24f;
    [SerializeField] private Color fillLightColor = new Color(1f, 0.34f, 0.58f);
    [SerializeField] private float fillLightIntensity = 0.42f;
    [SerializeField] private Color primaryAccent = new Color(0f, 0.96f, 1f);
    [SerializeField] private Color secondaryAccent = new Color(1f, 0f, 0.78f);
    [SerializeField] private Color tertiaryAccent = new Color(0.42f, 0f, 1f);
    [SerializeField] private float bloomIntensityMultiplier = 1f;
    [SerializeField] private float vignetteIntensityMultiplier = 1f;
    [SerializeField] private float chromaticBoost = 0f;
    [SerializeField] private float contrastBias;
    [SerializeField] private float saturationBias;
    [SerializeField] private float exposureBias;

    public int DistrictIndex => Mathf.Max(0, districtIndex);
    public string DistrictName => string.IsNullOrWhiteSpace(districtName) ? "District" : districtName;
    public Color CameraBackground => cameraBackground;
    public Color AmbientColor => ambientColor;
    public Color FogColor => fogColor;
    public float FogStartDistance => Mathf.Max(0f, fogStartDistance);
    public float FogEndDistance => Mathf.Max(FogStartDistance + 1f, fogEndDistance);
    public Color KeyLightColor => keyLightColor;
    public float KeyLightIntensity => Mathf.Max(0f, keyLightIntensity);
    public Color FillLightColor => fillLightColor;
    public float FillLightIntensity => Mathf.Max(0f, fillLightIntensity);
    public Color PrimaryAccent => primaryAccent;
    public Color SecondaryAccent => secondaryAccent;
    public Color TertiaryAccent => tertiaryAccent;
    public float BloomIntensityMultiplier => Mathf.Max(0f, bloomIntensityMultiplier);
    public float VignetteIntensityMultiplier => Mathf.Max(0f, vignetteIntensityMultiplier);
    public float ChromaticBoost => chromaticBoost;
    public float ContrastBias => contrastBias;
    public float SaturationBias => saturationBias;
    public float ExposureBias => exposureBias;
}
