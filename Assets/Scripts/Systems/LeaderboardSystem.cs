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

    private readonly List<LeaderboardEntry> _entries = new List<LeaderboardEntry>(MaxEntries);

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
        Load();
    }

    public bool SubmitRun(RunSummary summary)
    {
        LeaderboardEntry entry = new LeaderboardEntry
        {
            PlayerName = "You",
            Score = summary.Score,
            Distance = summary.Distance,
            Date = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
        };

        int insertIndex = _entries.Count;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (summary.Score > _entries[i].Score)
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

        Save();
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

    private void Load()
    {
        _entries.Clear();
        int count = PlayerPrefs.GetInt(LeaderboardPrefix + "count", 0);
        for (int i = 0; i < Mathf.Min(count, MaxEntries); i++)
        {
            LeaderboardEntry entry = new LeaderboardEntry
            {
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
            PlayerPrefs.SetInt(LeaderboardPrefix + "score." + i, _entries[i].Score);
            PlayerPrefs.SetFloat(LeaderboardPrefix + "dist." + i, _entries[i].Distance);
            PlayerPrefs.SetString(LeaderboardPrefix + "date." + i, _entries[i].Date);
        }

        PlayerPrefs.Save();
    }
}
