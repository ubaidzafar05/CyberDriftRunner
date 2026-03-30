using UnityEngine;

public sealed class XpLevelSystem : MonoBehaviour
{
    public static XpLevelSystem Instance { get; private set; }

    private const string XpKey = "cdr.xp.total";
    private const string LevelKey = "cdr.xp.level";

    [Header("XP Curve")]
    [SerializeField] private int baseXpPerLevel = 100;
    [SerializeField] private float xpScalePerLevel = 1.35f;
    [SerializeField] private int maxLevel = 50;

    [Header("XP Earn Rates")]
    [SerializeField] private float xpPerMeter = 0.5f;
    [SerializeField] private float xpPerScore = 0.1f;
    [SerializeField] private int xpPerDroneKill = 8;
    [SerializeField] private int xpPerHack = 5;

    public int CurrentLevel { get; private set; }
    public int TotalXp { get; private set; }
    public int XpForCurrentLevel => GetXpForLevel(CurrentLevel);
    public int XpForNextLevel => GetXpForLevel(CurrentLevel + 1);
    public int XpInCurrentLevel => TotalXp - XpForCurrentLevel;
    public int XpNeededForNext => XpForNextLevel - XpForCurrentLevel;
    public float LevelProgress => XpNeededForNext > 0 ? (float)XpInCurrentLevel / XpNeededForNext : 1f;

    public event System.Action<int> OnLevelUp;

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

    public void AwardRunXp(RunSummary summary)
    {
        int xpEarned = 0;
        xpEarned += Mathf.FloorToInt(summary.Distance * xpPerMeter);
        xpEarned += Mathf.FloorToInt(summary.Score * xpPerScore);
        AddXp(xpEarned);
    }

    public void AwardDroneKillXp()
    {
        AddXp(xpPerDroneKill);
    }

    public void AwardHackXp()
    {
        AddXp(xpPerHack);
    }

    public void AddXp(int amount)
    {
        if (amount <= 0 || CurrentLevel >= maxLevel)
        {
            return;
        }

        int previousLevel = CurrentLevel;
        TotalXp += amount;

        while (CurrentLevel < maxLevel && TotalXp >= XpForNextLevel)
        {
            CurrentLevel++;
        }

        if (CurrentLevel > previousLevel)
        {
            OnLevelUp?.Invoke(CurrentLevel);
        }

        Save();
    }

    public int GetXpForLevel(int level)
    {
        if (level <= 0)
        {
            return 0;
        }

        float total = 0f;
        for (int i = 1; i < level; i++)
        {
            total += baseXpPerLevel * Mathf.Pow(xpScalePerLevel, i - 1);
        }

        return Mathf.FloorToInt(total);
    }

    public string GetUnlockDescription(int level)
    {
        switch (level)
        {
            case 2: return "Hack range +10%";
            case 5: return "Starting shield (2s)";
            case 8: return "Double score duration +2s";
            case 10: return "Credit magnet unlock";
            case 15: return "Slow motion duration +1s";
            case 20: return "Extra revive per run";
            case 25: return "Auto-shoot unlock";
            case 30: return "EMP radius +50%";
            case 40: return "Golden trail effect";
            case 50: return "Legendary skin: Neon Phantom";
            default: return null;
        }
    }

    private void Load()
    {
        TotalXp = PlayerPrefs.GetInt(XpKey, 0);
        CurrentLevel = PlayerPrefs.GetInt(LevelKey, 1);

        // Recalculate level from XP in case of data mismatch
        int recalcLevel = 1;
        while (recalcLevel < maxLevel && TotalXp >= GetXpForLevel(recalcLevel + 1))
        {
            recalcLevel++;
        }

        CurrentLevel = recalcLevel;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(XpKey, TotalXp);
        PlayerPrefs.SetInt(LevelKey, CurrentLevel);
        PlayerPrefs.Save();
    }
}
