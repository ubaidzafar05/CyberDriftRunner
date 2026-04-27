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
            public string ghostData;
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
        LastSyncTime = SecurePrefs.GetString(LastSyncKey, "Never");
    }

    public void SaveProgress()
    {
        SaveData data = GatherSaveData();
        string json = JsonUtility.ToJson(data);

        SecurePrefs.SetString(CloudSaveKey, json);
        SecurePrefs.Save();

        // Try cloud save
        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn)
        {
            GooglePlayManager.Instance.SaveToCloud("progress", json);
        }

        LastSyncTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        SecurePrefs.SetString(LastSyncKey, LastSyncTime);
        SecurePrefs.Save();

        OnSyncComplete?.Invoke(true);
        Debug.Log("[CloudSaveManager] Progress saved");
    }

    public void LoadProgress()
    {
        string json = string.Empty;

        // Try cloud first
        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn)
        {
            json = GooglePlayManager.Instance.LoadFromCloud("progress");
        }

        // Fall back to local
        if (string.IsNullOrEmpty(json))
        {
            json = SecurePrefs.GetString(CloudSaveKey, string.Empty);
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[CloudSaveManager] No save data found");
            OnSyncComplete?.Invoke(false);
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("[CloudSaveManager] Failed to load: invalid payload");
            OnSyncComplete?.Invoke(false);
            return;
        }

        ApplySaveData(data);
        OnSyncComplete?.Invoke(true);
        Debug.Log("[CloudSaveManager] Progress loaded");
    }

    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SecurePrefs.DeleteKey(CloudSaveKey);
        SecurePrefs.DeleteKey(LastSyncKey);
        SecurePrefs.Save();
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
            data.unlockedSkins = string.Join("|", ProgressionManager.Instance.UnlockedSkinIds);
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

        if (GhostRunManager.Instance != null)
        {
            data.ghostData = GhostRunManager.Instance.ExportBestRunJson();
        }

        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        // Restore to PlayerPrefs so each system picks it up on next load
        WriteInt("cdr.progress.softCurrency", data.softCurrency);
        WriteInt("cdr.progress.highScore", data.highScore);
        WriteFloat("cdr.progress.bestDistance", data.bestDistance);
        WriteInt("cdr.progress.totalRuns", data.totalRuns);
        WriteFloat("cdr.progress.totalDistance", data.totalDistance);
        WriteInt("cdr.progress.totalDrones", data.totalDrones);
        WriteInt("cdr.xp.total", data.xpTotal);
        WriteInt("cdr.xp.level", data.xpLevel);

        if (!string.IsNullOrEmpty(data.selectedSkin))
        {
            WriteString("cdr.progress.selectedSkin", data.selectedSkin);
        }

        if (!string.IsNullOrEmpty(data.unlockedSkins))
        {
            WriteString("cdr.progress.unlockedSkins", data.unlockedSkins);
        }

        if (!string.IsNullOrEmpty(data.upgradeData))
        {
            string[] tokens = data.upgradeData.Split(',');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (int.TryParse(tokens[i], out int level))
                {
                    WriteInt("cdr.upgrade." + i, level);
                }
            }
        }

        if (!string.IsNullOrEmpty(data.ghostData) && GhostRunManager.Instance != null)
        {
            GhostRunManager.Instance.ImportBestRunJson(data.ghostData);
        }

        PlayerPrefs.Save();
        SecurePrefs.Save();
        Debug.Log($"[CloudSaveManager] Applied save data from {data.timestamp}");
    }

    private static void WriteInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        SecurePrefs.SetInt(key, value);
    }

    private static void WriteFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        SecurePrefs.SetFloat(key, value);
    }

    private static void WriteString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        SecurePrefs.SetString(key, value);
    }
}
