using UnityEngine;

public sealed class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 2.5f;
    [SerializeField] private int maxMultiplier = 8;
    [SerializeField] private int hitsPerTier = 3;

    [Header("Near-Miss")]
    [SerializeField] private float nearMissDistance = 1.8f;
    [SerializeField] private int nearMissBonus = 50;
    [SerializeField] private float nearMissCooldown = 0.5f;

    [Header("Kill Streak")]
    [SerializeField] private int killStreakBonusPerKill = 15;

    private int _comboCount;
    private int _currentMultiplier = 1;
    private float _comboTimer;
    private int _killStreak;
    private float _lastNearMissTime;
    private int _nearMissCount;
    private int _maxCombo;

    public int ComboCount => _comboCount;
    public int CurrentMultiplier => _currentMultiplier;
    public int KillStreak => _killStreak;
    public int NearMissCount => _nearMissCount;
    public int MaxCombo => _maxCombo;

    public event System.Action<int> OnComboChanged;
    public event System.Action<int> OnNearMiss;
    public event System.Action<int> OnKillStreakChanged;

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

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (_comboCount > 0)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        CheckNearMisses();
    }

    public void RegisterPickup()
    {
        _comboCount++;
        _comboTimer = comboResetTime;
        _currentMultiplier = Mathf.Min(1 + (_comboCount / hitsPerTier), maxMultiplier);
        if (_currentMultiplier > _maxCombo)
        {
            _maxCombo = _currentMultiplier;
        }

        OnComboChanged?.Invoke(_currentMultiplier);

        HapticFeedback.Instance?.VibrateOnCollect();
    }

    public void RegisterKill()
    {
        _killStreak++;
        int streakBonus = _killStreak * killStreakBonusPerKill;
        GameManager.Instance?.AddScore(streakBonus);
        OnKillStreakChanged?.Invoke(_killStreak);
    }

    public void BreakStreak()
    {
        _killStreak = 0;
    }

    public void ResetCombo()
    {
        _comboCount = 0;
        _currentMultiplier = 1;
        _comboTimer = 0f;
        OnComboChanged?.Invoke(1);
    }

    public void ResetAll()
    {
        ResetCombo();
        _killStreak = 0;
        _nearMissCount = 0;
        _maxCombo = 0;
    }

    private void CheckNearMisses()
    {
        if (GameManager.Instance.Player == null)
        {
            return;
        }

        if (Time.time - _lastNearMissTime < nearMissCooldown)
        {
            return;
        }

        Vector3 playerPos = GameManager.Instance.Player.transform.position;
        float nearMissSqr = nearMissDistance * nearMissDistance;

        foreach (RunnerObstacle obstacle in RunnerObstacle.ActiveObstacles)
        {
            if (obstacle == null)
            {
                continue;
            }

            Vector3 obstaclePos = obstacle.transform.position;
            float zDiff = obstaclePos.z - playerPos.z;

            // Only check obstacles that just passed us
            if (zDiff > 0f || zDiff < -2f)
            {
                continue;
            }

            float lateralDist = Mathf.Abs(obstaclePos.x - playerPos.x);
            if (lateralDist < nearMissDistance && lateralDist > 0.6f)
            {
                _nearMissCount++;
                _lastNearMissTime = Time.time;
                GameManager.Instance.AddScore(nearMissBonus);
                OnNearMiss?.Invoke(nearMissBonus);
                ScreenShake.Instance?.AddTrauma(0.08f);
                HapticFeedback.Instance?.VibrateLight();
                break;
            }
        }
    }
}
