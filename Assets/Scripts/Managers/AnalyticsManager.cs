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
        EventBus.Subscribe<ShopItemPurchasedEvent>(HandleShopPurchase);
        EventBus.Subscribe<RunStartedEvent>(HandleRunStarted);
        EventBus.Subscribe<RunEndedEvent>(HandleRunEnded);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EventBus.Unsubscribe<ShopItemPurchasedEvent>(HandleShopPurchase);
            EventBus.Unsubscribe<RunStartedEvent>(HandleRunStarted);
            EventBus.Unsubscribe<RunEndedEvent>(HandleRunEnded);
        }
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
        LogEvent("game_start", "run_number", _runsThisSession);
    }

    public void TrackRunEnd(RunSummary summary)
    {
        LogEvent("game_over",
            "score", summary.Score,
            "distance", Mathf.FloorToInt(summary.Distance),
            "credits", summary.Credits,
            "survival", Mathf.FloorToInt(summary.SurvivalTime));
    }

    public void TrackPurchase(string itemId, float price)
    {
        LogEvent("purchase_made", "item", itemId, "price", price);
    }

    public void TrackAdView(string adType)
    {
        LogEvent("ad_watched", "type", adType);
    }

    public void TrackReviveUsed(float distance)
    {
        LogEvent("revive_used", "distance", Mathf.FloorToInt(distance));
    }

    public void TrackDistanceReached(int distance)
    {
        LogEvent("distance_reached", "distance", distance);
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

    private void HandleShopPurchase(ShopItemPurchasedEvent payload)
    {
        TrackPurchase(payload.ItemId, payload.Price);
    }

    private void HandleRunStarted(RunStartedEvent payload)
    {
        TrackRunStart();
    }

    private void HandleRunEnded(RunEndedEvent payload)
    {
        TrackRunEnd(payload.Summary);
        if (!string.IsNullOrWhiteSpace(payload.DeathReason))
        {
            LogEvent("death_reason", "reason", payload.DeathReason);
        }
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
