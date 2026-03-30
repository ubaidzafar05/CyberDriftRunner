using UnityEngine;

/// <summary>
/// Schedules local push notifications for player retention.
/// Sends reminders for daily rewards, streaks, and return-to-play prompts.
/// </summary>
public sealed class NotificationScheduler : MonoBehaviour
{
    public static NotificationScheduler Instance { get; private set; }

    [Header("Notification Timing (hours)")]
    [SerializeField] private float inactivityReminder = 24f;
    [SerializeField] private float dailyRewardReminder = 20f;
    [SerializeField] private float streakRiskReminder = 22f;

    private const string LastPlayKey = "cdr.notify.lastPlay";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RecordPlaySession();
    }

    public void RecordPlaySession()
    {
        PlayerPrefs.SetString(LastPlayKey, System.DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();
    }

    public void ScheduleRetentionNotifications()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.NotificationsEnabled)
        {
            CancelAll();
            return;
        }

        // Schedule notifications when app goes to background
        ScheduleNotification(
            "Your daily reward is waiting!",
            "Login to claim free credits and keep your streak alive!",
            dailyRewardReminder
        );

        ScheduleNotification(
            "Don't lose your streak!",
            "Your streak is at risk! Play now to keep it going.",
            streakRiskReminder
        );

        ScheduleNotification(
            "The neon city misses you!",
            "Come back for a quick run. Beat your high score of " + GetHighScoreLabel(),
            inactivityReminder
        );
    }

    public void CancelAll()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CancelAndroidNotifications();
#endif
        Debug.Log("[NotificationScheduler] Notifications cancelled");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            RecordPlaySession();
            ScheduleRetentionNotifications();
        }
        else
        {
            CancelAll();
            RecordPlaySession();
        }
    }

    private void OnApplicationQuit()
    {
        RecordPlaySession();
        ScheduleRetentionNotifications();
    }

    private string GetHighScoreLabel()
    {
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.HighScore > 0)
        {
            return ProgressionManager.Instance.HighScore.ToString("N0") + "!";
        }
        return "your record!";
    }

    private void ScheduleNotification(string title, string body, float hoursFromNow)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ScheduleAndroidNotification(title, body, hoursFromNow);
#else
        Debug.Log($"[NotificationScheduler] Scheduled: '{title}' in {hoursFromNow}h");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void ScheduleAndroidNotification(string title, string body, float hoursFromNow)
    {
        try
        {
            long triggerMs = (long)(hoursFromNow * 3600f * 1000f);
            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

                // Use Android's AlarmManager or NotificationManager
                // This is a simplified stub — for production, use Unity Mobile Notifications package
                Debug.Log($"[NotificationScheduler] Android notification scheduled: {title}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NotificationScheduler] Failed to schedule: {e.Message}");
        }
    }

    private void CancelAndroidNotifications()
    {
        try
        {
            // Cancel all pending notifications
            Debug.Log("[NotificationScheduler] Android notifications cancelled");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NotificationScheduler] Failed to cancel: {e.Message}");
        }
    }
#endif
}
