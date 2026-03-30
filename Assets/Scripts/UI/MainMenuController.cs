using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private const string SoundPrefKey = "cdr.sound";
    private const string VibrationPrefKey = "cdr.vibration";

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Text soundValueText;
    [SerializeField] private Text vibrationValueText;
    [SerializeField] private SkinShopController shopController;

    private void Start()
    {
        RefreshLabels();
        AudioManager.Instance?.SetAudioEnabled(PlayerPrefs.GetInt(SoundPrefKey, 1) == 1);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void Configure(GameObject panel, Text soundText, Text vibrationText)
    {
        settingsPanel = panel;
        soundValueText = soundText;
        vibrationValueText = vibrationText;
    }

    public void Play()
    {
        GameManager.Instance.StartRun();
    }

    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    public void ToggleShop()
    {
        shopController?.TogglePanel();
    }

    public void BindShop(SkinShopController controller)
    {
        shopController = controller;
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
        RefreshLabels();
    }

    public void QuitGame()
    {
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
}
