using UnityEngine;

public sealed class PerformanceAuditor : MonoBehaviour
{
    [Header("Thresholds")]
    [SerializeField] private int targetFps = 60;
    [SerializeField] private float fpsWarningThreshold = 45f;
    [SerializeField] private int drawCallWarningThreshold = 150;
    [SerializeField] private float gcAllocWarningMB = 5f;

    [Header("Monitoring")]
    [SerializeField] private float checkIntervalSeconds = 5f;

    private float _checkTimer;
    private float _fpsAccumulator;
    private int _fpsFrameCount;
    private float _minFps = float.MaxValue;

    public float CurrentFps { get; private set; }
    public float MinFps => _minFps;

    private void Awake()
    {
        Application.targetFrameRate = targetFps;
        QualitySettings.vSyncCount = 0;
    }

    private void Update()
    {
        _fpsAccumulator += Time.unscaledDeltaTime;
        _fpsFrameCount++;

        _checkTimer += Time.unscaledDeltaTime;
        if (_checkTimer >= checkIntervalSeconds)
        {
            _checkTimer = 0f;
            RunAudit();
        }
    }

    private void RunAudit()
    {
        if (_fpsAccumulator <= 0f || _fpsFrameCount == 0)
        {
            return;
        }

        CurrentFps = _fpsFrameCount / _fpsAccumulator;
        _minFps = Mathf.Min(_minFps, CurrentFps);
        _fpsAccumulator = 0f;
        _fpsFrameCount = 0;

        if (CurrentFps < fpsWarningThreshold)
        {
            Debug.LogWarning($"[PerfAudit] FPS drop: {CurrentFps:0.0} (target {targetFps})");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        if (memoryMB > gcAllocWarningMB)
        {
            Debug.LogWarning($"[PerfAudit] High GC memory: {memoryMB:0.0}MB (budget {gcAllocWarningMB:0.0}MB)");
        }
#endif
    }

    [ContextMenu("Log Performance Report")]
    public void LogReport()
    {
        Debug.Log($"[PerfAudit] FPS: {CurrentFps:0.0} | Min: {_minFps:0.0} | Target: {targetFps}");
        Debug.Log($"[PerfAudit] Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        Debug.Log($"[PerfAudit] Draw call warning threshold: {drawCallWarningThreshold}");
        Debug.Log($"[PerfAudit] GC Memory: {System.GC.GetTotalMemory(false) / (1024f * 1024f):0.0}MB");
        Debug.Log($"[PerfAudit] Active Drones: {EnemyDrone.ActiveDrones.Count} | Active Obstacles: {RunnerObstacle.ActiveObstacles.Count}");
    }
}
