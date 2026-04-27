using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RunData
{
    public float distance;
    public int coins;
    public string deathReason;
    public float survivalTime;
    public string createdAt;
}

[Serializable]
public sealed class RunDataCollection
{
    public List<RunData> runs = new List<RunData>();
}

public sealed class RunAnalyticsStore : MonoBehaviour
{
    public static RunAnalyticsStore Instance { get; private set; }

    private const string RunAnalyticsKey = "cdr.analytics.runs";
    private const int MaxRuns = 50;

    private readonly List<RunData> _runs = new List<RunData>(MaxRuns);

    public IReadOnlyList<RunData> Runs => _runs;

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

    public void RecordRun(RunSummary summary, string deathReason)
    {
        RunData data = new RunData
        {
            distance = summary.Distance,
            coins = summary.Credits,
            deathReason = string.IsNullOrWhiteSpace(deathReason) ? "unknown" : deathReason,
            survivalTime = summary.SurvivalTime,
            createdAt = DateTime.UtcNow.ToString("o")
        };

        _runs.Add(data);
        if (_runs.Count > MaxRuns)
        {
            _runs.RemoveAt(0);
        }

        Save();
    }

    public float GetAverageDistance()
    {
        if (_runs.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 0; i < _runs.Count; i++)
        {
            total += _runs[i].distance;
        }

        return total / _runs.Count;
    }

    public string GetTopDeathReason()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < _runs.Count; i++)
        {
            string reason = string.IsNullOrWhiteSpace(_runs[i].deathReason) ? "unknown" : _runs[i].deathReason;
            counts[reason] = counts.TryGetValue(reason, out int count) ? count + 1 : 1;
        }

        string bestReason = "unknown";
        int bestCount = 0;
        foreach (KeyValuePair<string, int> pair in counts)
        {
            if (pair.Value > bestCount)
            {
                bestReason = pair.Key;
                bestCount = pair.Value;
            }
        }

        return bestReason;
    }

    private void Load()
    {
        string json = SecurePrefs.GetString(RunAnalyticsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        RunDataCollection collection = JsonUtility.FromJson<RunDataCollection>(json);
        if (collection == null || collection.runs == null)
        {
            return;
        }

        _runs.Clear();
        _runs.AddRange(collection.runs);
    }

    private void Save()
    {
        RunDataCollection collection = new RunDataCollection();
        collection.runs.AddRange(_runs);
        SecurePrefs.SetString(RunAnalyticsKey, JsonUtility.ToJson(collection));
        SecurePrefs.Save();
    }
}
