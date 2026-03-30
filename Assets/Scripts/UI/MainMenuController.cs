using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string SoundPrefKey = "cdr.sound";
    private const string VibrationPrefKey = "cdr.vibration";

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private Text soundValueText;
    [SerializeField] private Text vibrationValueText;
    [SerializeField] private Text playerLevelText;
    [SerializeField] private Text currencyText;
    [SerializeField] private Text dailyRewardText;
    [SerializeField] private SkinShopController shopController;
    [SerializeField] private LeaderboardPanel leaderboardPanel;

    private void Start()
    {
        RefreshLabels();
        AudioManager.Instance?.SetAudioEnabled(PlayerPrefs.GetInt(SoundPrefKey, 1) == 1);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);

        // Apply saved settings volumes
        if (SettingsManager.Instance != null)
        {
            AudioManager.Instance?.SetMusicVolume(SettingsManager.Instance.MusicVolume);
            AudioManager.Instance?.SetSfxVolume(SettingsManager.Instance.SfxVolume);
        }

        RefreshDailyReward();
        RefreshPlayerInfo();
    }

    public void Configure(GameObject panel, Text soundText, Text vibrationText)
    {
        settingsPanel = panel;
        soundValueText = soundText;
        vibrationValueText = vibrationText;
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
        int nextValue = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1 ? 0 : 1;
        PlayerPrefs.SetInt(SoundPrefKey, nextValue);
        AudioManager.Instance?.SetAudioEnabled(nextValue == 1);
        RefreshLabels();
    }

    public void ToggleVibration()
    {
        int nextValue = PlayerPrefs.GetInt(VibrationPrefKey, 1) == 1 ? 0 : 1;
        PlayerPrefs.SetInt(VibrationPrefKey, nextValue);
        SettingsManager.Instance?.SetVibration(nextValue == 1);
        RefreshLabels();
    }

    public void QuitGame()
    {
        CloudSaveManager.Instance?.SaveProgress();
        Application.Quit();
    }

    private void RefreshLabels()
    {
        if (soundValueText != null)
        {
            soundValueText.text = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1 ? "On" : "Off";
        }

        if (vibrationValueText != null)
        {
            vibrationValueText.text = PlayerPrefs.GetInt(VibrationPrefKey, 1) == 1 ? "On" : "Off";
        }
    }

    private void RefreshDailyReward()
    {
        if (dailyRewardText == null || DailyRewardSystem.Instance == null) return;

        if (DailyRewardSystem.Instance.CanClaimToday)
        {
            dailyRewardText.text = $"CLAIM {DailyRewardSystem.Instance.TodayReward} credits!";
            dailyRewardText.color = Color.yellow;
        }
        else
        {
            dailyRewardText.text = "Claimed today ✓";
            dailyRewardText.color = Color.green;
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
            currencyText.text = $"{ProgressionManager.Instance.SoftCurrency}";
        }
    }
}
