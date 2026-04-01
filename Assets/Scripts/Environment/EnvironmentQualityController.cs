using UnityEngine;

public sealed class EnvironmentQualityController : MonoBehaviour
{
    [SerializeField] private VisualQualityConfig qualityConfig;
    [SerializeField] private NeonPropAnimator[] neonProps;
    [SerializeField] private LoopingTrafficProp[] trafficProps;

    private void Awake()
    {
        RefreshTargets();
        ApplyQuality();
    }

    private void OnEnable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += ApplyQuality;
        }
    }

    private void OnDisable()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= ApplyQuality;
        }
    }

    public void Configure(VisualQualityConfig config)
    {
        qualityConfig = config;
        RefreshTargets();
        ApplyQuality();
    }

    private void RefreshTargets()
    {
        if (neonProps == null || neonProps.Length == 0)
        {
            neonProps = FindObjectsByType<NeonPropAnimator>(FindObjectsInactive.Include);
        }

        if (trafficProps == null || trafficProps.Length == 0)
        {
            trafficProps = FindObjectsByType<LoopingTrafficProp>(FindObjectsInactive.Include);
        }
    }

    private void ApplyQuality()
    {
        if (qualityConfig == null || SettingsManager.Instance == null)
        {
            return;
        }

        VisualQualityConfig.Tier tier = qualityConfig.GetTier(SettingsManager.Instance.QualityLevel);
        ApplyTraffic(tier);
        ApplyNeon(tier);
    }

    private void ApplyTraffic(VisualQualityConfig.Tier tier)
    {
        if (trafficProps == null)
        {
            return;
        }

        int activeCount = Mathf.RoundToInt(trafficProps.Length * Mathf.Clamp01(tier.trafficDensity));
        for (int i = 0; i < trafficProps.Length; i++)
        {
            if (trafficProps[i] == null)
            {
                continue;
            }

            bool enabled = tier.animatedTrafficEnabled && i < activeCount;
            trafficProps[i].gameObject.SetActive(enabled);
        }
    }

    private void ApplyNeon(VisualQualityConfig.Tier tier)
    {
        if (neonProps == null)
        {
            return;
        }

        for (int i = 0; i < neonProps.Length; i++)
        {
            if (neonProps[i] == null)
            {
                continue;
            }

            neonProps[i].SetQuality(tier.animatedNeonEnabled, tier.neonEmissionScale);
        }
    }
}
