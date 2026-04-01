using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class LevelChunkSet
{
    public string Name = "District";
    public float StartDistance;
    public GameObject[] ChunkPrefabs;
}

public sealed class LevelChunkGenerator : MonoBehaviour
{
    private struct ActiveChunk
    {
        public GameObject Instance;
        public float EndZ;
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private LevelChunkSet[] chunkSets;
    [SerializeField] private float chunkLength = 30f;
    [SerializeField] private float spawnDistanceAhead = 90f;
    [SerializeField] private float recycleDistanceBehind = 45f;
    [SerializeField] private int preloadPerPrefab = 2;
    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<GameObject, ObjectPool> _pools = new Dictionary<GameObject, ObjectPool>();
    private readonly Queue<ActiveChunk> _activeChunks = new Queue<ActiveChunk>();
    private float _nextSpawnZ;

    public void Configure(PlayerController targetPlayer, LevelChunkSet[] sets, Transform root)
    {
        player = targetPlayer;
        chunkSets = sets;
        poolRoot = root;
    }

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("ChunkPools").transform;
            poolRoot.SetParent(transform, false);
        }
    }

    private void Start()
    {
        WarmPools();
        _nextSpawnZ = 0f;
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

        if (player == null || chunkSets == null || chunkSets.Length == 0)
        {
            return;
        }

        while (_nextSpawnZ < player.transform.position.z + spawnDistanceAhead)
        {
            SpawnChunk(_nextSpawnZ);
            _nextSpawnZ += chunkLength;
        }

        RecycleChunks(player.transform.position.z);
    }

    private void SpawnChunk(float zPosition)
    {
        GameObject prefab = PickChunkPrefab();
        if (prefab == null)
        {
            return;
        }

        GameObject chunkObject = GetPool(prefab).Acquire(new Vector3(0f, 0f, zPosition), Quaternion.identity);
        _activeChunks.Enqueue(new ActiveChunk { Instance = chunkObject, EndZ = zPosition + chunkLength });
    }

    private void RecycleChunks(float playerZ)
    {
        while (_activeChunks.Count > 0 && _activeChunks.Peek().EndZ < playerZ - recycleDistanceBehind)
        {
            ActiveChunk chunk = _activeChunks.Dequeue();
            PooledObject pooled = chunk.Instance != null ? chunk.Instance.GetComponent<PooledObject>() : null;
            if (pooled != null)
            {
                pooled.ReturnToPool();
            }
        }
    }

    private GameObject PickChunkPrefab()
    {
        LevelChunkSet set = chunkSets[0];
        float distance = GameManager.Instance != null ? GameManager.Instance.Distance : 0f;
        for (int i = 1; i < chunkSets.Length; i++)
        {
            if (distance < chunkSets[i].StartDistance)
            {
                break;
            }

            set = chunkSets[i];
        }

        if (set.ChunkPrefabs == null || set.ChunkPrefabs.Length == 0)
        {
            return null;
        }

        return set.ChunkPrefabs[Random.Range(0, set.ChunkPrefabs.Length)];
    }

    private void WarmPools()
    {
        if (chunkSets == null)
        {
            return;
        }

        for (int i = 0; i < chunkSets.Length; i++)
        {
            GameObject[] prefabs = chunkSets[i].ChunkPrefabs;
            if (prefabs == null)
            {
                continue;
            }

            for (int j = 0; j < prefabs.Length; j++)
            {
                if (prefabs[j] != null && !_pools.ContainsKey(prefabs[j]))
                {
                    _pools[prefabs[j]] = new ObjectPool(prefabs[j], preloadPerPrefab, poolRoot);
                }
            }
        }
    }

    private ObjectPool GetPool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out ObjectPool pool))
        {
            pool = new ObjectPool(prefab, preloadPerPrefab, poolRoot);
            _pools[prefab] = pool;
        }

        return pool;
    }
}
