using System.Collections.Generic;
using UnityEngine;

public sealed class ProgressionManager : MonoBehaviour
{
    private const string SoftCurrencyKey = "cdr.progress.softCurrency";
    private const string SelectedSkinKey = "cdr.progress.selectedSkin";
    private const string UnlockedSkinsKey = "cdr.progress.unlockedSkins";
    private const string HighScoreKey = "cdr.progress.highScore";
    private const string BestDistanceKey = "cdr.progress.bestDistance";
    private const string TotalRunsKey = "cdr.progress.totalRuns";
    private const string TotalDistanceKey = "cdr.progress.totalDistance";
    private const string TotalDronesKey = "cdr.progress.totalDrones";

    public static ProgressionManager Instance { get; private set; }

    private readonly HashSet<string> _unlockedSkinIds = new HashSet<string>();
    private SkinDefinition[] _skins;

    public int SoftCurrency { get; private set; }
    public string SelectedSkinId { get; private set; }
    public IReadOnlyList<SkinDefinition> Skins => _skins;
    public int HighScore { get; private set; }
    public float BestDistance { get; private set; }
    public int TotalRuns { get; private set; }
    public float TotalDistance { get; private set; }
    public int TotalDronesDestroyed { get; private set; }
    public IEnumerable<string> UnlockedSkinIds => _unlockedSkinIds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _skins = SkinCatalog.CreateDefaultCatalog();
        Load();
    }

    public SkinDefinition GetSelectedSkin()
    {
        return FindSkin(SelectedSkinId) ?? _skins[0];
    }

    public bool IsUnlocked(string skinId)
    {
        return !string.IsNullOrWhiteSpace(skinId) && _unlockedSkinIds.Contains(skinId);
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

        _unlockedSkinIds.Add(skinId);
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
        if (amount == 0)
        {
            return;
        }

        SoftCurrency += amount;
        if (SoftCurrency < 0) SoftCurrency = 0;
        Save();
        EventBus.Publish(new CurrencyChangedEvent(SoftCurrency, EconomySystem.Instance != null ? EconomySystem.Instance.PremiumCurrency : 0));
    }

    public bool CommitRunStats(RunSummary summary)
    {
        bool newHighScore = summary.Score > HighScore;
        if (newHighScore)
        {
            HighScore = summary.Score;
        }

        if (summary.Distance > BestDistance)
        {
            BestDistance = summary.Distance;
        }

        TotalRuns++;
        TotalDistance += summary.Distance;
        Save();
        return newHighScore;
    }

    public void AddDronesDestroyed(int count)
    {
        TotalDronesDestroyed += Mathf.Max(0, count);
    }

    private void Load()
    {
        SoftCurrency = SecurePrefs.GetInt(SoftCurrencyKey, 0);
        SelectedSkinId = SecurePrefs.GetString(SelectedSkinKey, _skins[0].Id);
        HighScore = SecurePrefs.GetInt(HighScoreKey, 0);
        BestDistance = SecurePrefs.GetFloat(BestDistanceKey, 0f);
        TotalRuns = SecurePrefs.GetInt(TotalRunsKey, 0);
        TotalDistance = SecurePrefs.GetFloat(TotalDistanceKey, 0f);
        TotalDronesDestroyed = SecurePrefs.GetInt(TotalDronesKey, 0);
        _unlockedSkinIds.Clear();

        string[] tokens = SecurePrefs.GetString(UnlockedSkinsKey, _skins[0].Id).Split('|');
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tokens[i]))
            {
                _unlockedSkinIds.Add(tokens[i]);
            }
        }

        UnlockSkin(_skins[0].Id);
        if (!IsUnlocked(SelectedSkinId))
        {
            SelectedSkinId = _skins[0].Id;
        }
    }

    private void Save()
    {
        SecurePrefs.SetInt(SoftCurrencyKey, SoftCurrency);
        SecurePrefs.SetString(SelectedSkinKey, SelectedSkinId);
        SecurePrefs.SetString(UnlockedSkinsKey, string.Join("|", _unlockedSkinIds));
        SecurePrefs.SetInt(HighScoreKey, HighScore);
        SecurePrefs.SetFloat(BestDistanceKey, BestDistance);
        SecurePrefs.SetInt(TotalRunsKey, TotalRuns);
        SecurePrefs.SetFloat(TotalDistanceKey, TotalDistance);
        SecurePrefs.SetInt(TotalDronesKey, TotalDronesDestroyed);
        SecurePrefs.Save();
    }

    private SkinDefinition FindSkin(string skinId)
    {
        for (int i = 0; i < _skins.Length; i++)
        {
            if (_skins[i].Id == skinId)
            {
                return _skins[i];
            }
        }

        return null;
    }
}
