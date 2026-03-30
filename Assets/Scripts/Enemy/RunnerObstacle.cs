using System.Collections.Generic;
using UnityEngine;

public sealed class RunnerObstacle : MonoBehaviour, IHackable
{
    public static IReadOnlyList<RunnerObstacle> ActiveObstacles => activeObstacles;

    private static readonly List<RunnerObstacle> activeObstacles = new List<RunnerObstacle>();

    [SerializeField] private int collisionDamage = 1;
    [SerializeField] private int hackReward = 25;

    private bool allowMovement;
    private float moveAmplitude;
    private float moveFrequency;
    private float anchorX;
    private float phase;
    private PooledObject pooledObject;

    public bool IsHackable => isActiveAndEnabled;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
        if (!activeObstacles.Contains(this))
        {
            activeObstacles.Add(this);
        }

        anchorX = transform.position.x;
        phase = Random.Range(0f, 6.28f);
    }

    private void OnDisable()
    {
        activeObstacles.Remove(this);
    }

    private void Update()
    {
        if (allowMovement)
        {
            float x = anchorX + Mathf.Sin((Time.time * moveFrequency) + phase) * moveAmplitude;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        ReturnWhenBehindPlayer();
    }

    public void Initialize(bool shouldMove, float amplitude, float frequency, int reward)
    {
        allowMovement = shouldMove;
        moveAmplitude = amplitude;
        moveFrequency = frequency;
        hackReward = reward;
        anchorX = transform.position.x;
        phase = Random.Range(0f, 6.28f);
    }

    public bool TryHack()
    {
        if (!IsHackable)
        {
            return false;
        }

        GameManager.Instance.AddScore(hackReward);
        ReturnToPool();
        return true;
    }

    public static void DisableThreatsNear(Vector3 position, float radius)
    {
        float radiusSquared = radius * radius;
        RunnerObstacle[] snapshot = activeObstacles.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] == null)
            {
                continue;
            }

            if ((snapshot[i].transform.position - position).sqrMagnitude <= radiusSquared)
            {
                snapshot[i].ReturnToPool();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.TakeHit(collisionDamage);
        ReturnToPool();
    }

    private void ReturnWhenBehindPlayer()
    {
        if (GameManager.Instance?.Player == null)
        {
            return;
        }

        if (transform.position.z < GameManager.Instance.Player.transform.position.z - 10f)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
