using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    Distance,
    DronesDestroyed,
    HacksPerformed,
    PowerUpsUsed,
    BossesDefeated
}

[Serializable]
public sealed class MissionDefinition
{
    public string Id;
    public string Description;
    public MissionType Type;
    public int Target;
    public int CreditReward;
}

[Serializable]
public sealed class MissionProgress
{
    public string Id;
    public string Description;
    public MissionType Type;
    public int Target;
    public int Progress;
    public int CreditReward;
    public bool Claimed;

    public bool IsComplete => Progress >= Target;

    public void ApplyProgress(int amount)
    {
        Progress = Mathf.Min(Target, Progress + Mathf.Max(0, amount));
    }
}

public sealed class MissionSystem : MonoBehaviour
{
    private const string MissionDateKey = "cdr.missions.date";
    private const string MissionIdsKey = "cdr.missions.ids";
    private const string MissionProgressKey = "cdr.missions.progress";
    private const string MissionClaimedKey = "cdr.missions.claimed";
    private const int ActiveMissionCount = 3;

    private static readonly MissionDefinition[] MissionPool =
    {
        new MissionDefinition { Id = "mission_distance_1500", Description = "Travel 1,500m", Type = MissionType.Distance, Target = 1500, CreditReward = 60 },
        new MissionDefinition { Id = "mission_distance_4000", Description = "Travel 4,000m", Type = MissionType.Distance, Target = 4000, CreditReward = 140 },
        new MissionDefinition { Id = "mission_drones_10", Description = "Destroy 10 drones", Type = MissionType.DronesDestroyed, Target = 10, CreditReward = 70 },
        new MissionDefinition { Id = "mission_drones_25", Description = "Destroy 25 drones", Type = MissionType.DronesDestroyed, Target = 25, CreditReward = 150 },
        new MissionDefinition { Id = "mission_hacks_8", Description = "Hack 8 threats", Type = MissionType.HacksPerformed, Target = 8, CreditReward = 70 },
        new MissionDefinition { Id = "mission_hacks_16", Description = "Hack 16 threats", Type = MissionType.HacksPerformed, Target = 16, CreditReward = 140 },
        new MissionDefinition { Id = "mission_powerups_6", Description = "Use 6 power-ups", Type = MissionType.PowerUpsUsed, Target = 6, CreditReward = 80 },
        new MissionDefinition { Id = "mission_powerups_12", Description = "Use 12 power-ups", Type = MissionType.PowerUpsUsed, Target = 12, CreditReward = 150 },
        new MissionDefinition { Id = "mission_boss_1", Description = "Defeat 1 boss", Type = MissionType.BossesDefeated, Target = 1, CreditReward = 120 },
        new MissionDefinition { Id = "mission_boss_3", Description = "Defeat 3 bosses", Type = MissionType.BossesDefeated, Target = 3, CreditReward = 260 },
    };

    public static MissionSystem Instance { get; private set; }

    private readonly List<MissionProgress> _activeMissions = new List<MissionProgress>(ActiveMissionCount);

    public IReadOnlyList<MissionProgress> ActiveMissions => _activeMissions;

