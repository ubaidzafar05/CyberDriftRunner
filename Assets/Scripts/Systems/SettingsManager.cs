using UnityEngine;

public sealed class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string MusicVolKey = "cdr.settings.musicVol";
    private const string SfxVolKey = "cdr.settings.sfxVol";
    private const string VibrationKey = "cdr.vibration";
    private const string QualityKey = "cdr.settings.quality";
    private const string SensitivityKey = "cdr.settings.sensitivity";
    private const string LanguageKey = "cdr.settings.language";
    private const string ScreenShakeKey = "cdr.settings.screenShake";
    private const string ShowFpsKey = "cdr.settings.showFps";
    private const string LeftHandedKey = "cdr.settings.leftHanded";
    private const string NotificationsKey = "cdr.settings.notifications";
    private const string AutoShootKey = "cdr.settings.autoShoot";

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 1f;
    public bool VibrationEnabled { get; private set; } = true;
    public int QualityLevel { get; private set; } = 1; // 0=Low, 1=Medium, 2=High
    public float SwipeSensitivity { get; private set; } = 1f;
    public bool ScreenShakeEnabled { get; private set; } = true;
    public bool ShowFps { get; private set; }
    public bool LeftHandedMode { get; private set; }
    public bool NotificationsEnabled { get; private set; } = true;
    public bool AutoShootEnabled { get; private set; }
    public string Language { get; private set; } = "en";

    public event System.Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        ApplyQuality();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        Save();
        AudioManager.Instance?.SetMusicVolume(MusicVolume);
        OnSettingsChanged?.Invoke();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        Save();
        AudioManager.Instance?.SetSfxVolume(SfxVolume);
        OnSettingsChanged?.Invoke();
    }

    public void SetVibration(bool enabled)
    {
        VibrationEnabled = enabled;
        PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetQuality(int level)
    {
        QualityLevel = Mathf.Clamp(level, 0, 2);
        Save();
        ApplyQuality();
        OnSettingsChanged?.Invoke();
    }

    public void SetSwipeSensitivity(float sensitivity)
    {
        SwipeSensitivity = Mathf.Clamp(sensitivity, 0.5f, 2f);
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetScreenShake(bool enabled)
    {
        ScreenShakeEnabled = enabled;
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetShowFps(bool show)
    {
        ShowFps = show;
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetLeftHanded(bool leftHanded)
    {
        LeftHandedMode = leftHanded;
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetNotifications(bool enabled)
    {
        NotificationsEnabled = enabled;
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetAutoShoot(bool enabled)
    {
        AutoShootEnabled = enabled;
        Save();
        OnSettingsChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        MusicVolume = 0.7f;
        SfxVolume = 1f;
        VibrationEnabled = true;
        QualityLevel = 1;
        SwipeSensitivity = 1f;
        ScreenShakeEnabled = true;
        ShowFps = false;
        LeftHandedMode = false;
        NotificationsEnabled = true;
        AutoShootEnabled = false;
        Language = "en";
        Save();
        ApplyQuality();
        OnSettingsChanged?.Invoke();
    }

    private void ApplyQuality()
    {
        switch (QualityLevel)
        {
            case 0: // Low
                QualitySettings.SetQualityLevel(0);
                Application.targetFrameRate = 30;
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
            case 1: // Medium
                QualitySettings.SetQualityLevel(2);
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                break;
            case 2: // High
                QualitySettings.SetQualityLevel(4);
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.All;
                break;
        }
    }

    private void Load()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolKey, 0.7f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolKey, 1f);
        VibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        QualityLevel = PlayerPrefs.GetInt(QualityKey, 1);
        SwipeSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);
        ScreenShakeEnabled = PlayerPrefs.GetInt(ScreenShakeKey, 1) == 1;
        ShowFps = PlayerPrefs.GetInt(ShowFpsKey, 0) == 1;
        LeftHandedMode = PlayerPrefs.GetInt(LeftHandedKey, 0) == 1;
        NotificationsEnabled = PlayerPrefs.GetInt(NotificationsKey, 1) == 1;
        AutoShootEnabled = PlayerPrefs.GetInt(AutoShootKey, 0) == 1;
        Language = PlayerPrefs.GetString(LanguageKey, "en");
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(MusicVolKey, MusicVolume);
        PlayerPrefs.SetFloat(SfxVolKey, SfxVolume);
        PlayerPrefs.SetInt(VibrationKey, VibrationEnabled ? 1 : 0);
        PlayerPrefs.SetInt(QualityKey, QualityLevel);
        PlayerPrefs.SetFloat(SensitivityKey, SwipeSensitivity);
        PlayerPrefs.SetInt(ScreenShakeKey, ScreenShakeEnabled ? 1 : 0);
        PlayerPrefs.SetInt(ShowFpsKey, ShowFps ? 1 : 0);
        PlayerPrefs.SetInt(LeftHandedKey, LeftHandedMode ? 1 : 0);
        PlayerPrefs.SetInt(NotificationsKey, NotificationsEnabled ? 1 : 0);
        PlayerPrefs.SetInt(AutoShootKey, AutoShootEnabled ? 1 : 0);
        PlayerPrefs.SetString(LanguageKey, Language);
        PlayerPrefs.Save();
    }
}
