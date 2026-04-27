using UnityEngine;

public sealed class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }

    private const string PremiumCurrencyKey = "cdr.economy.premium";

    public int Credits => ProgressionManager.Instance != null ? ProgressionManager.Instance.SoftCurrency : 0;
    public int PremiumCurrency { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PremiumCurrency = SecurePrefs.GetInt(PremiumCurrencyKey, 0);
    }

    public void AddCredits(int amount, string source = null)
    {
        if (amount <= 0)
        {
            return;
        }

        ProgressionManager.Instance?.AddSoftCurrency(amount);
        PublishCurrencyChanged();
    }

    public bool SpendCredits(int amount, string sink = null)
    {
        if (amount <= 0 || Credits < amount)
        {
            return false;
        }

        ProgressionManager.Instance?.AddSoftCurrency(-amount);
        PublishCurrencyChanged();

        return true;
    }

    public void AddPremiumCurrency(int amount, string source = null)
    {
        if (amount <= 0)
        {
            return;
        }

        PremiumCurrency += amount;
        Save();
        PublishCurrencyChanged();
    }

    public bool SpendPremiumCurrency(int amount, string sink = null)
    {
        if (amount <= 0 || PremiumCurrency < amount)
        {
            return false;
        }

        PremiumCurrency -= amount;
        Save();
        PublishCurrencyChanged();

        return true;
    }

    public bool CanAfford(ShopCurrencyType currencyType, int price)
    {
        switch (currencyType)
        {
            case ShopCurrencyType.SoftCurrency:
                return Credits >= price;
            case ShopCurrencyType.PremiumCurrency:
                return PremiumCurrency >= price;
            default:
                return true;
        }
    }

    public void PublishCurrencyChanged()
    {
        EventBus.Publish(new CurrencyChangedEvent(Credits, PremiumCurrency));
    }

    private void Save()
    {
        SecurePrefs.SetInt(PremiumCurrencyKey, PremiumCurrency);
        SecurePrefs.Save();
    }
}
