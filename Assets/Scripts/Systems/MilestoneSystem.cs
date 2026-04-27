using UnityEngine;

public sealed class MilestoneSystem : MonoBehaviour
{
    public static MilestoneSystem Instance { get; private set; }

    [Header("Milestones")]
    [SerializeField] private int[] milestoneDistances = { 250, 500, 1000, 1500, 2000, 3000, 5000, 7500, 10000 };
    [SerializeField] private int[] milestoneCreditRewards = { 10, 25, 50, 75, 100, 150, 250, 400, 750 };
    [SerializeField] private int milestoneScoreBonus = 200;

    private int _nextMilestoneIndex;
    private int _currentZone;

    public int CurrentZone => _currentZone;
    public string CurrentZoneName => RunDistrictCatalog.GetByIndex(_currentZone).Name;

    public event System.Action<int, string> OnMilestoneReached;
    public event System.Action<int, string, Color> OnZoneChanged;

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
        _currentZone = RunDistrictCatalog.Resolve(0f).Index;
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
        RunDistrictCatalog.DistrictInfo district = RunDistrictCatalog.Resolve(distance);
        if (district.Index > _currentZone)
        {
            _currentZone = district.Index;
            OnZoneChanged?.Invoke(_currentZone, district.Name, district.AccentColor);
        }
    }
}
