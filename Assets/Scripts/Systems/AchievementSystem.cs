using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class AchievementDefinition
{
    public string Id;
    public string Title;
    public string Description;
    public int Tier; // 0=Bronze, 1=Silver, 2=Gold
    public int Target;
    public int CreditReward;
    public int XpReward;
}

public sealed class AchievementSystem : MonoBehaviour
{
    public static AchievementSystem Instance { get; private set; }

    private const string AchievementPrefix = "cdr.ach.";

    private List<AchievementDefinition> _definitions;
    private readonly HashSet<string> _unlockedIds = new HashSet<string>();

    public IReadOnlyList<AchievementDefinition> Definitions => _definitions;

    public event System.Action<AchievementDefinition> OnAchievementUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDefinitions();
        LoadUnlocked();
    }

    public bool IsUnlocked(string achievementId)
    {
        return _unlockedIds.Contains(achievementId);
    }

    public int GetProgress(string achievementId)
    {
        return PlayerPrefs.GetInt(AchievementPrefix + "prog." + achievementId, 0);
    }

    public void IncrementProgress(string achievementId, int amount = 1)
    {
        if (IsUnlocked(achievementId))
        {
            return;
        }

        AchievementDefinition def = FindDefinition(achievementId);
        if (def == null)
        {
            return;
        }

        int current = GetProgress(achievementId) + amount;
        PlayerPrefs.SetInt(AchievementPrefix + "prog." + achievementId, current);

        if (current >= def.Target)
        {
            Unlock(def);
        }

        PlayerPrefs.Save();
    }

    public void SetProgress(string achievementId, int value)
    {
        if (IsUnlocked(achievementId))
        {
            return;
        }

        AchievementDefinition def = FindDefinition(achievementId);
        if (def == null)
        {
            return;
        }

        PlayerPrefs.SetInt(AchievementPrefix + "prog." + achievementId, value);

        if (value >= def.Target)
        {
            Unlock(def);
        }

        PlayerPrefs.Save();
    }

    public void CheckAfterRun(RunSummary summary)
    {
        SetProgress("score_1000", summary.Score);
        SetProgress("score_5000", summary.Score);
        SetProgress("score_25000", summary.Score);
        SetProgress("score_100000", summary.Score);

        SetProgress("dist_500", Mathf.FloorToInt(summary.Distance));
        SetProgress("dist_2000", Mathf.FloorToInt(summary.Distance));
        SetProgress("dist_5000", Mathf.FloorToInt(summary.Distance));
        SetProgress("dist_10000", Mathf.FloorToInt(summary.Distance));

        if (ProgressionManager.Instance != null)
        {
            SetProgress("runs_10", ProgressionManager.Instance.TotalRuns);
            SetProgress("runs_50", ProgressionManager.Instance.TotalRuns);
            SetProgress("runs_200", ProgressionManager.Instance.TotalRuns);
        }
    }

    private void Unlock(AchievementDefinition def)
    {
        if (_unlockedIds.Contains(def.Id))
        {
            return;
        }

        _unlockedIds.Add(def.Id);
        PlayerPrefs.SetInt(AchievementPrefix + def.Id, 1);
        PlayerPrefs.Save();

        ProgressionManager.Instance?.AddSoftCurrency(def.CreditReward);
        XpLevelSystem.Instance?.AddXp(def.XpReward);
        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateMedium();

        OnAchievementUnlocked?.Invoke(def);
    }

    private AchievementDefinition FindDefinition(string id)
    {
        for (int i = 0; i < _definitions.Count; i++)
        {
            if (_definitions[i].Id == id)
            {
                return _definitions[i];
            }
        }

        return null;
    }

    private void LoadUnlocked()
    {
        _unlockedIds.Clear();
        for (int i = 0; i < _definitions.Count; i++)
        {
            if (PlayerPrefs.GetInt(AchievementPrefix + _definitions[i].Id, 0) == 1)
            {
                _unlockedIds.Add(_definitions[i].Id);
            }
        }
    }

    private void BuildDefinitions()
    {
        _definitions = new List<AchievementDefinition>
        {
            // Score achievements
            Ach("score_1000", "First Score", "Score 1,000 in a single run", 0, 1000, 15, 20),
            Ach("score_5000", "Getting Serious", "Score 5,000 in a single run", 1, 5000, 40, 50),
            Ach("score_25000", "Score Master", "Score 25,000 in a single run", 2, 25000, 100, 120),
            Ach("score_100000", "Legendary Runner", "Score 100,000 in a single run", 2, 100000, 500, 300),

            // Distance achievements
            Ach("dist_500", "First Steps", "Travel 500m in a single run", 0, 500, 15, 20),
            Ach("dist_2000", "Marathon", "Travel 2,000m in a single run", 1, 2000, 50, 60),
            Ach("dist_5000", "Ultra Runner", "Travel 5,000m in a single run", 2, 5000, 120, 150),
            Ach("dist_10000", "Infinite Runner", "Travel 10,000m in a single run", 2, 10000, 500, 350),

            // Run count achievements
            Ach("runs_10", "Regular", "Complete 10 runs", 0, 10, 25, 30),
            Ach("runs_50", "Dedicated", "Complete 50 runs", 1, 50, 75, 80),
            Ach("runs_200", "Addicted", "Complete 200 runs", 2, 200, 200, 200),

            // Combat achievements
            Ach("drones_10", "Drone Hunter", "Destroy 10 drones total", 0, 10, 20, 25),
            Ach("drones_50", "Drone Slayer", "Destroy 50 drones total", 1, 50, 60, 70),
            Ach("drones_200", "Exterminator", "Destroy 200 drones total", 2, 200, 150, 180),

            // Hack achievements
            Ach("hacks_5", "Script Kiddie", "Hack 5 objects", 0, 5, 15, 20),
            Ach("hacks_25", "Hacker", "Hack 25 objects", 1, 25, 50, 60),
            Ach("hacks_100", "Ghost in the Machine", "Hack 100 objects", 2, 100, 150, 180),

            // Combo achievements
            Ach("combo_5", "Combo Starter", "Reach 5x combo", 0, 5, 20, 25),
            Ach("combo_8", "Combo Master", "Reach 8x combo", 2, 8, 75, 90),

            // Near-miss achievements
            Ach("nearmiss_10", "Risk Taker", "10 near misses in one run", 0, 10, 25, 30),
            Ach("nearmiss_50", "Daredevil", "50 near misses total", 1, 50, 60, 75),

            // Collection achievements
            Ach("credits_500", "Saver", "Earn 500 credits total", 0, 500, 25, 30),
            Ach("credits_5000", "Wealthy", "Earn 5,000 credits total", 1, 5000, 100, 120),
            Ach("credits_25000", "Tycoon", "Earn 25,000 credits total", 2, 25000, 300, 250),

            // Skin achievements
            Ach("skins_2", "Fashionable", "Unlock 2 skins", 0, 2, 30, 35),
            Ach("skins_4", "Collector", "Unlock all 4 skins", 2, 4, 100, 120),

            // Survival achievements
            Ach("survive_60", "One Minute", "Survive 60 seconds", 0, 60, 20, 25),
            Ach("survive_180", "Three Minutes", "Survive 180 seconds", 1, 180, 60, 70),
            Ach("survive_300", "Five Minutes", "Survive 300 seconds", 2, 300, 150, 180),

            // Special
            Ach("revive_1", "Second Chance", "Use revive for the first time", 0, 1, 15, 20),
            Ach("daily_7", "Weekly Warrior", "Claim daily rewards 7 days in a row", 1, 7, 100, 120),
        };
    }

    private static AchievementDefinition Ach(string id, string title, string desc, int tier, int target, int credits, int xp)
    {
        return new AchievementDefinition
        {
            Id = id,
            Title = title,
            Description = desc,
            Tier = tier,
            Target = target,
            CreditReward = credits,
            XpReward = xp
        };
    }
}
