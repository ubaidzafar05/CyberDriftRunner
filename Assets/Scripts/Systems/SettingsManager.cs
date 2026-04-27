using UnityEngine;

public sealed class SettingsManager : MonoBehaviour
{
    private const string MusicVolKey = "cdr.settings.musicVol";
    private const string SfxVolKey = "cdr.settings.sfxVol";
    private const string AudioEnabledKey = "cdr.settings.audioEnabled";
    private const string VibrationKey = "cdr.vibration";
    private const string QualityKey = "cdr.settings.quality";
    private const string SensitivityKey = "cdr.settings.sensitivity";
    private const string LanguageKey = "cdr.settings.language";
    private const string ScreenShakeKey = "cdr.settings.screenShake";
    private const string ShowFpsKey = "cdr.settings.showFps";
    private const string LeftHandedKey = "cdr.settings.leftHanded";
    private const string NotificationsKey = "cdr.settings.notifications";
    private const string AutoShootKey = "cdr.settings.autoShoot";
    private const string LegacySoundKey = "cdr.sound";

    public static SettingsManager Instance { get; private set; }

    [SerializeField] private VisualQualityConfig qualityConfig;

    public float MusicVolume { get; private set; } = 0.7f;
    public float SfxVolume { get; private set; } = 1f;
    public bool AudioEnabled { get; private set; } = true;
    public bool VibrationEnabled { get; private set; } = true;
    public int QualityLevel { get; private set; } = 1;
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
        MigrateLegacyPrefs();
        ApplyRuntimeSettings();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        SaveAndNotify();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        SaveAndNotify();
    }

    public void SetAudioEnabled(bool enabled)
    {
        AudioEnabled = enabled;
        SaveAndNotify();
    }

    public void SetVibration(bool enabled)
    {
        VibrationEnabled = enabled;
        SaveAndNotify();
    }

    public void SetQuality(int level)
    {
        QualityLevel = Mathf.Clamp(level, 0, 2);
        SaveAndNotify();
    }

    public void SetSwipeSensitivity(float sensitivity)
    {
        SwipeSensitivity = Mathf.Clamp(sensitivity, 0.5f, 2f);
        SaveAndNotify();
    }

    public void SetScreenShake(bool enabled)
    {
        ScreenShakeEnabled = enabled;
        SaveAndNotify();
    }

    public void SetShowFps(bool show)
    {
        ShowFps = show;
        SaveAndNotify();
    }

    public void SetLeftHanded(bool leftHanded)
    {
        LeftHandedMode = leftHanded;
        SaveAndNotify();
    }

    public void SetNotifications(bool enabled)
    {
        NotificationsEnabled = enabled;
        SaveAndNotify();
    }

    public void SetAutoShoot(bool enabled)
    {
        AutoShootEnabled = enabled;
        SaveAndNotify();
    }

    public void ResetToDefaults()
    {
        MusicVolume = 0.7f;
        SfxVolume = 1f;
        AudioEnabled = true;
        VibrationEnabled = true;
        QualityLevel = 1;
        SwipeSensitivity = 1f;
        ScreenShakeEnabled = true;
        ShowFps = false;
        LeftHandedMode = false;
        NotificationsEnabled = true;
        AutoShootEnabled = false;
        Language = "en";
        SaveAndNotify();
    }

    private void ApplyRuntimeSettings()
    {
        ApplyQuality();
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetAudioEnabled(AudioEnabled);
            AudioManager.Instance.SetMusicVolume(MusicVolume);
            AudioManager.Instance.SetSfxVolume(SfxVolume);
        }
    }

    private void ApplyQuality()
    {
        VisualQualityConfig.Tier tier = qualityConfig != null ? qualityConfig.GetTier(QualityLevel) : null;
        if (tier != null)
        {
            int qualityIndex = Mathf.Clamp(tier.unityQualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            QualitySettings.SetQualityLevel(qualityIndex, true);
            Application.targetFrameRate = tier.targetFrameRate;
            QualitySettings.shadows = tier.shadowQuality;
            QualitySettings.antiAliasing = tier.antiAliasing;
            return;
        }

        switch (QualityLevel)
        {
            case 0:
                QualitySettings.SetQualityLevel(1, true);
                Application.targetFrameRate = 45;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 0;
                break;
            case 1:
                QualitySettings.SetQualityLevel(3, true);
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.antiAliasing = 0;
                break;
            default:
                QualitySettings.SetQualityLevel(4, true);
                Application.targetFrameRate = 60;
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.antiAliasing = 2;
                break;
        }
    }

    private void Load()
    {
        MusicVolume = SecurePrefs.GetFloat(MusicVolKey, 0.7f);
        SfxVolume = SecurePrefs.GetFloat(SfxVolKey, 1f);
        AudioEnabled = SecurePrefs.GetBool(AudioEnabledKey, true);
        VibrationEnabled = SecurePrefs.GetBool(VibrationKey, true);
        QualityLevel = SecurePrefs.GetInt(QualityKey, 1);
        SwipeSensitivity = SecurePrefs.GetFloat(SensitivityKey, 1f);
        ScreenShakeEnabled = SecurePrefs.GetBool(ScreenShakeKey, true);
        ShowFps = SecurePrefs.GetBool(ShowFpsKey, false);
        LeftHandedMode = SecurePrefs.GetBool(LeftHandedKey, false);
        NotificationsEnabled = SecurePrefs.GetBool(NotificationsKey, true);
        AutoShootEnabled = SecurePrefs.GetBool(AutoShootKey, false);
        Language = SecurePrefs.GetString(LanguageKey, "en");
    }

    private void MigrateLegacyPrefs()
    {
        if (!PlayerPrefs.HasKey(LegacySoundKey))
        {
            return;
        }

        if (!PlayerPrefs.HasKey(AudioEnabledKey))
        {
            AudioEnabled = PlayerPrefs.GetInt(LegacySoundKey, 1) == 1;
        }

        PlayerPrefs.DeleteKey(LegacySoundKey);
        Save();
    }

    private void SaveAndNotify()
    {
        Save();
        ApplyRuntimeSettings();
        OnSettingsChanged?.Invoke();
    }

    private void Save()
    {
        SecurePrefs.SetFloat(MusicVolKey, MusicVolume);
        SecurePrefs.SetFloat(SfxVolKey, SfxVolume);
        SecurePrefs.SetBool(AudioEnabledKey, AudioEnabled);
        SecurePrefs.SetBool(VibrationKey, VibrationEnabled);
        SecurePrefs.SetInt(QualityKey, QualityLevel);
        SecurePrefs.SetFloat(SensitivityKey, SwipeSensitivity);
        SecurePrefs.SetBool(ScreenShakeKey, ScreenShakeEnabled);
        SecurePrefs.SetBool(ShowFpsKey, ShowFps);
        SecurePrefs.SetBool(LeftHandedKey, LeftHandedMode);
        SecurePrefs.SetBool(NotificationsKey, NotificationsEnabled);
        SecurePrefs.SetBool(AutoShootKey, AutoShootEnabled);
        SecurePrefs.SetString(LanguageKey, Language);
        SecurePrefs.Save();
    }
}
