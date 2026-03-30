using UnityEngine;

public sealed class DailyRewardSystem : MonoBehaviour
{
    public static DailyRewardSystem Instance { get; private set; }

    private const string LastClaimDateKey = "cdr.daily.lastClaim";
    private const string StreakKey = "cdr.daily.streak";
    private const int MaxStreak = 7;

    [Header("Rewards Per Day")]
    [SerializeField] private int[] creditRewards = { 25, 50, 75, 100, 150, 200, 500 };

    public int CurrentStreak { get; private set; }
    public bool CanClaimToday { get; private set; }
    public int TodayReward => creditRewards[Mathf.Clamp(CurrentStreak, 0, creditRewards.Length - 1)];

    public event System.Action<int, int> OnRewardClaimed; // day, credits

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool TryClaim()
    {
        if (!CanClaimToday)
        {
            return false;
        }

        int reward = TodayReward;
        ProgressionManager.Instance?.AddSoftCurrency(reward);

        CurrentStreak = (CurrentStreak + 1) % (MaxStreak + 1);
        CanClaimToday = false;

        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
        PlayerPrefs.SetString(LastClaimDateKey, today);
        PlayerPrefs.SetInt(StreakKey, CurrentStreak);
        PlayerPrefs.Save();

        OnRewardClaimed?.Invoke(CurrentStreak, reward);
        return true;
    }

    public int GetRewardForDay(int day)
    {
        return creditRewards[Mathf.Clamp(day, 0, creditRewards.Length - 1)];
    }

    private void Load()
    {
        CurrentStreak = PlayerPrefs.GetInt(StreakKey, 0);
        string lastClaim = PlayerPrefs.GetString(LastClaimDateKey, "");
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");

        if (string.IsNullOrEmpty(lastClaim))
        {
            CanClaimToday = true;
            return;
        }

        if (lastClaim == today)
        {
            CanClaimToday = false;
            return;
        }

        // Check if yesterday (streak continues) or older (streak resets)
        if (System.DateTime.TryParseExact(lastClaim, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out System.DateTime lastDate))
        {
            System.DateTime todayDate = System.DateTime.UtcNow.Date;
            int daysDiff = (todayDate - lastDate.Date).Days;

            if (daysDiff > 1)
            {
                CurrentStreak = 0;
                PlayerPrefs.SetInt(StreakKey, 0);
                PlayerPrefs.Save();
            }
        }

        CanClaimToday = true;
    }
}
