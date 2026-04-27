public enum NetworkConnectionState
{
    Offline,
    Connecting,
    Matchmaking,
    InRoom,
    Failed
}

public enum ShopCurrencyType
{
    SoftCurrency,
    PremiumCurrency,
    RealMoney
}

public enum ShopItemType
{
    Skin,
    Trail,
    WeaponSkin,
    CreditPack,
    Utility
}

public readonly struct RunStartedEvent
{
    public RunStartedEvent(int totalRuns)
    {
        TotalRuns = totalRuns;
    }

    public int TotalRuns { get; }
}

public readonly struct RunEndedEvent
{
    public RunEndedEvent(RunSummary summary, string deathReason)
    {
        Summary = summary;
        DeathReason = deathReason;
    }

    public RunSummary Summary { get; }
    public string DeathReason { get; }
}

public readonly struct CurrencyChangedEvent
{
    public CurrencyChangedEvent(int softCurrency, int premiumCurrency)
    {
        SoftCurrency = softCurrency;
        PremiumCurrency = premiumCurrency;
    }

    public int SoftCurrency { get; }
    public int PremiumCurrency { get; }
}

public readonly struct LiveOpsConfigChangedEvent
{
    public LiveOpsConfigChangedEvent(RemoteGameConfig config)
    {
        Config = config;
    }

    public RemoteGameConfig Config { get; }
}

public readonly struct ShopItemPurchasedEvent
{
    public ShopItemPurchasedEvent(string itemId, ShopItemType itemType, ShopCurrencyType currencyType, int price)
    {
        ItemId = itemId;
        ItemType = itemType;
        CurrencyType = currencyType;
        Price = price;
    }

    public string ItemId { get; }
    public ShopItemType ItemType { get; }
    public ShopCurrencyType CurrencyType { get; }
    public int Price { get; }
}

public readonly struct NetworkStateChangedEvent
{
    public NetworkStateChangedEvent(NetworkConnectionState state, bool realtimeEnabled)
    {
        State = state;
        RealtimeEnabled = realtimeEnabled;
    }

    public NetworkConnectionState State { get; }
    public bool RealtimeEnabled { get; }
}
