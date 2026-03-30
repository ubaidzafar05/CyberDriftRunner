using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject enemyDronePrefab;
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private GameObject creditPrefab;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private float laneOffset = 3f;
    [SerializeField] private float spawnDistanceAhead = 38f;
    [SerializeField] private float minRowSpacing = 9f;
    [SerializeField] private float maxRowSpacing = 14f;
    [SerializeField] private int obstaclePreload = 12;
    [SerializeField] private int dronePreload = 6;
    [SerializeField] private int collectiblePreload = 6;

    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();
    private float nextSpawnZ;
    private readonly int[] laneIndexes = { -1, 0, 1 };

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("SpawnPools").transform;
            poolRoot.SetParent(transform, false);
        }

        nextSpawnZ = spawnDistanceAhead;
    }

    private void Start()
    {
        WarmPoolSet(obstaclePrefabs, obstaclePreload);
        WarmPoolSet(powerUpPrefabs, collectiblePreload);
        WarmPool(creditPrefab, collectiblePreload);
        WarmPool(enemyDronePrefab, dronePreload);
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

        if (player == null)
        {
            return;
        }

        while (nextSpawnZ < player.transform.position.z + spawnDistanceAhead)
        {
            SpawnRow(nextSpawnZ);
            nextSpawnZ += GetRowSpacing();
        }
    }

    private void SpawnRow(float rowZ)
    {
        int safeLane = Random.Range(0, laneIndexes.Length);
        bool spawnedHazard = false;
        float difficulty = Mathf.Clamp01(GameManager.Instance.Distance / 1500f);

        for (int i = 0; i < laneIndexes.Length; i++)
        {
            if (i == safeLane || Random.value > 0.55f + (difficulty * 0.25f))
            {
                continue;
            }

            SpawnObstacle(rowZ, laneIndexes[i], difficulty);
            spawnedHazard = true;
        }

        if (!spawnedHazard)
        {
            int forcedLane = safeLane == 0 ? 1 : 0;
            SpawnObstacle(rowZ, laneIndexes[forcedLane], difficulty);
        }

        if (enemyDronePrefab != null && Random.value < 0.25f + (difficulty * 0.25f))
        {
            int droneLane = laneIndexes[(safeLane + Random.Range(1, laneIndexes.Length)) % laneIndexes.Length];
            SpawnDrone(rowZ + Random.Range(1.5f, 3.5f), droneLane, difficulty);
        }

        SpawnCollectible(rowZ, laneIndexes[safeLane], difficulty);
    }

    private void SpawnObstacle(float rowZ, int lane, float difficulty)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        GameObject obstacleObject = GetPool(prefab, obstaclePreload).Acquire(GetLanePosition(lane, rowZ), Quaternion.identity);
        RunnerObstacle obstacle = obstacleObject.GetComponent<RunnerObstacle>();
        bool shouldMove = prefab.name.Contains("Car") || Random.value < difficulty * 0.45f;
        obstacle.Initialize(shouldMove, 0.75f, 2.5f + difficulty, 20 + Mathf.RoundToInt(difficulty * 20f));
    }

    private void SpawnDrone(float rowZ, int lane, float difficulty)
    {
        GameObject droneObject = GetPool(enemyDronePrefab, dronePreload).Acquire(GetLanePosition(lane, rowZ) + Vector3.up * 2.2f, Quaternion.identity);
        EnemyDrone drone = droneObject.GetComponent<EnemyDrone>();
        drone.Initialize(lane * laneOffset, 2.2f, 5f + (difficulty * 3f), 1 + Mathf.RoundToInt(difficulty), 40 + Mathf.RoundToInt(difficulty * 30f));
    }

    private void SpawnCollectible(float rowZ, int lane, float difficulty)
    {
        bool spawnPowerUp = powerUpPrefabs != null && powerUpPrefabs.Length > 0 && Random.value < 0.18f + (difficulty * 0.08f);
        if (spawnPowerUp)
        {
            GameObject powerUpPrefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
            GetPool(powerUpPrefab, collectiblePreload).Acquire(GetLanePosition(lane, rowZ + 0.5f) + Vector3.up * 1.1f, Quaternion.identity);
            return;
        }

        if (creditPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 position = GetLanePosition(lane, rowZ + (i * 1.2f)) + Vector3.up * 1.2f;
                GetPool(creditPrefab, collectiblePreload).Acquire(position, Quaternion.identity);
            }
        }
    }

    private float GetRowSpacing()
    {
        float difficulty = Mathf.Clamp01(GameManager.Instance.Distance / 1800f);
        return Mathf.Lerp(maxRowSpacing, minRowSpacing, difficulty);
    }

    private Vector3 GetLanePosition(int lane, float zPosition)
    {
        return new Vector3(lane * laneOffset, 0.9f, zPosition);
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
}
