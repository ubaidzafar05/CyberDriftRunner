using UnityEngine;
using UnityEngine.UI;

public sealed class ReviveOverlayController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text statusText;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button skipButton;

    private bool _listenersAttached;

    public void Configure(GameObject targetPanel, Text targetText, Button watchButton, Button declineButton)
    {
        panel = targetPanel;
        statusText = targetText;
        watchAdButton = watchButton;
        skipButton = declineButton;
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

    private void Update()
    {
        bool visible = GameManager.Instance != null && GameManager.Instance.State == GameState.RevivePrompt;
        SetVisible(visible);
        if (visible && statusText != null)
        {
            statusText.text = "Watch a rewarded ad to continue this run once.";
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
    }
}
