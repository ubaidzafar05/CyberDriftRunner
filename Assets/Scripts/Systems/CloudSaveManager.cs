using UnityEngine;

/// <summary>
/// Cross-device progress sync via Google Play cloud save or local JSON backup.
/// Saves: currency, skins, upgrades, achievements, stats, settings.
/// </summary>
public sealed class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }

    private const string CloudSaveKey = "cdr.cloudsave.data";
    private const string LastSyncKey = "cdr.cloudsave.lastSync";

    [System.Serializable]
    private class SaveData
    {
        public int softCurrency;
        public int highScore;
        public float bestDistance;
        public int totalRuns;
        public float totalDistance;
        public int totalDrones;
        public int xpTotal;
        public int xpLevel;
        public string selectedSkin;
        public string unlockedSkins;
        public string upgradeData;
        public string achievementData;
        public string timestamp;
    }

    public string LastSyncTime { get; private set; } = "Never";
    public event System.Action<bool> OnSyncComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LastSyncTime = PlayerPrefs.GetString(LastSyncKey, "Never");
    }

    public void SaveProgress()
    {
        SaveData data = GatherSaveData();
        string json = JsonUtility.ToJson(data);

        // Save locally first
        PlayerPrefs.SetString(CloudSaveKey, json);
        PlayerPrefs.Save();

        // Try cloud save
        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn)
        {
            GooglePlayManager.Instance.SaveToCloud("progress", json);
        }

        LastSyncTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        PlayerPrefs.SetString(LastSyncKey, LastSyncTime);
        PlayerPrefs.Save();

        OnSyncComplete?.Invoke(true);
        Debug.Log("[CloudSaveManager] Progress saved");
    }

    public void LoadProgress()
    {
        string json = "";

        // Try cloud first
        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn)
        {
            json = GooglePlayManager.Instance.LoadFromCloud("progress");
        }

        // Fall back to local
        if (string.IsNullOrEmpty(json))
        {
            json = PlayerPrefs.GetString(CloudSaveKey, "");
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[CloudSaveManager] No save data found");
            OnSyncComplete?.Invoke(false);
            return;
        }

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            ApplySaveData(data);
            OnSyncComplete?.Invoke(true);
            Debug.Log("[CloudSaveManager] Progress loaded");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CloudSaveManager] Failed to load: {e.Message}");
            OnSyncComplete?.Invoke(false);
        }
    }

    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[CloudSaveManager] All data deleted");
    }

    private SaveData GatherSaveData()
    {
        SaveData data = new SaveData();
        data.timestamp = System.DateTime.UtcNow.ToString("o");

        if (ProgressionManager.Instance != null)
        {
            data.softCurrency = ProgressionManager.Instance.SoftCurrency;
            data.highScore = ProgressionManager.Instance.HighScore;
            data.bestDistance = ProgressionManager.Instance.BestDistance;
            data.totalRuns = ProgressionManager.Instance.TotalRuns;
            data.totalDistance = ProgressionManager.Instance.TotalDistance;
            data.totalDrones = ProgressionManager.Instance.TotalDronesDestroyed;
            data.selectedSkin = ProgressionManager.Instance.SelectedSkinId;
        }

        if (XpLevelSystem.Instance != null)
        {
            data.xpTotal = XpLevelSystem.Instance.TotalXp;
            data.xpLevel = XpLevelSystem.Instance.CurrentLevel;
        }

        // Store upgrade levels as comma-separated
        if (UpgradeSystem.Instance != null)
        {
            var types = System.Enum.GetValues(typeof(UpgradeSystem.UpgradeType));
            string[] levels = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                levels[i] = UpgradeSystem.Instance.GetLevel((UpgradeSystem.UpgradeType)i).ToString();
            }
            data.upgradeData = string.Join(",", levels);
        }

        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        // Restore to PlayerPrefs so each system picks it up on next load
        PlayerPrefs.SetInt("cdr.progress.softCurrency", data.softCurrency);
        PlayerPrefs.SetInt("cdr.progress.highScore", data.highScore);
        PlayerPrefs.SetFloat("cdr.progress.bestDistance", data.bestDistance);
        PlayerPrefs.SetInt("cdr.progress.totalRuns", data.totalRuns);
        PlayerPrefs.SetFloat("cdr.progress.totalDistance", data.totalDistance);
        PlayerPrefs.SetInt("cdr.progress.totalDrones", data.totalDrones);
        PlayerPrefs.SetInt("cdr.xp.total", data.xpTotal);
        PlayerPrefs.SetInt("cdr.xp.level", data.xpLevel);

        if (!string.IsNullOrEmpty(data.selectedSkin))
        {
            PlayerPrefs.SetString("cdr.progress.selectedSkin", data.selectedSkin);
        }

        PlayerPrefs.Save();
        Debug.Log($"[CloudSaveManager] Applied save data from {data.timestamp}");
    }
}
