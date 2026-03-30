using UnityEngine;

public sealed class SeasonPassSystem : MonoBehaviour
{
    public static SeasonPassSystem Instance { get; private set; }

    private const string SeasonPrefix = "cdr.season.";
    private const int MaxTier = 30;

    [System.Serializable]
    public struct TierReward
    {
        public int FreeCredits;
        public int PremiumCredits;
        public int FreeXp;
        public int PremiumXp;
        public string PremiumSkinId;
    }

    private int _currentXp;
    private int _currentTier;
    private bool _isPremium;
    private int _seasonId;

    public int CurrentTier => _currentTier;
    public int CurrentXp => _currentXp;
    public int XpForNextTier => GetXpForTier(_currentTier + 1);
    public bool IsPremium => _isPremium;
    public int SeasonId => _seasonId;

    public event System.Action<int, TierReward> OnTierUnlocked;

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

    public void AddSeasonXp(int amount)
    {
        if (amount <= 0 || _currentTier >= MaxTier)
        {
            return;
        }

        _currentXp += amount;
        while (_currentTier < MaxTier && _currentXp >= GetXpForTier(_currentTier + 1))
        {
            _currentXp -= GetXpForTier(_currentTier + 1);
            _currentTier++;
            TierReward reward = GetReward(_currentTier);
            GrantReward(reward);
            OnTierUnlocked?.Invoke(_currentTier, reward);
        }

        Save();
    }

    public void UpgradeToPremium()
    {
        if (_isPremium)
        {
            return;
        }

        _isPremium = true;
        // Retroactively grant premium rewards for already-unlocked tiers
        for (int i = 1; i <= _currentTier; i++)
        {
            TierReward reward = GetReward(i);
            ProgressionManager.Instance?.AddSoftCurrency(reward.PremiumCredits);
            XpLevelSystem.Instance?.AddXp(reward.PremiumXp);
        }

        Save();
    }

    public TierReward GetReward(int tier)
    {
        int baseFreeCredits = 10 + (tier * 5);
        int basePremiumCredits = 15 + (tier * 8);

        return new TierReward
        {
            FreeCredits = baseFreeCredits,
            PremiumCredits = basePremiumCredits,
            FreeXp = 15 + (tier * 3),
            PremiumXp = 25 + (tier * 5),
            PremiumSkinId = (tier % 10 == 0) ? $"season_{_seasonId}_tier_{tier}" : ""
        };
    }

    public static int GetXpForTier(int tier)
    {
        return 50 + (tier * 30);
    }

    private void GrantReward(TierReward reward)
    {
        ProgressionManager.Instance?.AddSoftCurrency(reward.FreeCredits);
        XpLevelSystem.Instance?.AddXp(reward.FreeXp);

        if (_isPremium)
        {
            ProgressionManager.Instance?.AddSoftCurrency(reward.PremiumCredits);
            XpLevelSystem.Instance?.AddXp(reward.PremiumXp);
        }

        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateMedium();
    }

    private void Load()
    {
        _seasonId = PlayerPrefs.GetInt(SeasonPrefix + "id", 1);
        _currentTier = PlayerPrefs.GetInt(SeasonPrefix + "tier", 0);
        _currentXp = PlayerPrefs.GetInt(SeasonPrefix + "xp", 0);
        _isPremium = PlayerPrefs.GetInt(SeasonPrefix + "premium", 0) == 1;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SeasonPrefix + "id", _seasonId);
        PlayerPrefs.SetInt(SeasonPrefix + "tier", _currentTier);
        PlayerPrefs.SetInt(SeasonPrefix + "xp", _currentXp);
        PlayerPrefs.SetInt(SeasonPrefix + "premium", _isPremium ? 1 : 0);
        PlayerPrefs.Save();
    }
}
