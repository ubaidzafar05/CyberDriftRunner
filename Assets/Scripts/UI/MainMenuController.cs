using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Text soundValueText;
    [SerializeField] private Text vibrationValueText;
    [SerializeField] private Text playerLevelText;
    [SerializeField] private Text currencyText;
    [SerializeField] private Text dailyRewardText;
    [SerializeField] private Text routeText;
    [SerializeField] private Text bestRunText;
    [SerializeField] private Text loadoutText;
    [SerializeField] private Text streakText;
    [SerializeField] private SkinShopController shopController;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    private float _refreshTimer;

    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += RefreshLabels;
        }

        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.OnRewardClaimed += HandleRewardClaimed;
        }

        RefreshLabels();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        RefreshDailyReward();
        RefreshPlayerInfo();
    }

    private void Update()
    {
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer > 0f)
        {
            return;
        }

        _refreshTimer = 0.35f;
        RefreshDailyReward();
        RefreshPlayerInfo();
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged -= RefreshLabels;
        }

        if (DailyRewardSystem.Instance != null)
        {
            DailyRewardSystem.Instance.OnRewardClaimed -= HandleRewardClaimed;
        }
    }

    public void Configure(GameObject panel, Text soundText, Text vibrationText)
    {
        settingsPanel = panel;
        soundValueText = soundText;
        vibrationValueText = vibrationText;
    }

    public void BindProgress(Text levelText, Text creditsText, Text rewardText)
    {
        playerLevelText = levelText;
        currencyText = creditsText;
        dailyRewardText = rewardText;
    }

    public void BindProfileDetails(Text routeValueText, Text bestValueText, Text loadoutValueText, Text streakValueText)
    {
        routeText = routeValueText;
        bestRunText = bestValueText;
        loadoutText = loadoutValueText;
        streakText = streakValueText;
    }

    public void Play()
    {
        GameManager.Instance?.StartRun();
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            if (statsPanel != null && settingsPanel.activeSelf) statsPanel.SetActive(false);
        }
    }

    public void ToggleStats()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(!statsPanel.activeSelf);
            if (settingsPanel != null && statsPanel.activeSelf) settingsPanel.SetActive(false);
        }
    }

    public void ToggleShop()
    {
        shopController?.TogglePanel();
    }

    public void ToggleLeaderboard()
    {
        leaderboardPanel?.TogglePanel();
    }

    public void BindLeaderboard(LeaderboardPanel panel)
    {
        leaderboardPanel = panel;
    }

    public void BindShop(SkinShopController controller)
    {
        shopController = controller;
    }

    public void ShowGooglePlayLeaderboard()
    {
        GooglePlayManager.Instance?.ShowLeaderboard();
    }

    public void ShowGooglePlayAchievements()
    {
        GooglePlayManager.Instance?.ShowAchievements();
    }

    public void ClaimDailyReward()
    {
        if (DailyRewardSystem.Instance != null && DailyRewardSystem.Instance.TryClaim())
        {
            AudioManager.Instance?.PlayPowerUp();
            HapticFeedback.Instance?.VibrateMedium();
            RefreshDailyReward();
            RefreshPlayerInfo();
        }
    }

    public void ToggleSound()
    {
        if (SettingsManager.Instance == null)
        {
            return;
        }

        SettingsManager.Instance.SetAudioEnabled(!SettingsManager.Instance.AudioEnabled);
        RefreshLabels();
    }

    public void ToggleVibration()
    {
        if (SettingsManager.Instance == null)
        {
            return;
        }

        SettingsManager.Instance.SetVibration(!SettingsManager.Instance.VibrationEnabled);
        RefreshLabels();
    }

    public void QuitGame()
    {
        CloudSaveManager.Instance?.SaveProgress();
        Application.Quit();
    }

    private void RefreshLabels()
    {
        if (SettingsManager.Instance == null)
        {
            return;
        }

        if (soundValueText != null)
        {
            soundValueText.text = SettingsManager.Instance.AudioEnabled ? "On" : "Off";
        }

        if (vibrationValueText != null)
        {
            vibrationValueText.text = SettingsManager.Instance.VibrationEnabled ? "On" : "Off";
        }
    }

    private void RefreshDailyReward()
    {
        if (dailyRewardText == null || DailyRewardSystem.Instance == null) return;

        if (DailyRewardSystem.Instance.CanClaimToday)
        {
            dailyRewardText.text = $"DAY {DailyRewardSystem.Instance.CurrentStreak + 1} READY  //  +{DailyRewardSystem.Instance.TodayReward} CREDITS";
            dailyRewardText.color = new Color(1f, 0.84f, 0.24f);
        }
        else
        {
            dailyRewardText.text = "Daily route stipend claimed";
            dailyRewardText.color = new Color(0.34f, 1f, 0.72f);
        }

        if (streakText != null)
        {
            int streak = DailyRewardSystem.Instance.CurrentStreak;
            int previewReward = DailyRewardSystem.Instance.GetRewardForDay(Mathf.Min(streak + (DailyRewardSystem.Instance.CanClaimToday ? 0 : 1), 6));
            streakText.text = DailyRewardSystem.Instance.CanClaimToday
                ? $"Streak {streak + 1}/7  //  Claim for +{previewReward}"
                : $"Streak locked at {streak}/7  //  Next route stipend tomorrow";
            streakText.color = DailyRewardSystem.Instance.CanClaimToday
                ? new Color(0.7f, 0.92f, 1f)
                : new Color(0.58f, 0.82f, 1f);
        }
    }

    private void RefreshPlayerInfo()
    {
        if (playerLevelText != null && XpLevelSystem.Instance != null)
        {
            playerLevelText.text = $"Level {XpLevelSystem.Instance.CurrentLevel}";
        }

        if (currencyText != null && ProgressionManager.Instance != null)
        {
            int premium = EconomySystem.Instance != null ? EconomySystem.Instance.PremiumCurrency : 0;
            currencyText.text = $"{ProgressionManager.Instance.SoftCurrency} CR  //  {premium} PR";
        }

        if (routeText != null && ProgressionManager.Instance != null)
        {
            routeText.text = $"Current Route  //  {RunDistrictCatalog.ResolveName(ProgressionManager.Instance.BestDistance)}";
        }

        if (bestRunText != null && ProgressionManager.Instance != null)
        {
            bestRunText.text = $"Best {ProgressionManager.Instance.HighScore:000000}  //  {ProgressionManager.Instance.BestDistance:0}m";
        }

        if (loadoutText != null)
        {
            string skin = ProgressionManager.Instance != null ? ProgressionManager.Instance.GetSelectedSkin().DisplayName : "Street Default";
            string trail = ResolveTrailDisplayName();
            string weapon = ResolveWeaponDisplayName();
            loadoutText.text = $"{skin}\n{trail}  //  {weapon}";
        }
    }

    private void HandleRewardClaimed(int day, int credits)
    {
        RefreshDailyReward();
        RefreshPlayerInfo();
    }

    private static string ResolveTrailDisplayName()
    {
        if (ShopSystem.Instance == null)
        {
            return "Default Trail";
        }

        string selected = ShopSystem.Instance.SelectedTrailId;
        for (int i = 0; i < ShopSystem.Instance.Items.Count; i++)
        {
            ShopItemDefinition item = ShopSystem.Instance.Items[i];
            if (item.ItemType == ShopItemType.Trail && item.RewardId == selected)
            {
                return item.DisplayName;
            }
        }

        return "Default Trail";
    }

    private static string ResolveWeaponDisplayName()
    {
        if (ShopSystem.Instance == null)
        {
            return "Standard Blaster";
        }

        string selected = ShopSystem.Instance.SelectedWeaponSkinId;
        for (int i = 0; i < ShopSystem.Instance.Items.Count; i++)
        {
            ShopItemDefinition item = ShopSystem.Instance.Items[i];
            if (item.ItemType == ShopItemType.WeaponSkin && item.RewardId == selected)
            {
                return item.DisplayName;
            }
        }

        return "Standard Blaster";
    }
}
