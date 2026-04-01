using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleSpawner : MonoBehaviour
{
    private enum EncounterPattern
    {
        SingleBlocker,
        DoubleBlocker,
        Sweeper,
        DroneEscort,
        CreditTunnel,
        Chicane
    }

    private struct DistrictProfile
    {
        public string Name;
        public float DifficultyBias;
        public float DroneChance;
        public float PowerUpChance;
        public float SpacingScale;
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private EncounterTuningConfig encounterConfig;
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject enemyDronePrefab;
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private GameObject creditPrefab;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private float spawnDistanceAhead = 38f;
    [SerializeField] private float minRowSpacing = 8f;
    [SerializeField] private float maxRowSpacing = 13f;
    [SerializeField] private int obstaclePreload = 16;
    [SerializeField] private int dronePreload = 8;
    [SerializeField] private int collectiblePreload = 10;

    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();
    private readonly int[] laneIndexes = { -1, 0, 1 };
    private float nextSpawnZ;
    private bool encounterSuspended;

    public float LaneOffset => GetLaneOffset();

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("SpawnPools").transform;
            poolRoot.SetParent(transform, false);
        }

        nextSpawnZ = GetSpawnDistanceAhead();
    }

    private void Start()
    {
        WarmPoolSet(obstaclePrefabs, GetObstaclePreload());
        WarmPoolSet(powerUpPrefabs, GetCollectiblePreload());
        WarmPool(creditPrefab, GetCollectiblePreload());
        WarmPool(enemyDronePrefab, GetDronePreload());
    }

    public void Configure(PlayerController targetPlayer, GameObject[] obstacles, GameObject dronePrefab, GameObject[] pickups, GameObject credits, Transform root)
    {
        player = targetPlayer;
        obstaclePrefabs = obstacles;
        enemyDronePrefab = dronePrefab;
        powerUpPrefabs = pickups;
        creditPrefab = credits;
        poolRoot = root;
    }

    public void SetEncounterSuspended(bool suspended)
    {
        encounterSuspended = suspended;
    }

    public void SpawnBossMinions(float rowZ, int count)
    {
        if (enemyDronePrefab == null)
        {
            return;
        }

        int spawnCount = Mathf.Clamp(count, 1, laneIndexes.Length);
        for (int i = 0; i < spawnCount; i++)
        {
            DistrictProfile district = GetDistrictProfile(GameManager.Instance != null ? GameManager.Instance.Distance : 0f);
            int lane = laneIndexes[i];
            DroneType droneType = i == spawnCount - 1 ? DroneType.Kamikaze : DroneType.Chaser;
            SpawnDrone(rowZ + (i * 1.5f), lane, district, 0.85f + (0.1f * i), droneType);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || encounterSuspended)
        {
            return;
        }

        if (player == null)
        {
            player = GameManager.Instance.Player;
        }

        if (player == null)
        {
            return;
        }

        while (nextSpawnZ < player.transform.position.z + GetSpawnDistanceAhead())
        {
            SpawnEncounter(nextSpawnZ);
            nextSpawnZ += GetRowSpacing();
        }
    }

    private void SpawnEncounter(float rowZ)
    {
        float progress = Mathf.Clamp01(GameManager.Instance.Distance / 2200f);
        DistrictProfile district = GetDistrictProfile(GameManager.Instance.Distance);
        int safeLaneIndex = Random.Range(0, laneIndexes.Length);
        int safeLane = laneIndexes[safeLaneIndex];
        EncounterPattern pattern = SelectPattern(progress);

        switch (pattern)
        {
            case EncounterPattern.SingleBlocker:
                SpawnSingleBlocker(rowZ, safeLane, district, progress);
                break;
            case EncounterPattern.DoubleBlocker:
                SpawnDoubleBlocker(rowZ, safeLane, district, progress);
                break;
            case EncounterPattern.Sweeper:
                SpawnSweeper(rowZ, safeLane, district, progress);
                break;
            case EncounterPattern.DroneEscort:
                SpawnDroneEscort(rowZ, safeLane, district, progress);
                break;
            case EncounterPattern.CreditTunnel:
                SpawnCreditTunnel(rowZ, safeLane, district, progress);
                break;
            case EncounterPattern.Chicane:
                SpawnChicane(rowZ, safeLane, district, progress);
                break;
        }
    }

    private EncounterPattern SelectPattern(float progress)
    {
        float roll = Random.value;
        if (progress < 0.2f)
        {
            return roll < 0.65f ? EncounterPattern.SingleBlocker : EncounterPattern.CreditTunnel;
        }

        if (progress < 0.55f)
        {
            if (roll < 0.2f) return EncounterPattern.CreditTunnel;
            if (roll < 0.45f) return EncounterPattern.DoubleBlocker;
            if (roll < 0.7f) return EncounterPattern.Sweeper;
            return EncounterPattern.DroneEscort;
        }

        if (roll < 0.18f) return EncounterPattern.CreditTunnel;
        if (roll < 0.38f) return EncounterPattern.DoubleBlocker;
        if (roll < 0.58f) return EncounterPattern.Sweeper;
        if (roll < 0.78f) return EncounterPattern.DroneEscort;
        return EncounterPattern.Chicane;
    }

    private DistrictProfile GetDistrictProfile(float distance)
    {
        EncounterTuningConfig.DistrictDefinition[] districts = encounterConfig != null ? encounterConfig.Districts : null;
        if (districts == null || districts.Length == 0)
        {
            if (distance < 500f)
            {
                return new DistrictProfile { Name = "Gateway", DifficultyBias = 0.15f, DroneChance = 0.12f, PowerUpChance = 0.2f, SpacingScale = 1f };
            }

            if (distance < 1200f)
            {
                return new DistrictProfile { Name = "Commerce Strip", DifficultyBias = 0.35f, DroneChance = 0.2f, PowerUpChance = 0.18f, SpacingScale = 0.95f };
            }

            if (distance < 2000f)
            {
                return new DistrictProfile { Name = "Transit Spine", DifficultyBias = 0.55f, DroneChance = 0.28f, PowerUpChance = 0.16f, SpacingScale = 0.88f };
            }

            return new DistrictProfile { Name = "Security Zone", DifficultyBias = 0.78f, DroneChance = 0.34f, PowerUpChance = 0.14f, SpacingScale = 0.82f };
        }

        EncounterTuningConfig.DistrictDefinition current = districts[0];
        for (int i = 1; i < districts.Length; i++)
        {
            if (distance < districts[i].startDistance)
            {
                break;
            }

            current = districts[i];
        }

        return new DistrictProfile
        {
            Name = string.IsNullOrWhiteSpace(current.name) ? "District" : current.name,
            DifficultyBias = current.difficultyBias,
            DroneChance = current.droneChance,
            PowerUpChance = current.powerUpChance,
            SpacingScale = current.spacingScale <= 0f ? 1f : current.spacingScale
        };
    }

    private void SpawnSingleBlocker(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        int blockedLane = PickBlockedLane(safeLane);
        SpawnObstacle(rowZ, blockedLane, district, progress, false);
        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 3);
    }

    private void SpawnDoubleBlocker(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        foreach (int lane in laneIndexes)
        {
            if (lane == safeLane)
            {
                continue;
            }

            SpawnObstacle(rowZ, lane, district, progress, false);
        }

        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 4);
    }

    private void SpawnSweeper(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        int movingLane = PickBlockedLane(safeLane);
        SpawnObstacle(rowZ, movingLane, district, progress, true);

        int supportLane = movingLane == -safeLane ? 0 : -movingLane;
        if (supportLane != safeLane)
        {
            SpawnObstacle(rowZ + 1.2f, supportLane, district, progress, false);
        }

        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 2);
    }

    private void SpawnDroneEscort(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        int blockedLane = PickBlockedLane(safeLane);
        SpawnObstacle(rowZ, blockedLane, district, progress, false);
        if (enemyDronePrefab != null && Random.value < district.DroneChance + progress * 0.1f)
        {
            SpawnDrone(rowZ + 2f, blockedLane, district, progress, SelectDroneType(progress));
        }

        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 3);
    }

    private void SpawnCreditTunnel(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        foreach (int lane in laneIndexes)
        {
            if (lane == safeLane)
            {
                continue;
            }

            SpawnObstacle(rowZ, lane, district, progress, false);
        }

        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 5);
        if (enemyDronePrefab != null && Random.value < district.DroneChance * 0.5f)
        {
            SpawnDrone(rowZ + 5.5f, safeLane, district, progress * 0.6f, SelectDroneType(progress * 0.8f));
        }
    }

    private void SpawnChicane(float rowZ, int safeLane, DistrictProfile district, float progress)
    {
        int firstBlockedLane = PickBlockedLane(safeLane);
        SpawnObstacle(rowZ, firstBlockedLane, district, progress, false);

        int secondSafeLane = firstBlockedLane;
        int secondBlockedLane = safeLane;
        SpawnObstacle(rowZ + 4f, secondBlockedLane, district, progress, true);
        SpawnCollectibleCorridor(rowZ, safeLane, district, progress, 2);
        SpawnCollectibleCorridor(rowZ + 4f, secondSafeLane, district, progress, 2);
    }

    private int PickBlockedLane(int safeLane)
    {
        List<int> candidates = new List<int>(2);
        for (int i = 0; i < laneIndexes.Length; i++)
        {
            if (laneIndexes[i] != safeLane)
            {
                candidates.Add(laneIndexes[i]);
            }
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SpawnObstacle(float rowZ, int lane, DistrictProfile district, float progress, bool forceMovement)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacleObject = GetPool(prefab, GetObstaclePreload()).Acquire(GetLanePosition(lane, rowZ), Quaternion.identity);
        RunnerObstacle obstacle = obstacleObject.GetComponent<RunnerObstacle>();
        bool shouldMove = forceMovement || prefab.name.Contains("Car") || Random.value < district.DifficultyBias * 0.45f;
        float amplitude = shouldMove ? Mathf.Lerp(0.45f, 1.15f, progress) : 0f;
        float frequency = Mathf.Lerp(1.6f, 3.8f, district.DifficultyBias + progress * 0.3f);
        int reward = 20 + Mathf.RoundToInt(progress * 35f);
        obstacle.Initialize(shouldMove, amplitude, frequency, reward);
    }

    private void SpawnDrone(float rowZ, int lane, DistrictProfile district, float progress, DroneType droneType)
    {
        GameObject droneObject = GetPool(enemyDronePrefab, GetDronePreload()).Acquire(GetLanePosition(lane, rowZ) + Vector3.up * 2.2f, Quaternion.identity);
        EnemyDrone drone = droneObject.GetComponent<EnemyDrone>();
        drone.Initialize(lane * GetLaneOffset(), 2.2f, 5f + (progress * 4f), 1 + Mathf.RoundToInt(progress * 2f), 40 + Mathf.RoundToInt(district.DifficultyBias * 45f), droneType);
    }

    private DroneType SelectDroneType(float progress)
    {
        if (progress < 0.28f)
        {
            return DroneType.Chaser;
        }

        if (progress < 0.62f)
        {
            return Random.value < 0.65f ? DroneType.Chaser : DroneType.Shooter;
        }

        float roll = Random.value;
        if (roll < 0.45f) return DroneType.Chaser;
        if (roll < 0.78f) return DroneType.Shooter;
        return DroneType.Kamikaze;
    }

    private void SpawnCollectibleCorridor(float rowZ, int lane, DistrictProfile district, float progress, int creditCount)
    {
        bool spawnPowerUp = powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value < district.PowerUpChance;
        if (spawnPowerUp)
        {
            GameObject powerUpPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            GetPool(powerUpPrefab, GetCollectiblePreload()).Acquire(GetLanePosition(lane, rowZ + 0.8f) + Vector3.up * 1.1f, Quaternion.identity);
            return;
        }

        if (creditPrefab == null)
        {
            return;
        }

        for (int i = 0; i < creditCount; i++)
        {
            float arcHeight = 1.1f + Mathf.Sin((i / Mathf.Max(1f, creditCount - 1f)) * Mathf.PI) * Mathf.Lerp(0.15f, 0.45f, progress);
            Vector3 position = GetLanePosition(lane, rowZ + (i * 1.25f)) + Vector3.up * arcHeight;
            GetPool(creditPrefab, GetCollectiblePreload()).Acquire(position, Quaternion.identity);
        }
    }

    private float GetRowSpacing()
    {
        DistrictProfile district = GetDistrictProfile(GameManager.Instance.Distance);
        float difficulty = Mathf.Clamp01(GameManager.Instance.Distance / 2000f);
        return Mathf.Lerp(GetMaxRowSpacing(), GetMinRowSpacing(), difficulty) * district.SpacingScale;
    }

    private Vector3 GetLanePosition(int lane, float zPosition)
    {
        return new Vector3(lane * GetLaneOffset(), 0.9f, zPosition);
    }

    private void WarmPoolSet(GameObject[] prefabs, int preload)
    {
        if (prefabs == null)
        {
            return;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            WarmPool(prefabs[i], preload);
        }
    }

    private void WarmPool(GameObject prefab, int preload)
    {
        if (prefab == null || pools.ContainsKey(prefab))
        {
            return;
        }

        pools[prefab] = new ObjectPool(prefab, preload, poolRoot);
    }

    private ObjectPool GetPool(GameObject prefab, int preload)
    {
        WarmPool(prefab, preload);
        return pools[prefab];
    }

    private float GetLaneOffset()
    {
        return encounterConfig != null ? encounterConfig.LaneOffset : laneOffset;
    }

    private float GetSpawnDistanceAhead()
    {
        return encounterConfig != null ? encounterConfig.SpawnDistanceAhead : spawnDistanceAhead;
    }

    private float GetMinRowSpacing()
    {
        return encounterConfig != null ? encounterConfig.MinRowSpacing : minRowSpacing;
    }

    private float GetMaxRowSpacing()
    {
        return encounterConfig != null ? encounterConfig.MaxRowSpacing : maxRowSpacing;
    }

    private int GetObstaclePreload()
    {
        return encounterConfig != null ? encounterConfig.ObstaclePreload : obstaclePreload;
    }

    private int GetDronePreload()
    {
        return encounterConfig != null ? encounterConfig.DronePreload : dronePreload;
    }

    private int GetCollectiblePreload()
    {
        return encounterConfig != null ? encounterConfig.CollectiblePreload : collectiblePreload;
    }
}
