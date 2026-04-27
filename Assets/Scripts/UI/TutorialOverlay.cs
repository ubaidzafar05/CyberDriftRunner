using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Text instructionText;
    [SerializeField] private Button dismissButton;
    [SerializeField] private float initialStepDelay = 0.35f;
    [SerializeField] private float stepDuration = 1.8f;
    [SerializeField] [Range(0.15f, 1f)] private float finalStepOverlayAlpha = 0.38f;

    private const string TutorialCompletedKey = "cdr.tutorial.done";
    private static readonly string[] Steps =
    {
        "DRIFT LEFT / RIGHT to line up your route",
        "SWIPE UP to jump // SWIPE DOWN to slide",
        "TAP to blast nearby drones before they crowd you",
        "HOLD HACK to bend time and thread tight gaps",
        "Grab credits + power-ups // build your combo safely",
        "Stay alive through the first gate. Good hunting."
    };

    private int _currentStep;
    private bool _isActive;
    private float _nextAutoAdvanceAt;
    private CanvasGroup _overlayGroup;

    public bool IsShowingTutorial => _isActive;

    private void Start()
    {
        ConfigureInstructionText();
        ConfigureOverlayInteraction();

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
    }

    private void AdvanceStep()
    {
        if (!_isActive)
        {
            return;
        }

        _currentStep++;
        if (_currentStep >= Steps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
        HapticFeedback.Instance?.VibrateLight();
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        if (Time.unscaledTime >= _nextAutoAdvanceAt)
        {
            AdvanceStep();
        }
    }

    private void ShowCurrentStep()
    {
        if (instructionText != null)
        {
            instructionText.text = Steps[_currentStep];
            instructionText.color = _currentStep >= Steps.Length - 1
                ? new Color(1f, 0.92f, 0.35f)
                : Color.white;
        }

        _nextAutoAdvanceAt = Time.unscaledTime + (_currentStep == 0 ? initialStepDelay : stepDuration);
        if (_overlayGroup != null)
        {
            float lateTutorialFade = _currentStep >= Steps.Length - 2 ? finalStepOverlayAlpha : 1f;
            _overlayGroup.alpha = lateTutorialFade;
        }

        if (UIAnimator.Instance != null && instructionText != null)
        {
            UIAnimator.Instance.PunchScale(instructionText.transform, 1.05f, 0.12f);
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
    }

    private void ConfigureInstructionText()
    {
        if (instructionText == null)
        {
            return;
        }

        RectTransform rect = instructionText.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(660f, 190f);
            rect.anchoredPosition = new Vector2(0f, 168f);
        }

        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        instructionText.verticalOverflow = VerticalWrapMode.Overflow;
        instructionText.resizeTextForBestFit = true;
        instructionText.resizeTextMinSize = 24;
        instructionText.resizeTextMaxSize = 42;
        instructionText.supportRichText = true;
    }

    private void ConfigureOverlayInteraction()
    {
        if (overlayRoot == null)
        {
            return;
        }

        _overlayGroup = overlayRoot.GetComponent<CanvasGroup>();
        if (_overlayGroup == null)
        {
            _overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
        }

        Graphic[] graphics = overlayRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            bool keepRaycast = dismissButton != null && graphic.transform.IsChildOf(dismissButton.transform);
            graphic.raycastTarget = keepRaycast;
        }
    }
}
