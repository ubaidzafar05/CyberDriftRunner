using UnityEngine;

public sealed class MilestoneSystem : MonoBehaviour
{
    public static MilestoneSystem Instance { get; private set; }

    [Header("Milestones")]
    [SerializeField] private int[] milestoneDistances = { 250, 500, 1000, 1500, 2000, 3000, 5000, 7500, 10000 };
    [SerializeField] private int[] milestoneCreditRewards = { 10, 25, 50, 75, 100, 150, 250, 400, 750 };
    [SerializeField] private int milestoneScoreBonus = 200;

    [Header("Zones")]
    [SerializeField] private float zoneLength = 500f;

    private int _nextMilestoneIndex;
    private int _currentZone;

    public int CurrentZone => _currentZone;
    public string CurrentZoneName => GetZoneName(_currentZone);

    public event System.Action<int, string> OnMilestoneReached;
    public event System.Action<int, string, Color> OnZoneChanged;

    private static readonly string[] ZoneNames =
    {
        "Neon District",
        "Data Highway",
        "Chrome Wastes",
        "Synth Corridor",
        "Void Bridge",
        "Quantum Alley",
        "Plasma Depths",
        "Infinity Grid",
        "Singularity Core",
        "Event Horizon"
    };

    private static readonly Color[] ZoneColors =
    {
        new Color(0.1f, 0.7f, 1f),
        new Color(1f, 0.15f, 0.7f),
        new Color(1f, 0.85f, 0.15f),
        new Color(0.3f, 1f, 0.5f),
        new Color(0.7f, 0.3f, 1f),
        new Color(1f, 0.5f, 0.15f),
        new Color(0.15f, 1f, 1f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.5f, 0.5f, 1f),
        new Color(1f, 1f, 1f)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResetForRun()
    {
        _nextMilestoneIndex = 0;
        _currentZone = 0;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        float distance = GameManager.Instance.Distance;
        CheckMilestones(distance);
        CheckZoneChange(distance);
    }

    private void CheckMilestones(float distance)
    {
        while (_nextMilestoneIndex < milestoneDistances.Length && distance >= milestoneDistances[_nextMilestoneIndex])
        {
            int metersMark = milestoneDistances[_nextMilestoneIndex];
            int creditReward = _nextMilestoneIndex < milestoneCreditRewards.Length
                ? milestoneCreditRewards[_nextMilestoneIndex]
                : 50;

            GameManager.Instance.AddScore(milestoneScoreBonus);
            GameManager.Instance.AddCredits(creditReward);
            AudioManager.Instance?.PlayPowerUp();
            ScreenShake.Instance?.AddTrauma(0.15f);
            HapticFeedback.Instance?.VibrateMedium();

            string label = $"{metersMark}m!";
            OnMilestoneReached?.Invoke(metersMark, label);

            _nextMilestoneIndex++;
        }
    }

    private void CheckZoneChange(float distance)
    {
        int newZone = Mathf.FloorToInt(distance / zoneLength);
        if (newZone > _currentZone)
        {
            _currentZone = newZone;
            string zoneName = GetZoneName(_currentZone);
            Color zoneColor = GetZoneColor(_currentZone);
            OnZoneChanged?.Invoke(_currentZone, zoneName, zoneColor);
        }
    }

    private static string GetZoneName(int zone)
    {
        return ZoneNames[zone % ZoneNames.Length];
    }

    private static Color GetZoneColor(int zone)
    {
        return ZoneColors[zone % ZoneColors.Length];
    }
}
