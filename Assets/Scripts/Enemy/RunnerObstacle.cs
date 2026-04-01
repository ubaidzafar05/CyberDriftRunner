using System.Collections.Generic;
using UnityEngine;

public sealed class RunnerObstacle : MonoBehaviour, IHackable
{
    private static readonly List<RunnerObstacle> ActiveObstacleList = new List<RunnerObstacle>(64);
    private static readonly HashSet<RunnerObstacle> ActiveObstacleSet = new HashSet<RunnerObstacle>();

    public static IReadOnlyList<RunnerObstacle> ActiveObstacles => ActiveObstacleList;

    [SerializeField] private int collisionDamage = 1;
    [SerializeField] private int hackReward = 25;

    private bool _allowMovement;
    private float _moveAmplitude;
    private float _moveFrequency;
    private float _anchorX;
    private float _phase;
    private PooledObject _pooledObject;

    public bool IsHackable => isActiveAndEnabled;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        if (ActiveObstacleSet.Add(this))
        {
            ActiveObstacleList.Add(this);
        }

        _anchorX = transform.position.x;
        _phase = Random.Range(0f, 6.28f);
    }

    private void OnDisable()
    {
        if (ActiveObstacleSet.Remove(this))
        {
            ActiveObstacleList.Remove(this);
        }
    }

    private void Update()
    {
        if (_allowMovement)
        {
            float x = _anchorX + Mathf.Sin((Time.time * _moveFrequency) + _phase) * _moveAmplitude;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        ReturnWhenBehindPlayer();
    }

    public void Initialize(bool shouldMove, float amplitude, float frequency, int reward)
    {
        _allowMovement = shouldMove;
        _moveAmplitude = amplitude;
        _moveFrequency = frequency;
        hackReward = reward;
        _anchorX = transform.position.x;
        _phase = Random.Range(0f, 6.28f);
    }

    public bool TryHack()
    {
        if (!IsHackable)
        {
            return false;
        }

        GameManager.Instance?.AddScore(hackReward);
        GameManager.Instance?.RegisterHackPerformed(1);
        ReturnToPool();
        return true;
    }

    public static void DisableThreatsNear(Vector3 position, float radius)
    {
        float radiusSquared = radius * radius;
        RunnerObstacle[] snapshot = ActiveObstacleList.ToArray();
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
        if (_pooledObject != null)
        {
            _pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
