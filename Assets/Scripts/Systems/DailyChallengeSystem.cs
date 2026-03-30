using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class DailyChallenge
{
    public string Id;
    public string Description;
    public int Target;
    public int CreditReward;
    public int XpReward;
}

public sealed class DailyChallengeSystem : MonoBehaviour
{
    public static DailyChallengeSystem Instance { get; private set; }

    private const string ChallengeDate = "cdr.challenge.date";
    private const string ChallengeIdKey = "cdr.challenge.id";
    private const string ChallengeProgressKey = "cdr.challenge.progress";
    private const string ChallengeClaimedKey = "cdr.challenge.claimed";

    private static readonly DailyChallenge[] ChallengePool =
    {
        new DailyChallenge { Id = "score_3000", Description = "Score 3,000 in one run", Target = 3000, CreditReward = 40, XpReward = 50 },
        new DailyChallenge { Id = "score_8000", Description = "Score 8,000 in one run", Target = 8000, CreditReward = 80, XpReward = 90 },
        new DailyChallenge { Id = "dist_1000", Description = "Travel 1,000m in one run", Target = 1000, CreditReward = 50, XpReward = 60 },
        new DailyChallenge { Id = "dist_3000", Description = "Travel 3,000m in one run", Target = 3000, CreditReward = 100, XpReward = 110 },
        new DailyChallenge { Id = "drones_5", Description = "Destroy 5 drones in one run", Target = 5, CreditReward = 35, XpReward = 40 },
        new DailyChallenge { Id = "drones_15", Description = "Destroy 15 drones in one run", Target = 15, CreditReward = 70, XpReward = 80 },
        new DailyChallenge { Id = "hacks_3", Description = "Hack 3 obstacles in one run", Target = 3, CreditReward = 30, XpReward = 35 },
        new DailyChallenge { Id = "hacks_10", Description = "Hack 10 obstacles in one run", Target = 10, CreditReward = 65, XpReward = 75 },
        new DailyChallenge { Id = "survive_90", Description = "Survive for 90 seconds", Target = 90, CreditReward = 45, XpReward = 55 },
        new DailyChallenge { Id = "survive_180", Description = "Survive for 3 minutes", Target = 180, CreditReward = 90, XpReward = 100 },
        new DailyChallenge { Id = "credits_50", Description = "Collect 50 credits in one run", Target = 50, CreditReward = 40, XpReward = 45 },
        new DailyChallenge { Id = "combo_4", Description = "Reach a 4x combo", Target = 4, CreditReward = 35, XpReward = 40 },
        new DailyChallenge { Id = "nearmiss_5", Description = "Get 5 near misses in one run", Target = 5, CreditReward = 30, XpReward = 35 },
        new DailyChallenge { Id = "runs_3", Description = "Complete 3 runs today", Target = 3, CreditReward = 50, XpReward = 60 },
    };

    public DailyChallenge ActiveChallenge { get; private set; }
    public int Progress { get; private set; }
    public bool IsCompleted => ActiveChallenge != null && Progress >= ActiveChallenge.Target;
    public bool IsClaimed { get; private set; }

    public event System.Action<DailyChallenge> OnChallengeCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOrGenerate();
    }

    public void UpdateProgressAfterRun(RunSummary summary, int dronesKilled, int hacksPerformed, int nearMisses, int maxCombo)
    {
        if (ActiveChallenge == null || IsClaimed)
        {
            return;
        }

        string id = ActiveChallenge.Id;
        int newProgress = Progress;

        if (id.StartsWith("score_")) newProgress = Mathf.Max(newProgress, summary.Score);
        else if (id.StartsWith("dist_")) newProgress = Mathf.Max(newProgress, Mathf.FloorToInt(summary.Distance));
        else if (id.StartsWith("drones_")) newProgress = Mathf.Max(newProgress, dronesKilled);
        else if (id.StartsWith("hacks_")) newProgress = Mathf.Max(newProgress, hacksPerformed);
        else if (id.StartsWith("survive_")) newProgress = Mathf.Max(newProgress, Mathf.FloorToInt(summary.SurvivalTime));
        else if (id.StartsWith("credits_")) newProgress = Mathf.Max(newProgress, summary.Credits);
        else if (id.StartsWith("combo_")) newProgress = Mathf.Max(newProgress, maxCombo);
        else if (id.StartsWith("nearmiss_")) newProgress = Mathf.Max(newProgress, nearMisses);
        else if (id.StartsWith("runs_")) newProgress++;

        Progress = newProgress;
        PlayerPrefs.SetInt(ChallengeProgressKey, Progress);
        PlayerPrefs.Save();

        if (IsCompleted)
        {
            OnChallengeCompleted?.Invoke(ActiveChallenge);
        }
    }

    public bool TryClaimReward()
    {
        if (!IsCompleted || IsClaimed || ActiveChallenge == null)
        {
            return false;
        }

        IsClaimed = true;
        PlayerPrefs.SetInt(ChallengeClaimedKey, 1);
        PlayerPrefs.Save();

        ProgressionManager.Instance?.AddSoftCurrency(ActiveChallenge.CreditReward);
        XpLevelSystem.Instance?.AddXp(ActiveChallenge.XpReward);
        AudioManager.Instance?.PlayPowerUp();

        return true;
    }

    private void LoadOrGenerate()
    {
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
        string savedDate = PlayerPrefs.GetString(ChallengeDate, "");

        if (savedDate == today)
        {
            int savedIndex = PlayerPrefs.GetInt(ChallengeIdKey, 0);
            ActiveChallenge = ChallengePool[Mathf.Clamp(savedIndex, 0, ChallengePool.Length - 1)];
            Progress = PlayerPrefs.GetInt(ChallengeProgressKey, 0);
            IsClaimed = PlayerPrefs.GetInt(ChallengeClaimedKey, 0) == 1;
            return;
        }

        // Generate new challenge using date as seed for deterministic daily
        int seed = today.GetHashCode();
        int index = Mathf.Abs(seed) % ChallengePool.Length;
        ActiveChallenge = ChallengePool[index];
        Progress = 0;
        IsClaimed = false;

        PlayerPrefs.SetString(ChallengeDate, today);
        PlayerPrefs.SetInt(ChallengeIdKey, index);
        PlayerPrefs.SetInt(ChallengeProgressKey, 0);
        PlayerPrefs.SetInt(ChallengeClaimedKey, 0);
        PlayerPrefs.Save();
    }
}
