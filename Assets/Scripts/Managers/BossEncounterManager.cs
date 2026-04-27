using UnityEngine;

public sealed class BossEncounterManager : MonoBehaviour
{
    public static BossEncounterManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject bossHazardPrefab;
    [SerializeField] private GameObject bossStagePrefab;
    [SerializeField] private float encounterIntervalDistance = 1000f;
    [SerializeField] private float encounterWarningLeadDistance = 140f;
    [SerializeField] private float hazardTelegraphDuration = 0.85f;
    [SerializeField] private float hazardActiveDuration = 0.75f;
    [SerializeField] private int hazardPreload = 6;
    [SerializeField] private float defeatDebriefDuration = 0.75f;

    private ObjectPool _bossPool;
    private ObjectPool _hazardPool;
    private ObjectPool _stagePool;
    private Transform _poolRoot;
    private int _nextBossTier = 1;
    private GameObject _activeStage;
    private Coroutine _encounterResolveRoutine;

    public BossController ActiveBoss { get; private set; }
    public bool IsEncounterActive => ActiveBoss != null && ActiveBoss.IsAlive;
    public float DistanceToNextEncounter => Mathf.Max(0f, (_nextBossTier * encounterIntervalDistance) - (GameManager.Instance != null ? GameManager.Instance.Distance : 0f));
    public bool IsEncounterImminent => !IsEncounterActive && GameManager.Instance != null && DistanceToNextEncounter > 0f && DistanceToNextEncounter <= encounterWarningLeadDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _poolRoot = new GameObject("BossPools").transform;
        _poolRoot.SetParent(transform, false);
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

        if (player == null)
        {
            player = GameManager.Instance.Player;
        }

        if (player == null || IsEncounterActive)
        {
            return;
        }

        if (GameManager.Instance.Distance < _nextBossTier * encounterIntervalDistance)
        {
            return;
        }

        BeginEncounter();
    }

    public void Configure(PlayerController targetPlayer, ObstacleSpawner spawner, GameObject configuredBossPrefab, GameObject configuredHazardPrefab, GameObject configuredBossStagePrefab)
    {
        player = targetPlayer;
        obstacleSpawner = spawner;
        bossPrefab = configuredBossPrefab;
        bossHazardPrefab = configuredHazardPrefab;
        bossStagePrefab = configuredBossStagePrefab;
    }

    public void SpawnLaneStrike(float laneX, float zPosition, int damage, int tier)
    {
        BossLaneHazard hazard = GetHazardPool().Acquire(new Vector3(laneX, 1f, zPosition), Quaternion.identity).GetComponent<BossLaneHazard>();
        hazard.ConfigureLaneStrike(laneX, zPosition, hazardTelegraphDuration, hazardActiveDuration + (tier * 0.08f), damage);
    }

    public void SpawnSweep(float startX, float endX, float zPosition, int damage, int tier)
    {
        BossLaneHazard hazard = GetHazardPool().Acquire(new Vector3(startX, 1f, zPosition), Quaternion.identity).GetComponent<BossLaneHazard>();
        hazard.ConfigureSweep(startX, endX, zPosition, hazardTelegraphDuration, hazardActiveDuration + (tier * 0.15f), damage);
    }

    public void SpawnBossMinions(float zPosition, int tier)
    {
        if (obstacleSpawner == null)
        {
            return;
        }

        obstacleSpawner.SpawnBossMinions(zPosition, Mathf.Min(3, 1 + tier));
    }

    public void CompleteEncounter(BossController boss)
    {
        if (boss != ActiveBoss || _encounterResolveRoutine != null)
        {
            return;
        }

        ScreenShake.Instance?.AddTrauma(0.3f);
        HapticFeedback.Instance?.VibrateHeavy();
        AudioManager.Instance?.PlayBossDefeated();
        _encounterResolveRoutine = StartCoroutine(ResolveEncounterRoutine());
    }

    private void BeginEncounter()
    {
        if (bossPrefab == null)
        {
            return;
        }

        BossController boss = GetBossPool().Acquire(new Vector3(0f, 3f, player.transform.position.z + 24f), Quaternion.identity).GetComponent<BossController>();
        boss.Initialize(this, _nextBossTier, obstacleSpawner != null ? obstacleSpawner.LaneOffset : 2.5f);
        ActiveBoss = boss;
        AcquireBossStage(boss);
        obstacleSpawner?.SetEncounterSuspended(true);
        GameManager.Instance?.SetBossEncounterState(true, boss);
    }

    private void LateUpdate()
    {
        if (!IsEncounterActive || ActiveBoss == null || _activeStage == null || player == null)
        {
            return;
        }

        Vector3 bossPosition = ActiveBoss.transform.position;
        Vector3 stageTarget = new Vector3(0f, 0f, Mathf.Max(player.transform.position.z + 12f, bossPosition.z - 5f));
        _activeStage.transform.position = Vector3.Lerp(_activeStage.transform.position, stageTarget, 6f * Time.deltaTime);
    }

    private ObjectPool GetBossPool()
    {
        if (_bossPool == null && bossPrefab != null)
        {
            _bossPool = new ObjectPool(bossPrefab, 1, _poolRoot);
        }

        return _bossPool;
    }

    private ObjectPool GetHazardPool()
    {
        if (_hazardPool == null && bossHazardPrefab != null)
        {
            _hazardPool = new ObjectPool(bossHazardPrefab, hazardPreload, _poolRoot);
        }

        return _hazardPool;
    }

    private ObjectPool GetStagePool()
    {
        if (_stagePool == null && bossStagePrefab != null)
        {
            _stagePool = new ObjectPool(bossStagePrefab, 1, _poolRoot);
        }

        return _stagePool;
    }

    private void AcquireBossStage(BossController boss)
    {
        ReleaseActiveStage();
        ObjectPool pool = GetStagePool();
        if (pool == null || boss == null || player == null)
        {
            return;
        }

        Vector3 stagePosition = new Vector3(0f, 0f, Mathf.Max(player.transform.position.z + 12f, boss.transform.position.z - 5f));
        _activeStage = pool.Acquire(stagePosition, Quaternion.identity);
        _activeStage.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.22f, Mathf.Clamp01((boss.Tier - 1) / 3f));
    }

    private void ReleaseActiveStage()
    {
        if (_activeStage == null)
        {
            return;
        }

        PooledObject pooled = _activeStage.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
        }
        else
        {
            _activeStage.SetActive(false);
        }

        _activeStage = null;
    }

    private System.Collections.IEnumerator ResolveEncounterRoutine()
    {
        yield return new WaitForSecondsRealtime(defeatDebriefDuration);

        obstacleSpawner?.SetEncounterSuspended(false);
        GameManager.Instance?.SetBossEncounterState(false, null);
        ReleaseActiveStage();
        ActiveBoss = null;
        _nextBossTier++;
        _encounterResolveRoutine = null;
    }
}
