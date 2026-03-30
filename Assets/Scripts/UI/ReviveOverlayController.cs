using UnityEngine;
using UnityEngine.UI;

public sealed class ReviveOverlayController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text statusText;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button skipButton;

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
        watchAdButton.onClick.AddListener(WatchAd);
        skipButton.onClick.AddListener(Skip);
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

    private void WatchAd()
    {
        GameManager.Instance.AcceptReviveOffer();
    }

    private void Skip()
    {
        GameManager.Instance.DeclineReviveOffer();
    }

    private void SetVisible(bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }
    }
}
