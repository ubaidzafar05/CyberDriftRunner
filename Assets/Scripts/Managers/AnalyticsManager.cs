using System.Text;
using UnityEngine;

public sealed class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    [SerializeField] private bool logEventsInEditor = true;
    [SerializeField] private bool logEventsInPlayerBuilds;

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

    private void LogEvent(string eventName, params object[] parameters)
    {
        if (!ShouldLogEvents())
        {
            return;
        }

        StringBuilder builder = new StringBuilder(64);
        builder.Append("[Analytics] ");
        builder.Append(eventName);
        for (int i = 0; i + 1 < parameters.Length; i += 2)
        {
            builder.Append(' ');
            builder.Append(parameters[i]);
            builder.Append('=');
            builder.Append(parameters[i + 1]);
        }

        Debug.Log(builder.ToString());
    }

    private bool ShouldLogEvents()
    {
#if UNITY_EDITOR
        return logEventsInEditor;
#else
        return logEventsInPlayerBuilds;
#endif
    }
}
