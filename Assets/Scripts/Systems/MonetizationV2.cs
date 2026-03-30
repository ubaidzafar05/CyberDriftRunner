using UnityEngine;

public sealed class MonetizationV2 : MonoBehaviour
{
    public static MonetizationV2 Instance { get; private set; }

    private const string MonetPrefix = "cdr.monet.";

    [Header("Ad Pacing")]
    [SerializeField] private int minSessionsBetweenInterstitials = 3;
    [SerializeField] private float minTimeBetweenInterstitials = 180f;

    [Header("Piggy Bank")]
    [SerializeField] private int piggyBankMax = 500;
    [SerializeField] private float piggyBankRate = 0.1f; // % of credits earned added to piggy

    [Header("Starter Pack")]
    [SerializeField] private int starterPackRunThreshold = 3;
    [SerializeField] private int starterPackCredits = 500;

    private int _sessionCount;
    private float _lastInterstitialTime;
    private int _piggyBankCredits;
    private bool _adsRemoved;
    private bool _starterPackPurchased;

    public int PiggyBankCredits => _piggyBankCredits;
    public bool PiggyBankFull => _piggyBankCredits >= piggyBankMax;
    public bool AdsRemoved => _adsRemoved;
    public bool StarterPackAvailable => !_starterPackPurchased && ProgressionManager.Instance != null
                                         && ProgressionManager.Instance.TotalRuns >= starterPackRunThreshold;

    public event System.Action<int> OnPiggyBankBroken;
    public event System.Action OnStarterPackShown;

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

    public void OnRunStarted()
    {
        _sessionCount++;
    }

    public bool ShouldShowInterstitial()
    {
        if (_adsRemoved)
        {
            return false;
        }

        if (_sessionCount < minSessionsBetweenInterstitials)
        {
            return false;
        }

        if (Time.realtimeSinceStartup - _lastInterstitialTime < minTimeBetweenInterstitials)
        {
            return false;
        }

        return true;
    }

    public void RecordInterstitialShown()
    {
        _sessionCount = 0;
        _lastInterstitialTime = Time.realtimeSinceStartup;
    }

    public void AddToPiggyBank(int creditsEarned)
    {
        if (_piggyBankCredits >= piggyBankMax)
        {
            return;
        }

        int toAdd = Mathf.Max(1, Mathf.FloorToInt(creditsEarned * piggyBankRate));
        _piggyBankCredits = Mathf.Min(_piggyBankCredits + toAdd, piggyBankMax);
        Save();
    }

    public bool TryBreakPiggyBank()
    {
        if (_piggyBankCredits <= 0)
        {
            return false;
        }

        int credits = _piggyBankCredits;
        _piggyBankCredits = 0;
        ProgressionManager.Instance?.AddSoftCurrency(credits);
        Save();

        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateHeavy();
        OnPiggyBankBroken?.Invoke(credits);
        return true;
    }

    public void PurchaseRemoveAds()
    {
        _adsRemoved = true;
        Save();
    }

    public void PurchaseStarterPack()
    {
        _starterPackPurchased = true;
        ProgressionManager.Instance?.AddSoftCurrency(starterPackCredits);
        Save();
    }

    private void Load()
    {
        _piggyBankCredits = PlayerPrefs.GetInt(MonetPrefix + "piggy", 0);
        _adsRemoved = PlayerPrefs.GetInt(MonetPrefix + "noads", 0) == 1;
        _starterPackPurchased = PlayerPrefs.GetInt(MonetPrefix + "starter", 0) == 1;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(MonetPrefix + "piggy", _piggyBankCredits);
        PlayerPrefs.SetInt(MonetPrefix + "noads", _adsRemoved ? 1 : 0);
        PlayerPrefs.SetInt(MonetPrefix + "starter", _starterPackPurchased ? 1 : 0);
        PlayerPrefs.Save();
    }
}
