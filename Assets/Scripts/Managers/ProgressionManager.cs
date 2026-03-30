using System.Collections.Generic;
using UnityEngine;

public sealed class ProgressionManager : MonoBehaviour
{
    private const string SoftCurrencyKey = "cdr.progress.softCurrency";
    private const string SelectedSkinKey = "cdr.progress.selectedSkin";
    private const string UnlockedSkinsKey = "cdr.progress.unlockedSkins";

    public static ProgressionManager Instance { get; private set; }

    private readonly HashSet<string> unlockedSkinIds = new HashSet<string>();
    private SkinDefinition[] skins;

    public int SoftCurrency { get; private set; }
    public string SelectedSkinId { get; private set; }
    public IReadOnlyList<SkinDefinition> Skins => skins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        skins = SkinCatalog.CreateDefaultCatalog();
        Load();
    }

    public SkinDefinition GetSelectedSkin()
    {
        return FindSkin(SelectedSkinId) ?? skins[0];
    }

    public bool IsUnlocked(string skinId)
    {
        return !string.IsNullOrWhiteSpace(skinId) && unlockedSkinIds.Contains(skinId);
    }

    public bool TryUnlockWithSoftCurrency(string skinId)
    {
        SkinDefinition skin = FindSkin(skinId);
        if (skin == null || skin.IsPremium || IsUnlocked(skinId) || SoftCurrency < skin.SoftCurrencyCost)
        {
            return false;
        }

        SoftCurrency -= skin.SoftCurrencyCost;
        UnlockSkin(skinId);
        Save();
        return true;
    }

    public void UnlockSkin(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return;
        }

        unlockedSkinIds.Add(skinId);
        if (string.IsNullOrWhiteSpace(SelectedSkinId))
        {
            SelectedSkinId = skinId;
        }

        Save();
    }

    public void SelectSkin(string skinId)
    {
        if (!IsUnlocked(skinId))
        {
            return;
        }

        SelectedSkinId = skinId;
        Save();
    }

    public void AddSoftCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SoftCurrency += amount;
        Save();
    }

    private void Load()
    {
        SoftCurrency = PlayerPrefs.GetInt(SoftCurrencyKey, 0);
        SelectedSkinId = PlayerPrefs.GetString(SelectedSkinKey, skins[0].Id);
        unlockedSkinIds.Clear();

        string[] tokens = PlayerPrefs.GetString(UnlockedSkinsKey, skins[0].Id).Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tokens[i]))
            {
                unlockedSkinIds.Add(tokens[i]);
            }
        }

        UnlockSkin(skins[0].Id);
        if (!IsUnlocked(SelectedSkinId))
        {
            SelectedSkinId = skins[0].Id;
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SoftCurrencyKey, SoftCurrency);
        PlayerPrefs.SetString(SelectedSkinKey, SelectedSkinId);
        PlayerPrefs.SetString(UnlockedSkinsKey, string.Join("|", unlockedSkinIds));
        PlayerPrefs.Save();
    }

    private SkinDefinition FindSkin(string skinId)
    {
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i].Id == skinId)
            {
                return skins[i];
            }
        }

        return null;
    }
}
