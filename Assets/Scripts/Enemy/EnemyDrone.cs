using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyDrone : MonoBehaviour, IDamageable, IHackable
{
    private static readonly List<EnemyDrone> _activeDronesList = new List<EnemyDrone>(64);
    private static readonly HashSet<EnemyDrone> _activeDronesSet = new HashSet<EnemyDrone>();

    public static IReadOnlyList<EnemyDrone> ActiveDrones => _activeDronesList;

    [SerializeField] private float backwardDriftSpeed = 4f;
    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 4f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int scoreReward = 40;

    private bool _isAlive;
    private float _attackReadyAt;
    private float _hoverSeed;
    private float _laneX;
    private float _baseHeight;
    private int _currentHealth;
    private PooledObject _pooledObject;

    public bool IsAlive => _isAlive;
    public bool IsHackable => _isAlive;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        if (_activeDronesSet.Add(this))
        {
            _activeDronesList.Add(this);
        }

        _isAlive = true;
        _currentHealth = maxHealth;
        _hoverSeed = Random.Range(0f, 6.28f);
    }

    private void OnDisable()
    {
        if (_activeDronesSet.Remove(this))
        {
            _activeDronesList.Remove(this);
        }
    }

    private void Update()
    {
        if (!_isAlive || GameManager.Instance == null || GameManager.Instance.Player == null)
        {
            return;
        }

        MoveDrone();
        TryAttackPlayer();
        ReturnIfBehindPlayer();
    }

    public void Initialize(float assignedLaneX, float assignedHeight, float driftSpeed, int healthValue, int reward)
    {
        _laneX = assignedLaneX;
        _baseHeight = assignedHeight;
        backwardDriftSpeed = driftSpeed;
        maxHealth = Mathf.Max(1, healthValue);
        _currentHealth = maxHealth;
        scoreReward = reward;
        _attackReadyAt = 0f;
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!_isAlive)
        {
            return;
        }

        _currentHealth -= Mathf.Max(1, damage);
        if (_currentHealth <= 0)
        {
            Disable(scoreReward);
        }
    }

    public bool TryHack()
    {
        if (!_isAlive)
        {
            return false;
        }

        Disable(scoreReward + 20);
        return true;
    }

    public static void DisableAllActive(int rewardPerDrone)
    {
        EnemyDrone[] snapshot = _activeDronesList.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null && snapshot[i]._isAlive)
            {
                snapshot[i].Disable(rewardPerDrone);
            }
        }
    }

    public static void DisableThreatsNear(Vector3 position, float radius)
    {
        float radiusSquared = radius * radius;
        EnemyDrone[] snapshot = _activeDronesList.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] == null || !snapshot[i]._isAlive)
            {
                continue;
            }

            if ((snapshot[i].transform.position - position).sqrMagnitude <= radiusSquared)
            {
                snapshot[i].ReturnToPool();
            }
        }
    }

    private void MoveDrone()
    {
        float hoverY = _baseHeight + Mathf.Sin((Time.time * hoverFrequency) + _hoverSeed) * hoverAmplitude;
        Vector3 position = transform.position;
        position.x = Mathf.Lerp(position.x, _laneX, 6f * Time.deltaTime);
        position.y = hoverY;
        position.z -= backwardDriftSpeed * Time.deltaTime;
        transform.position = position;
    }

    private void TryAttackPlayer()
    {
        if (Time.time < _attackReadyAt)
        {
            return;
        }

        PlayerController player = GameManager.Instance.Player;
        if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
        {
            return;
        }

        _attackReadyAt = Time.time + attackCooldown;
        player.TakeHit(contactDamage);
    }

    private void ReturnIfBehindPlayer()
    {
        if (transform.position.z >= GameManager.Instance.Player.transform.position.z - 10f)
        {
            return;
        }

        ReturnToPool();
    }

    private void Disable(int reward)
    {
        _isAlive = false;
        GameManager.Instance?.AddScore(reward);
        ComboSystem.Instance?.RegisterKill();
        ProgressionManager.Instance?.AddDronesDestroyed(1);
        ReturnToPool();
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
