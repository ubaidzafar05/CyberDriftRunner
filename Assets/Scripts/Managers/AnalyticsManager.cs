using UnityEngine;

public sealed class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private float _sessionStartTime;
    private int _runsThisSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _sessionStartTime = Time.realtimeSinceStartup;
    }

    public void TrackFirstOpen()
    {
        if (PlayerPrefs.GetInt("cdr.analytics.firstopen", 0) == 0)
        {
            PlayerPrefs.SetInt("cdr.analytics.firstopen", 1);
            PlayerPrefs.Save();
            LogEvent("first_open");
        }
    }

    public void TrackRunStart()
    {
        _runsThisSession++;
        LogEvent("run_start", "run_number", _runsThisSession);
    }

    public void TrackRunEnd(RunSummary summary)
    {
        LogEvent("run_end",
            "score", summary.Score,
            "distance", Mathf.FloorToInt(summary.Distance),
            "credits", summary.Credits,
            "survival", Mathf.FloorToInt(summary.SurvivalTime));
    }

    public void TrackPurchase(string itemId, float price)
    {
        LogEvent("purchase", "item", itemId, "price", price);
    }

    public void TrackAdView(string adType)
    {
        LogEvent("ad_view", "type", adType);
    }

    public void TrackAchievement(string achievementId)
    {
        LogEvent("achievement_unlock", "id", achievementId);
    }

    public void TrackLevelUp(int level)
    {
        LogEvent("level_up", "level", level);
    }

    public void TrackSessionEnd()
    {
        float sessionLength = Time.realtimeSinceStartup - _sessionStartTime;
        LogEvent("session_end",
            "duration", Mathf.FloorToInt(sessionLength),
            "runs", _runsThisSession);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            TrackSessionEnd();
        }
    }

    private void OnApplicationQuit()
    {
        TrackSessionEnd();
    }

    // Unified event logging — replace with Unity Analytics SDK calls when integrated
    private static void LogEvent(string eventName, params object[] parameters)
    {
#if UNITY_EDITOR
        string paramStr = "";
        for (int i = 0; i < parameters.Length; i += 2)
        {
            if (i + 1 < parameters.Length)
            {
                paramStr += $" {parameters[i]}={parameters[i + 1]}";
            }
        }

        Debug.Log($"[Analytics] {eventName}{paramStr}");
#endif
        // TODO: Replace with Unity Analytics or Firebase when SDK is integrated
        // Unity.Services.Analytics.CustomEvent(eventName, new Dictionary<string, object>{ ... });
    }
}
