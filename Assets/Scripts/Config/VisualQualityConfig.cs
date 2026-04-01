using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/Visual Quality", fileName = "VisualQualityConfig")]
public sealed class VisualQualityConfig : ScriptableObject
{
    [System.Serializable]
    public sealed class Tier
    {
        public string name = "Medium";
        public int unityQualityLevel = 3;
        public int targetFrameRate = 60;
        public ShadowQuality shadowQuality = ShadowQuality.HardOnly;
        public int antiAliasing = 0;
        public float postProcessScale = 0.65f;
        public bool fogEnabled = true;
        public bool animatedTrafficEnabled = true;
        public bool animatedNeonEnabled = true;
        public float neonEmissionScale = 1f;
        [Range(0f, 1f)] public float trafficDensity = 1f;
    }

    [SerializeField] private Tier low = new Tier
    {
        name = "Low",
        unityQualityLevel = 1,
        targetFrameRate = 45,
        shadowQuality = ShadowQuality.Disable,
        antiAliasing = 0,
        postProcessScale = 0f,
        fogEnabled = false,
        animatedTrafficEnabled = false,
        animatedNeonEnabled = true,
        neonEmissionScale = 0.8f,
        trafficDensity = 0.2f
    };

    [SerializeField] private Tier medium = new Tier
    {
        name = "Medium",
        unityQualityLevel = 3,
        targetFrameRate = 60,
        shadowQuality = ShadowQuality.HardOnly,
        antiAliasing = 0,
        postProcessScale = 0.65f,
        fogEnabled = true,
        animatedTrafficEnabled = true,
        animatedNeonEnabled = true,
        neonEmissionScale = 1f,
        trafficDensity = 0.65f
    };

    [SerializeField] private Tier premium = new Tier
    {
        name = "Premium",
        unityQualityLevel = 4,
        targetFrameRate = 60,
        shadowQuality = ShadowQuality.All,
        antiAliasing = 2,
        postProcessScale = 1f,
        fogEnabled = true,
        animatedTrafficEnabled = true,
        animatedNeonEnabled = true,
        neonEmissionScale = 1.2f,
        trafficDensity = 1f
    };

    public Tier GetTier(int level)
    {
        switch (Mathf.Clamp(level, 0, 2))
        {
            case 0:
                return low;
            case 1:
                return medium;
            default:
                return premium;
        }
    }
}
