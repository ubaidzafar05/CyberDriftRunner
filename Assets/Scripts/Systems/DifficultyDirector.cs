using UnityEngine;

public sealed class DifficultyDirector : MonoBehaviour
{
    [SerializeField] private ObstacleSpawner spawner;
    [SerializeField] private float minIntensity = 0.8f;
    [SerializeField] private float maxIntensity = 1.45f;
    [SerializeField] private float evaluationInterval = 0.6f;

    private float _nextEvaluationAt;

    public void Configure(ObstacleSpawner targetSpawner)
    {
        spawner = targetSpawner;
    }

    private void Update()
    {
        if (spawner == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (Time.unscaledTime < _nextEvaluationAt)
        {
            return;
        }

        _nextEvaluationAt = Time.unscaledTime + evaluationInterval;
        Evaluate();
    }

    private void Evaluate()
    {
        float startupEase = 1f - Mathf.Clamp01(GameManager.Instance.SurvivalTime / 12f);
        float bestDistanceSignal = ProgressionManager.Instance != null
            ? Mathf.Clamp01(ProgressionManager.Instance.BestDistance / 2800f)
            : 0f;

        float upgradeSignal = 0f;
        if (UpgradeSystem.Instance != null)
        {
            int upgradeCount = System.Enum.GetValues(typeof(UpgradeSystem.UpgradeType)).Length;
            for (int i = 0; i < upgradeCount; i++)
            {
                upgradeSignal += Mathf.Clamp01(UpgradeSystem.Instance.GetLevel((UpgradeSystem.UpgradeType)i) / 5f);
            }

            upgradeSignal /= Mathf.Max(1, upgradeCount);
        }

        float runDistanceSignal = Mathf.Clamp01(GameManager.Instance.Distance / 2400f);
        float comboSignal = ComboSystem.Instance != null
            ? Mathf.Clamp01((ComboSystem.Instance.CurrentMultiplier - 1f) / 5f)
            : 0f;

        float nearMissSignal = NearMissDetector.Instance != null
            ? Mathf.Clamp01(NearMissDetector.Instance.NearMissCount / 8f)
            : 0f;

        float revivePenalty = GameManager.Instance.HasUsedRevive ? 0.16f : 0f;
        float targetIntensity = 0.85f +
                                (bestDistanceSignal * 0.18f) +
                                (upgradeSignal * 0.14f) +
                                (runDistanceSignal * 0.28f) +
                                (comboSignal * 0.22f) +
                                (nearMissSignal * 0.08f) -
                                revivePenalty -
                                (startupEase * 0.18f);

        float intensity = Mathf.Clamp(targetIntensity, minIntensity, maxIntensity);
        if (LiveOpsSystem.Instance != null)
        {
            intensity *= LiveOpsSystem.Instance.GetSpawnRateMultiplier();
        }
        float powerUpBias = Mathf.Clamp((1.5f - intensity) + (startupEase * 0.1f), 0.7f, 1.35f);
        spawner.ApplyRuntimeTuning(intensity, powerUpBias);
    }
}
