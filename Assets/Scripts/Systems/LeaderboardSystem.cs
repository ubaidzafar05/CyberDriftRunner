using System.Collections.Generic;
using UnityEngine;

public sealed class LeaderboardSystem : MonoBehaviour
{
    public static LeaderboardSystem Instance { get; private set; }

    private const string LeaderboardPrefix = "cdr.lb.";
    private const int MaxEntries = 10;

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
        return new LeaderboardSubmissionPayload
        {
            PlayerId = SystemInfo.deviceUniqueIdentifier,
            PlayerName = playerName,
            Score = summary.Score,
            Distance = Mathf.FloorToInt(summary.Distance),
            DateUtc = System.DateTime.UtcNow.ToString("o")
        };
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
        _transport.SubmitScore(payload);
    }

    private void Load()
    {
        _entries.Clear();
        int count = PlayerPrefs.GetInt(LeaderboardPrefix + "count", 0);
        for (int i = 0; i < Mathf.Min(count, MaxEntries); i++)
        {
            LeaderboardEntry entry = new LeaderboardEntry
            {
                PlayerName = PlayerPrefs.GetString(LeaderboardPrefix + "name." + i, "You"),
                Score = PlayerPrefs.GetInt(LeaderboardPrefix + "score." + i, 0),
                Distance = PlayerPrefs.GetFloat(LeaderboardPrefix + "dist." + i, 0f),
                Date = PlayerPrefs.GetString(LeaderboardPrefix + "date." + i, "")
            };

            _entries.Add(entry);
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(LeaderboardPrefix + "count", _entries.Count);
        for (int i = 0; i < _entries.Count; i++)
        {
            PlayerPrefs.SetString(LeaderboardPrefix + "name." + i, _entries[i].PlayerName);
            PlayerPrefs.SetInt(LeaderboardPrefix + "score." + i, _entries[i].Score);
            PlayerPrefs.SetFloat(LeaderboardPrefix + "dist." + i, _entries[i].Distance);
            PlayerPrefs.SetString(LeaderboardPrefix + "date." + i, _entries[i].Date);
        }

        PlayerPrefs.Save();
    }

    private static string GetPlayerName()
    {
        return string.IsNullOrWhiteSpace(SystemInfo.deviceName) ? "Runner" : SystemInfo.deviceName;
    }
}
