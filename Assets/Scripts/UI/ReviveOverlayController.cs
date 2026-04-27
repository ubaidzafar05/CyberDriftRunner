using UnityEngine;
using UnityEngine.UI;

public sealed class ReviveOverlayController : MonoBehaviour
{
    public static ReviveOverlayController Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text statusText;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button skipButton;

    private bool _listenersAttached;
    public bool IsReady => panel != null && watchAdButton != null && skipButton != null && panel.scene.IsValid();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateBindings();
    }

    public void Configure(GameObject targetPanel, Text targetText, Button watchButton, Button declineButton)
    {
        panel = targetPanel;
        statusText = targetText;
        watchAdButton = watchButton;
        skipButton = declineButton;
        ValidateBindings();
        EnsurePromptPriority();
    }

    private void Start()
    {
        SetVisible(false);
        AttachListeners();
    }

    private void OnEnable()
    {
        AttachListeners();
    }

    private void OnDisable()
    {
        DetachListeners();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        bool visible = GameManager.Instance != null && GameManager.Instance.State == GameState.RevivePrompt;
        SetVisible(visible);
        if (visible && statusText != null)
        {
            statusText.text = "REVIVE AVAILABLE\nContinue this run once or cash out to debrief.";
        }
    }

    private void AttachListeners()
    {
        if (_listenersAttached)
        {
            return;
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(WatchAd);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(Skip);
        }

        _listenersAttached = true;
    }

    private void DetachListeners()
    {
        if (!_listenersAttached)
        {
            return;
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveListener(WatchAd);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(Skip);
        }

        _listenersAttached = false;
    }

    private void WatchAd()
    {
        GameManager.Instance?.AcceptReviveOffer();
    }

    private void Skip()
    {
        GameManager.Instance?.DeclineReviveOffer();
    }

    private void SetVisible(bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }

        if (visible)
        {
            EnsurePromptPriority();
        }
    }

    public void FocusPrompt()
    {
        if (!ValidateBindings())
        {
            Debug.LogError("[ReviveOverlayController] Revive prompt is not wired correctly. Falling back to game over.");
            GameManager.Instance?.DeclineReviveOffer();
            return;
        }

        EnsurePromptPriority();
        SetVisible(true);
    }

    private void EnsurePromptPriority()
    {
        if (panel == null)
        {
            return;
        }

        panel.transform.SetAsLastSibling();
        Canvas panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas == null)
        {
            panelCanvas = panel.AddComponent<Canvas>();
        }

        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 250;

        if (panel.GetComponent<GraphicRaycaster>() == null)
        {
            panel.AddComponent<GraphicRaycaster>();
        }
    }

    private bool ValidateBindings()
    {
        bool valid = panel != null && watchAdButton != null && skipButton != null;
        if (!valid)
        {
            Debug.LogError("[ReviveOverlayController] Missing authored panel/button bindings.");
        }

        return valid;
    }
}
