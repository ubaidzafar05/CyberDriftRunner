using System;
using UnityEngine;

[Serializable]
public sealed class RemoteGameConfig
{
    public float SpawnRateMultiplier = 1f;
    public float RewardMultiplier = 1f;
    public float BossHealthMultiplier = 1f;
    public float HappyHourCreditMultiplier = 2f;
    public int StarterPackRunThreshold = 3;
    public int InterstitialMinSessions = 3;
    public bool GhostsEnabled = true;
    public bool RealtimeMultiplayerEnabled = false;
}

public sealed class LiveOpsSystem : MonoBehaviour
{
    public static LiveOpsSystem Instance { get; private set; }

    private const string ConfigKey = "cdr.liveops.config";

    public RemoteGameConfig CurrentConfig { get; private set; } = new RemoteGameConfig();

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

    public void Refresh()
    {
        Load();
    }

    public string ExportJson()
    {
        return JsonUtility.ToJson(CurrentConfig, true);
    }

    public bool ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        RemoteGameConfig config = JsonUtility.FromJson<RemoteGameConfig>(json);
        if (config == null)
        {
            return false;
        }

        CurrentConfig = config;
        Save();
        EventBus.Publish(new LiveOpsConfigChangedEvent(CurrentConfig));
        return true;
    }

    public float GetSpawnRateMultiplier()
    {
        return Mathf.Clamp(CurrentConfig.SpawnRateMultiplier, 0.75f, 1.5f);
    }

    public float GetRewardMultiplier()
    {
        return Mathf.Clamp(CurrentConfig.RewardMultiplier, 0.5f, 3f);
    }

    private void Load()
    {
        string json = SecurePrefs.GetString(ConfigKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            RemoteGameConfig config = JsonUtility.FromJson<RemoteGameConfig>(json);
            if (config != null)
            {
                CurrentConfig = config;
            }
        }

        EventBus.Publish(new LiveOpsConfigChangedEvent(CurrentConfig));
    }

    private void Save()
    {
        SecurePrefs.SetString(ConfigKey, JsonUtility.ToJson(CurrentConfig));
        SecurePrefs.Save();
    }
}