    public event Action<MissionProgress> OnMissionCompleted;
    public event Action OnMissionsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadOrGenerate();
    }

    public void RecordProgress(MissionType type, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < _activeMissions.Count; i++)
        {
            MissionProgress mission = _activeMissions[i];
            if (mission.Type != type || mission.Claimed || mission.IsComplete)
            {
                continue;
            }

            mission.ApplyProgress(amount);
            changed = true;
            if (mission.IsComplete)
            {
                OnMissionCompleted?.Invoke(mission);
            }
        }

        if (changed)
        {
            Save();
            OnMissionsChanged?.Invoke();
        }
    }

    public bool TryClaimMission(string missionId)
    {
        MissionProgress mission = FindMission(missionId);
        if (mission == null || !mission.IsComplete || mission.Claimed)
        {
            return false;
        }

        mission.Claimed = true;
        ProgressionManager.Instance?.AddSoftCurrency(mission.CreditReward);
        AudioManager.Instance?.PlayPowerUp();
        Save();
        OnMissionsChanged?.Invoke();
        return true;
    }

    public string GetPrimaryMissionLabel()
    {
        for (int i = 0; i < _activeMissions.Count; i++)
        {
            MissionProgress mission = _activeMissions[i];
            if (!mission.Claimed)
            {
                return $"{mission.Description} {mission.Progress}/{mission.Target}";
            }
        }

        return "All missions complete";
    }

    private MissionProgress FindMission(string missionId)
    {
        for (int i = 0; i < _activeMissions.Count; i++)
        {
            if (_activeMissions[i].Id == missionId)
            {
                return _activeMissions[i];
            }
        }

        return null;
    }

    private void LoadOrGenerate()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString(MissionDateKey, string.Empty) == today)
        {
            LoadPersistedMissions();
            return;
        }

        GenerateDailyMissions(today);
        Save();
    }

    private void GenerateDailyMissions(string today)
    {
        _activeMissions.Clear();
        System.Random random = new System.Random(today.GetHashCode());
        List<int> usedIndexes = new List<int>(ActiveMissionCount);

        while (_activeMissions.Count < ActiveMissionCount && usedIndexes.Count < MissionPool.Length)
        {
            int index = random.Next(0, MissionPool.Length);
            if (usedIndexes.Contains(index))
            {
                continue;
            }

            usedIndexes.Add(index);
            MissionDefinition definition = MissionPool[index];
            _activeMissions.Add(new MissionProgress
            {
                Id = definition.Id,
                Description = definition.Description,
                Type = definition.Type,
                Target = definition.Target,
                Progress = 0,
                CreditReward = definition.CreditReward,
                Claimed = false
            });
        }

        PlayerPrefs.SetString(MissionDateKey, today);
    }

    private void LoadPersistedMissions()
    {
        _activeMissions.Clear();
        string[] ids = PlayerPrefs.GetString(MissionIdsKey, string.Empty).Split('|');
        string[] progressValues = PlayerPrefs.GetString(MissionProgressKey, string.Empty).Split('|');
        string[] claimedValues = PlayerPrefs.GetString(MissionClaimedKey, string.Empty).Split('|');

        for (int i = 0; i < ids.Length; i++)
        {
            MissionDefinition definition = FindDefinition(ids[i]);
            if (definition == null)
            {
                continue;
            }

            _activeMissions.Add(new MissionProgress
            {
                Id = definition.Id,
                Description = definition.Description,
                Type = definition.Type,
                Target = definition.Target,
                Progress = ParseInt(progressValues, i),
                CreditReward = definition.CreditReward,
                Claimed = ParseInt(claimedValues, i) == 1
            });
        }

        if (_activeMissions.Count == 0)
        {
            GenerateDailyMissions(DateTime.UtcNow.ToString("yyyyMMdd"));
        }
    }

    private static MissionDefinition FindDefinition(string missionId)
    {
        for (int i = 0; i < MissionPool.Length; i++)
        {
            if (MissionPool[i].Id == missionId)
            {
                return MissionPool[i];
            }
        }

        return null;
    }

    private void Save()
    {
        string[] ids = new string[_activeMissions.Count];
        string[] progress = new string[_activeMissions.Count];
        string[] claimed = new string[_activeMissions.Count];

        for (int i = 0; i < _activeMissions.Count; i++)
        {
            ids[i] = _activeMissions[i].Id;
            progress[i] = _activeMissions[i].Progress.ToString();
            claimed[i] = _activeMissions[i].Claimed ? "1" : "0";
        }

        PlayerPrefs.SetString(MissionIdsKey, string.Join("|", ids));
        PlayerPrefs.SetString(MissionProgressKey, string.Join("|", progress));
        PlayerPrefs.SetString(MissionClaimedKey, string.Join("|", claimed));
        PlayerPrefs.Save();
    }

    private static int ParseInt(string[] values, int index)
    {
        if (index >= values.Length)
        {
            return 0;
        }

        return int.TryParse(values[index], out int parsed) ? parsed : 0;
    }
}
