using UnityEngine;

public sealed class BossEncounterManager : MonoBehaviour
{
    public static BossEncounterManager Instance { get; private set; }

    [SerializeField] private PlayerController player;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject bossHazardPrefab;
    [SerializeField] private float encounterIntervalDistance = 1000f;
    [SerializeField] private float hazardTelegraphDuration = 0.85f;
    [SerializeField] private float hazardActiveDuration = 0.75f;
    [SerializeField] private int hazardPreload = 6;

    private ObjectPool _bossPool;
    private ObjectPool _hazardPool;
    private Transform _poolRoot;
    private int _nextBossTier = 1;

    public BossController ActiveBoss { get; private set; }
    public bool IsEncounterActive => ActiveBoss != null && ActiveBoss.IsAlive;

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

    public void Configure(PlayerController targetPlayer, ObstacleSpawner spawner, GameObject configuredBossPrefab, GameObject configuredHazardPrefab)
    {
        player = targetPlayer;
        obstacleSpawner = spawner;
        bossPrefab = configuredBossPrefab;
        bossHazardPrefab = configuredHazardPrefab;
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
        if (boss != ActiveBoss)
        {
            return;
        }

        obstacleSpawner?.SetEncounterSuspended(false);
        GameManager.Instance?.SetBossEncounterState(false, null);
        ActiveBoss = null;
        _nextBossTier++;
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
        obstacleSpawner?.SetEncounterSuspended(true);
        GameManager.Instance?.SetBossEncounterState(true, boss);
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
}
