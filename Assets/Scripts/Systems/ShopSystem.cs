using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ShopItemDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public ShopItemType ItemType;
    public ShopCurrencyType CurrencyType;
    public int Price;
    public string RewardId;
}

public sealed class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    private const string OwnedTrailsKey = "cdr.shop.ownedTrails";
    private const string OwnedWeaponSkinsKey = "cdr.shop.ownedWeaponSkins";
    private const string SelectedTrailKey = "cdr.shop.selectedTrail";
    private const string SelectedWeaponSkinKey = "cdr.shop.selectedWeaponSkin";

    private static readonly ShopItemDefinition[] Catalog =
    {
        new ShopItemDefinition { Id = "trail_neon_blue", DisplayName = "Neon Blue Trail", Description = "Bright electric trail.", ItemType = ShopItemType.Trail, CurrencyType = ShopCurrencyType.SoftCurrency, Price = 180, RewardId = "trail_neon_blue" },
        new ShopItemDefinition { Id = "trail_void", DisplayName = "Void Trail", Description = "Dark premium trail.", ItemType = ShopItemType.Trail, CurrencyType = ShopCurrencyType.PremiumCurrency, Price = 8, RewardId = "trail_void" },
        new ShopItemDefinition { Id = "weapon_skin_chrome", DisplayName = "Chrome Blaster", Description = "Stylized weapon finish.", ItemType = ShopItemType.WeaponSkin, CurrencyType = ShopCurrencyType.SoftCurrency, Price = 260, RewardId = "weapon_skin_chrome" },
        new ShopItemDefinition { Id = "weapon_skin_overdrive", DisplayName = "Overdrive Cannon", Description = "Legendary premium skin.", ItemType = ShopItemType.WeaponSkin, CurrencyType = ShopCurrencyType.PremiumCurrency, Price = 12, RewardId = "weapon_skin_overdrive" },
        new ShopItemDefinition { Id = "premium_pack_small", DisplayName = "Premium Cache", Description = "Adds 25 premium currency.", ItemType = ShopItemType.CreditPack, CurrencyType = ShopCurrencyType.RealMoney, Price = 1, RewardId = "premium_25" },
        new ShopItemDefinition { Id = "skin_magenta_flux", DisplayName = "Magenta Flux", Description = "Unlock an existing runner skin.", ItemType = ShopItemType.Skin, CurrencyType = ShopCurrencyType.SoftCurrency, Price = 120, RewardId = "magenta_flux" },
        new ShopItemDefinition { Id = "skin_gold_circuit", DisplayName = "Gold Circuit", Description = "Unlock an existing runner skin.", ItemType = ShopItemType.Skin, CurrencyType = ShopCurrencyType.SoftCurrency, Price = 240, RewardId = "gold_circuit" },
        new ShopItemDefinition { Id = "skin_void_ghost", DisplayName = "Void Ghost", Description = "Premium runner skin.", ItemType = ShopItemType.Skin, CurrencyType = ShopCurrencyType.PremiumCurrency, Price = 15, RewardId = "void_ghost" }
    };

    private readonly HashSet<string> _ownedTrails = new HashSet<string>();
    private readonly HashSet<string> _ownedWeaponSkins = new HashSet<string>();

    public IReadOnlyList<ShopItemDefinition> Items => Catalog;
    public string SelectedTrailId { get; private set; } = "trail_default";
    public string SelectedWeaponSkinId { get; private set; } = "weapon_default";

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
    }

    public bool IsOwned(string itemId)
    {
        ShopItemDefinition item = FindItem(itemId);
        if (item == null)
        {
            return false;
        }

        switch (item.ItemType)
        {
            case ShopItemType.Skin:
                return ProgressionManager.Instance != null && ProgressionManager.Instance.IsUnlocked(item.RewardId);
            case ShopItemType.Trail:
                return _ownedTrails.Contains(item.RewardId);
            case ShopItemType.WeaponSkin:
                return _ownedWeaponSkins.Contains(item.RewardId);
            default:
                return false;
        }
    }

    public bool TryPurchase(string itemId)
    {
        ShopItemDefinition item = FindItem(itemId);
        if (item == null)
        {
            return false;
        }

        if (IsOwned(itemId) && item.ItemType != ShopItemType.CreditPack)
        {
            return true;
        }

        if (item.CurrencyType == ShopCurrencyType.RealMoney)
        {
            return ProcessRealMoneyPurchase(item);
        }

        if (EconomySystem.Instance == null || !EconomySystem.Instance.CanAfford(item.CurrencyType, item.Price))
        {
            return false;
        }

        bool paid = item.CurrencyType == ShopCurrencyType.SoftCurrency
            ? EconomySystem.Instance.SpendCredits(item.Price, item.Id)
            : EconomySystem.Instance.SpendPremiumCurrency(item.Price, item.Id);

        if (!paid)
        {
            return false;
        }

        Grant(item);
        return true;
    }

    public bool TrySelect(string itemId)
    {
        ShopItemDefinition item = FindItem(itemId);
        if (item == null || !IsOwned(itemId))
        {
            return false;
        }

        switch (item.ItemType)
        {
            case ShopItemType.Skin:
                ProgressionManager.Instance?.SelectSkin(item.RewardId);
                break;
            case ShopItemType.Trail:
                SelectedTrailId = item.RewardId;
                break;
            case ShopItemType.WeaponSkin:
                SelectedWeaponSkinId = item.RewardId;
                break;
            default:
                return false;
        }

        Save();
        return true;
    }

    private bool ProcessRealMoneyPurchase(ShopItemDefinition item)
    {
        if (MonetizationManager.Instance == null)
        {
            Grant(item);
            return true;
        }

        bool completed = false;
        MonetizationManager.Instance.Purchase(item.Id, succeeded =>
        {
            if (!succeeded)
            {
                return;
            }

            Grant(item);
            completed = true;
        });

        return completed;
    }

    private void Grant(ShopItemDefinition item)
    {
        switch (item.ItemType)
        {
            case ShopItemType.Skin:
                ProgressionManager.Instance?.UnlockSkin(item.RewardId);
                ProgressionManager.Instance?.SelectSkin(item.RewardId);
                break;
            case ShopItemType.Trail:
                _ownedTrails.Add(item.RewardId);
                SelectedTrailId = item.RewardId;
                break;
            case ShopItemType.WeaponSkin:
                _ownedWeaponSkins.Add(item.RewardId);
                SelectedWeaponSkinId = item.RewardId;
                break;
            case ShopItemType.CreditPack:
                if (item.RewardId == "premium_25")
                {
                    EconomySystem.Instance?.AddPremiumCurrency(25, item.Id);
                }
                break;
        }

        Save();
        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateMedium();
        EventBus.Publish(new ShopItemPurchasedEvent(item.Id, item.ItemType, item.CurrencyType, item.Price));
    }

    private void Load()
    {
        LoadSet(OwnedTrailsKey, _ownedTrails);
        LoadSet(OwnedWeaponSkinsKey, _ownedWeaponSkins);
        SelectedTrailId = SecurePrefs.GetString(SelectedTrailKey, "trail_default");
        SelectedWeaponSkinId = SecurePrefs.GetString(SelectedWeaponSkinKey, "weapon_default");
    }

    private void Save()
    {
        SaveSet(OwnedTrailsKey, _ownedTrails);
        SaveSet(OwnedWeaponSkinsKey, _ownedWeaponSkins);
        SecurePrefs.SetString(SelectedTrailKey, SelectedTrailId);
        SecurePrefs.SetString(SelectedWeaponSkinKey, SelectedWeaponSkinId);
        SecurePrefs.Save();
    }

    private static void LoadSet(string key, HashSet<string> target)
    {
        target.Clear();
        string[] tokens = SecurePrefs.GetString(key, string.Empty).Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tokens[i]))
            {
                target.Add(tokens[i]);
            }
        }
    }

    private static void SaveSet(string key, HashSet<string> source)
    {
        SecurePrefs.SetString(key, string.Join("|", source));
    }

    private static ShopItemDefinition FindItem(string itemId)
    {
        for (int i = 0; i < Catalog.Length; i++)
        {
            if (Catalog[i].Id == itemId)
            {
                return Catalog[i];
            }
        }

        return null;
    }
}
