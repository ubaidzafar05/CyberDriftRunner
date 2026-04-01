using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/Runner Balance", fileName = "RunnerBalanceConfig")]
public sealed class RunnerBalanceConfig : ScriptableObject
{
    [Header("Speed")]
    [SerializeField] private float baseForwardSpeed = 10.5f;
    [SerializeField] private float speedGainPerSecond = 0.16f;
    [SerializeField] private float maxForwardSpeed = 24f;

    [Header("Scoring")]
    [SerializeField] private float scoreRatePerSecond = 14f;
    [SerializeField] private float distanceScale = 1f;

    public float BaseForwardSpeed => Mathf.Max(0.1f, baseForwardSpeed);
    public float SpeedGainPerSecond => Mathf.Max(0f, speedGainPerSecond);
    public float MaxForwardSpeed => Mathf.Max(BaseForwardSpeed, maxForwardSpeed);
    public float ScoreRatePerSecond => Mathf.Max(0f, scoreRatePerSecond);
    public float DistanceScale => Mathf.Max(0.01f, distanceScale);
}
