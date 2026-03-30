using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full settings panel UI with all user preferences.
/// Wired to SettingsManager for persistence.
/// </summary>
public sealed class SettingsUIController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text musicValueText;
    [SerializeField] private Text sfxValueText;

    [Header("Gameplay")]
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle autoShootToggle;
    [SerializeField] private Toggle leftHandedToggle;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Text sensitivityValueText;

    [Header("Graphics")]
    [SerializeField] private Text qualityLabel;
    [SerializeField] private Toggle showFpsToggle;

    [Header("Account")]
    [SerializeField] private Text signInStatusText;
    [SerializeField] private Button signInButton;
    [SerializeField] private Button signOutButton;
    [SerializeField] private Text lastSyncText;
    [SerializeField] private Button cloudSaveButton;
    [SerializeField] private Button cloudLoadButton;

    [Header("Misc")]
    [SerializeField] private Toggle notificationsToggle;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    private static readonly string[] QualityNames = { "Low", "Medium", "High" };

    private void OnEnable()
    {
        RefreshAll();
    }

    public void Configure(
        Slider music, Slider sfx, Text musicVal, Text sfxVal,
        Toggle vibration, Toggle shake, Toggle autoShoot, Toggle leftHanded,
        Slider sensitivity, Text sensitivityVal,
        Text quality, Toggle fps,
        Text signInStatus, Button signIn, Button signOut,
        Text lastSync, Button cloudSave, Button cloudLoad,
        Toggle notifications, Button reset, Button close)
    {
        musicSlider = music;
        sfxSlider = sfx;
        musicValueText = musicVal;
        sfxValueText = sfxVal;
        vibrationToggle = vibration;
        screenShakeToggle = shake;
        autoShootToggle = autoShoot;
        leftHandedToggle = leftHanded;
        sensitivitySlider = sensitivity;
        sensitivityValueText = sensitivityVal;
        qualityLabel = quality;
        showFpsToggle = fps;
        signInStatusText = signInStatus;
        signInButton = signIn;
        signOutButton = signOut;
        lastSyncText = lastSync;
        cloudSaveButton = cloudSave;
        cloudLoadButton = cloudLoad;
        notificationsToggle = notifications;
        resetButton = reset;
        closeButton = close;

        WireListeners();
    }

    private void WireListeners()
    {
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        if (screenShakeToggle != null) screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
        if (autoShootToggle != null) autoShootToggle.onValueChanged.AddListener(OnAutoShootChanged);
        if (leftHandedToggle != null) leftHandedToggle.onValueChanged.AddListener(OnLeftHandedChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (showFpsToggle != null) showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
        if (notificationsToggle != null) notificationsToggle.onValueChanged.AddListener(OnNotificationsChanged);

        if (signInButton != null) signInButton.onClick.AddListener(OnSignIn);
        if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOut);
        if (cloudSaveButton != null) cloudSaveButton.onClick.AddListener(OnCloudSave);
        if (cloudLoadButton != null) cloudLoadButton.onClick.AddListener(OnCloudLoad);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void RefreshAll()
    {
        if (SettingsManager.Instance == null) return;

        var s = SettingsManager.Instance;
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.SfxVolume);
        if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(s.MusicVolume * 100)}%";
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(s.SfxVolume * 100)}%";
        if (vibrationToggle != null) vibrationToggle.SetIsOnWithoutNotify(s.VibrationEnabled);
        if (screenShakeToggle != null) screenShakeToggle.SetIsOnWithoutNotify(s.ScreenShakeEnabled);
        if (autoShootToggle != null) autoShootToggle.SetIsOnWithoutNotify(s.AutoShootEnabled);
        if (leftHandedToggle != null) leftHandedToggle.SetIsOnWithoutNotify(s.LeftHandedMode);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(s.SwipeSensitivity);
        if (sensitivityValueText != null) sensitivityValueText.text = $"{s.SwipeSensitivity:0.0}x";
        if (qualityLabel != null) qualityLabel.text = QualityNames[s.QualityLevel];
        if (showFpsToggle != null) showFpsToggle.SetIsOnWithoutNotify(s.ShowFps);
        if (notificationsToggle != null) notificationsToggle.SetIsOnWithoutNotify(s.NotificationsEnabled);

        RefreshAccountUI();
    }

    private void RefreshAccountUI()
    {
        bool signedIn = GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn;
        if (signInStatusText != null)
        {
            signInStatusText.text = signedIn
                ? $"Signed in as {GooglePlayManager.Instance.PlayerName}"
                : "Not signed in";
        }

        if (signInButton != null) signInButton.gameObject.SetActive(!signedIn);
        if (signOutButton != null) signOutButton.gameObject.SetActive(signedIn);
        if (cloudSaveButton != null) cloudSaveButton.interactable = signedIn;
        if (cloudLoadButton != null) cloudLoadButton.interactable = signedIn;

        if (lastSyncText != null && CloudSaveManager.Instance != null)
        {
            lastSyncText.text = $"Last sync: {CloudSaveManager.Instance.LastSyncTime}";
        }
    }

    // Event handlers
    private void OnMusicChanged(float val)
    {
        SettingsManager.Instance?.SetMusicVolume(val);
        if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    private void OnSfxChanged(float val)
    {
        SettingsManager.Instance?.SetSfxVolume(val);
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    private void OnVibrationChanged(bool val) => SettingsManager.Instance?.SetVibration(val);
    private void OnScreenShakeChanged(bool val) => SettingsManager.Instance?.SetScreenShake(val);
    private void OnAutoShootChanged(bool val) => SettingsManager.Instance?.SetAutoShoot(val);
    private void OnLeftHandedChanged(bool val) => SettingsManager.Instance?.SetLeftHanded(val);
    private void OnShowFpsChanged(bool val) => SettingsManager.Instance?.SetShowFps(val);
    private void OnNotificationsChanged(bool val) => SettingsManager.Instance?.SetNotifications(val);

    private void OnSensitivityChanged(float val)
    {
        SettingsManager.Instance?.SetSwipeSensitivity(val);
        if (sensitivityValueText != null) sensitivityValueText.text = $"{val:0.0}x";
    }

    public void CycleQuality()
    {
        if (SettingsManager.Instance == null) return;
        int next = (SettingsManager.Instance.QualityLevel + 1) % 3;
        SettingsManager.Instance.SetQuality(next);
        if (qualityLabel != null) qualityLabel.text = QualityNames[next];
    }

    private void OnSignIn() => GooglePlayManager.Instance?.SignIn();
    private void OnSignOut() => GooglePlayManager.Instance?.SignOut();
    private void OnCloudSave() => CloudSaveManager.Instance?.SaveProgress();
    private void OnCloudLoad() => CloudSaveManager.Instance?.LoadProgress();

    private void OnReset()
    {
        SettingsManager.Instance?.ResetToDefaults();
        RefreshAll();
    }
}
