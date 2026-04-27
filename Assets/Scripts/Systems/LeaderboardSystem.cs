using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LeaderboardSystem : MonoBehaviour
{
    public static LeaderboardSystem Instance { get; private set; }

    private const string LeaderboardSaveKey = "cdr.lb.entries";
    private const int MaxEntries = 10;

    [Serializable]
    private sealed class LeaderboardState
    {
        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>(MaxEntries);
    }

    [System.Serializable]
    public struct LeaderboardEntry
    {
        public string PlayerName;
        public int Score;
        public float Distance;
        public string Date;
    }

    [SerializeField] private MonoBehaviour transportBehaviour;

    private readonly List<LeaderboardEntry> _entries = new List<LeaderboardEntry>(MaxEntries);
    private ILeaderboardTransport _transport;

    public IReadOnlyList<LeaderboardEntry> Entries => _entries;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _transport = transportBehaviour as ILeaderboardTransport;
        Load();
    }

    public bool SubmitRun(RunSummary summary)
    {
        if (!LeaderboardValidator.TryValidateRun(summary, out string reason))
        {
            Debug.LogWarning($"[LeaderboardSystem] Rejected run: {reason}");
            return false;
        }

        LeaderboardEntry entry = new LeaderboardEntry
        {
            PlayerName = GetPlayerName(),
            Score = summary.Score,
            Distance = summary.Distance,
            Date = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        int insertIndex = InsertEntry(entry);
        Save();
        SubmitRemote(summary, entry.PlayerName);
        return insertIndex < MaxEntries;
    }

    public int GetRank(int score)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (score >= _entries[i].Score)
            {
                return i + 1;
            }
        }

        return _entries.Count + 1;
    }

    public List<LeaderboardEntry> GetTopEntries(int count)
    {
        int take = Mathf.Min(count, _entries.Count);
        return _entries.GetRange(0, take);
    }

    public LeaderboardSubmissionPayload BuildPayload(RunSummary summary, string playerName)
    {
        LeaderboardSubmissionPayload payload = new LeaderboardSubmissionPayload
        {
            PlayerId = SystemInfo.deviceUniqueIdentifier,
            PlayerName = playerName,
            Score = summary.Score,
            Distance = Mathf.FloorToInt(summary.Distance),
            SurvivalTime = summary.SurvivalTime,
            DateUtc = System.DateTime.UtcNow.ToString("o")
        };

        payload.Signature = LeaderboardValidator.BuildSignature(payload);
        return payload;
    }

    private int InsertEntry(LeaderboardEntry entry)
    {
        int insertIndex = _entries.Count;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (entry.Score > _entries[i].Score)
            {
                insertIndex = i;
                break;
            }
        }

        _entries.Insert(insertIndex, entry);
        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        return insertIndex;
    }

    private void SubmitRemote(RunSummary summary, string playerName)
    {
        if (_transport == null)
        {
            return;
        }

        LeaderboardSubmissionPayload payload = BuildPayload(summary, playerName);
        LeaderboardSubmissionReceipt receipt = _transport.SubmitScore(payload);
        if (!receipt.Accepted)
        {
            Debug.LogWarning($"[LeaderboardSystem] Transport rejected submission: {receipt.Message}");
        }
    }

    private void Load()
    {
        _entries.Clear();
        string json = SecurePrefs.GetString(LeaderboardSaveKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            LeaderboardState state = JsonUtility.FromJson<LeaderboardState>(json);
            if (state != null && state.Entries != null)
            {
                int count = Mathf.Min(state.Entries.Count, MaxEntries);
                for (int i = 0; i < count; i++)
                {
                    _entries.Add(state.Entries[i]);
                }
                return;
            }
        }

        const string legacyPrefix = "cdr.lb.";
        int legacyCount = PlayerPrefs.GetInt(legacyPrefix + "count", 0);
        for (int i = 0; i < Mathf.Min(legacyCount, MaxEntries); i++)
        {
            _entries.Add(new LeaderboardEntry
            {
                PlayerName = PlayerPrefs.GetString(legacyPrefix + "name." + i, "You"),
                Score = PlayerPrefs.GetInt(legacyPrefix + "score." + i, 0),
                Distance = PlayerPrefs.GetFloat(legacyPrefix + "dist." + i, 0f),
                Date = PlayerPrefs.GetString(legacyPrefix + "date." + i, "")
            });
        }

        Save();
    }

    private void Save()
    {
        LeaderboardState state = new LeaderboardState();
        state.Entries.AddRange(_entries);
        SecurePrefs.SetString(LeaderboardSaveKey, JsonUtility.ToJson(state));
        SecurePrefs.Save();
    }

    private static string GetPlayerName()
    {
        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn && !string.IsNullOrWhiteSpace(GooglePlayManager.Instance.PlayerName))
        {
            return GooglePlayManager.Instance.PlayerName;
        }

        return string.IsNullOrWhiteSpace(SystemInfo.deviceName) ? "Runner" : SystemInfo.deviceName;
    }
}
