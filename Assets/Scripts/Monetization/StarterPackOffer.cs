using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time starter pack offer shown after the player's 3rd run.
/// Offers a premium skin + bonus credits at a discounted price.
/// Uses loss aversion + endowed progress psychology.
/// </summary>
public sealed class StarterPackOffer : MonoBehaviour
{
    public static StarterPackOffer Instance { get; private set; }

    private const string ShownKey = "cdr.starterpack.shown";
    private const string PurchasedKey = "cdr.starterpack.purchased";
    private const int TriggerAfterRuns = 3;
    private const string ProductId = "com.cyberdrift.starterpack";
    private const int BonusCredits = 500;

    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button dismissButton;

    private bool _hasBeenShown;
    private bool _hasPurchased;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _hasBeenShown = PlayerPrefs.GetInt(ShownKey, 0) == 1;
        _hasPurchased = PlayerPrefs.GetInt(PurchasedKey, 0) == 1;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void TryShowAfterRun()
    {
        if (_hasBeenShown || _hasPurchased)
        {
            return;
        }

        if (ProgressionManager.Instance == null || ProgressionManager.Instance.TotalRuns < TriggerAfterRuns)
        {
            return;
        }

        Show();
    }

    private void Show()
    {
        _hasBeenShown = true;
        PlayerPrefs.SetInt(ShownKey, 1);
        PlayerPrefs.Save();

        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);

        if (titleText != null)
        {
            titleText.text = "STARTER PACK";
        }

        if (descriptionText != null)
        {
            descriptionText.text = $"Exclusive Neon Skin\n+ {BonusCredits} Credits\n\nOne-time offer!";
        }

        if (priceText != null)
        {
            priceText.text = "$0.99";
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Purchase);
        }

        if (dismissButton != null)
        {
            dismissButton.onClick.RemoveAllListeners();
            dismissButton.onClick.AddListener(Dismiss);
        }
    }

    private void Purchase()
    {
        if (MonetizationManager.Instance != null)
        {
            MonetizationManager.Instance.Purchase(ProductId, OnPurchaseResult);
        }
        else
        {
            // No store — grant anyway for testing
            GrantRewards();
            Dismiss();
        }
    }

    private void OnPurchaseResult(bool success)
    {
        if (success)
        {
            GrantRewards();
        }

        Dismiss();
    }

    private void GrantRewards()
    {
        _hasPurchased = true;
        PlayerPrefs.SetInt(PurchasedKey, 1);
        PlayerPrefs.Save();

        ProgressionManager.Instance?.AddSoftCurrency(BonusCredits);
        ProgressionManager.Instance?.UnlockSkin("neon_starter");
        ProgressionManager.Instance?.SelectSkin("neon_starter");
        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateHeavy();
    }

    private void Dismiss()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Configure(GameObject offerPanel, Text title, Text description, Text price, Button buy, Button dismiss)
    {
        panel = offerPanel;
        titleText = title;
        descriptionText = description;
        priceText = price;
        buyButton = buy;
        dismissButton = dismiss;
    }
}
