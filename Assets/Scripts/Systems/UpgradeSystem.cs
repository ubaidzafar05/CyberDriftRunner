using UnityEngine;

public sealed class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    private const string UpgradePrefix = "cdr.upgrade.";

    [Header("Upgrade Definitions")]
    [SerializeField] private int maxUpgradeLevel = 5;

    // Base costs per upgrade (scales with level)
    private static readonly int[] BaseCosts = { 50, 75, 100, 80, 60, 90, 120 };
    private static readonly float CostMultiplierPerLevel = 1.8f;

    public enum UpgradeType
    {
        BaseSpeed,
        JumpHeight,
        HackRange,
        ShieldDuration,
        TargetRange,
        CreditMagnet,
        SlowMotionDuration
    }

    private static readonly string[] UpgradeNames =
    {
        "Base Speed",
        "Jump Height",
        "Hack Range",
        "Shield Duration",
        "Target Range",
        "Credit Magnet",
        "Slow Motion Duration"
    };

    private static readonly string[] UpgradeDescriptions =
    {
        "+5% forward speed per level",
        "+8% jump force per level",
        "+12% hack detection range per level",
        "+1s shield duration per level",
        "+10% shooting range per level",
        "+0.5 unit pickup radius per level",
        "+0.5s slow motion per level"
    };

    private int[] _levels;

    public event System.Action<UpgradeType, int> OnUpgradePurchased;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _levels = new int[System.Enum.GetValues(typeof(UpgradeType)).Length];
        Load();
    }

    public int GetLevel(UpgradeType type) => _levels[(int)type];

    public int GetCost(UpgradeType type)
    {
        int level = _levels[(int)type];
        if (level >= maxUpgradeLevel)
        {
            return -1;
        }

        int baseCost = (int)type < BaseCosts.Length ? BaseCosts[(int)type] : 100;
        return Mathf.CeilToInt(baseCost * Mathf.Pow(CostMultiplierPerLevel, level));
    }

    public string GetName(UpgradeType type) => UpgradeNames[(int)type];
    public string GetDescription(UpgradeType type) => UpgradeDescriptions[(int)type];
    public bool IsMaxed(UpgradeType type) => _levels[(int)type] >= maxUpgradeLevel;

    public float GetMultiplier(UpgradeType type)
    {
        int level = _levels[(int)type];
        switch (type)
        {
            case UpgradeType.BaseSpeed: return 1f + (level * 0.05f);
            case UpgradeType.JumpHeight: return 1f + (level * 0.08f);
            case UpgradeType.HackRange: return 1f + (level * 0.12f);
            case UpgradeType.ShieldDuration: return level;
            case UpgradeType.TargetRange: return 1f + (level * 0.10f);
            case UpgradeType.CreditMagnet: return level * 0.5f;
            case UpgradeType.SlowMotionDuration: return level * 0.5f;
            default: return 1f;
        }
    }

    public bool TryPurchase(UpgradeType type)
    {
        if (IsMaxed(type) || ProgressionManager.Instance == null)
        {
            return false;
        }

        int cost = GetCost(type);
        if (cost < 0 || ProgressionManager.Instance.SoftCurrency < cost)
        {
            return false;
        }

        ProgressionManager.Instance.AddSoftCurrency(-cost);
        _levels[(int)type]++;
        Save();

        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateMedium();
        OnUpgradePurchased?.Invoke(type, _levels[(int)type]);
        return true;
    }

    private void Load()
    {
        for (int i = 0; i < _levels.Length; i++)
        {
            _levels[i] = PlayerPrefs.GetInt(UpgradePrefix + i, 0);
        }
    }

    private void Save()
    {
        for (int i = 0; i < _levels.Length; i++)
        {
            PlayerPrefs.SetInt(UpgradePrefix + i, _levels[i]);
        }

        PlayerPrefs.Save();
    }
}
