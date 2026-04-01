using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Text instructionText;
    [SerializeField] private Button dismissButton;

    private const string TutorialCompletedKey = "cdr.tutorial.done";
    private static readonly string[] Steps =
    {
        "SWIPE LEFT/RIGHT to switch lanes",
        "SWIPE UP to jump, SWIPE DOWN to slide",
        "TAP to shoot nearby drones",
        "HOLD the HACK button for slow-motion hacking",
        "Collect credits and power-ups to boost your run!",
        "Ready? Let's go!"
    };

    private int _currentStep;
    private bool _isActive;

    public bool IsShowingTutorial => _isActive;

    private void Start()
    {
        if (PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            return;
        }

        ShowTutorial();
    }

    public void ShowTutorial()
    {
        _currentStep = 0;
        _isActive = true;
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
        }

        ShowCurrentStep();

        if (dismissButton != null)
        {
            dismissButton.onClick.RemoveAllListeners();
            dismissButton.onClick.AddListener(AdvanceStep);
        }

        GameManager.Instance?.TryPauseRun();
    }

    private void AdvanceStep()
    {
        _currentStep++;
        if (_currentStep >= Steps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
        HapticFeedback.Instance?.VibrateLight();
    }

    private void ShowCurrentStep()
    {
        if (instructionText != null)
        {
            instructionText.text = Steps[_currentStep];
        }
    }

    private void CompleteTutorial()
    {
        _isActive = false;
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        GameManager.Instance?.ResumeRun();
    }
}
