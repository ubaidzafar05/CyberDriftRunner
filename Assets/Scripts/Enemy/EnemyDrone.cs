using System.Collections.Generic;
using UnityEngine;

public enum DroneType
{
    Chaser,
    Shooter,
    Kamikaze
}

public sealed class EnemyDrone : MonoBehaviour, IDamageable, IHackable
{
    private static readonly List<EnemyDrone> ActiveDroneList = new List<EnemyDrone>(64);
    private static readonly HashSet<EnemyDrone> ActiveDroneSet = new HashSet<EnemyDrone>();

    public static IReadOnlyList<EnemyDrone> ActiveDrones => ActiveDroneList;

    [SerializeField] private float backwardDriftSpeed = 4f;
    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 4f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private float shooterStandOffDistance = 10f;
    [SerializeField] private float shooterLaneTolerance = 0.8f;
    [SerializeField] private float kamikazeSpeedMultiplier = 1.6f;
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
    private DroneType _droneType = DroneType.Chaser;

    public bool IsAlive => _isAlive;
    public bool IsHackable => _isAlive;
    public DroneType Type => _droneType;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        if (ActiveDroneSet.Add(this))
        {
            ActiveDroneList.Add(this);
        }

        _isAlive = true;
        _currentHealth = maxHealth;
        _hoverSeed = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnDisable()
    {
        if (ActiveDroneSet.Remove(this))
        {
            ActiveDroneList.Remove(this);
        }
    }

    private void Update()
    {
        if (!_isAlive || GameManager.Instance?.Player == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        UpdatePosition();
        UpdateAttack();
        ReturnIfBehindPlayer();
    }

    public void Initialize(float assignedLaneX, float assignedHeight, float driftSpeed, int healthValue, int reward, DroneType droneType = DroneType.Chaser)
    {
        _laneX = assignedLaneX;
        _baseHeight = assignedHeight;
        backwardDriftSpeed = driftSpeed;
        maxHealth = Mathf.Max(1, healthValue);
        _currentHealth = maxHealth;
        scoreReward = reward;
        _attackReadyAt = 0f;
        _droneType = droneType;
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

        GameManager.Instance?.RegisterHackPerformed(1);
        Disable(scoreReward + 20);
        return true;
    }

    public static void DisableAllActive(int rewardPerDrone)
    {
        EnemyDrone[] snapshot = ActiveDroneList.ToArray();
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
        EnemyDrone[] snapshot = ActiveDroneList.ToArray();
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

    private void UpdatePosition()
    {
        Vector3 playerPosition = GameManager.Instance.Player.transform.position;
        Vector3 nextPosition = transform.position;
        nextPosition.y = _baseHeight + Mathf.Sin((Time.time * hoverFrequency) + _hoverSeed) * hoverAmplitude;
        nextPosition.z -= GetForwardDrift() * Time.deltaTime;
        nextPosition.x = Mathf.Lerp(nextPosition.x, GetTargetLaneX(playerPosition.x), GetLateralTracking() * Time.deltaTime);
        transform.position = nextPosition;
    }

    private void UpdateAttack()
    {
        if (Time.time < _attackReadyAt)
        {
            return;
        }

        if (_droneType == DroneType.Shooter)
        {
            TryShooterAttack();
            return;
        }

        TryContactAttack();
    }

    private void TryShooterAttack()
    {
        Vector3 playerPosition = GameManager.Instance.Player.transform.position;
        bool withinDepth = transform.position.z - playerPosition.z <= shooterStandOffDistance;
        bool sameLane = Mathf.Abs(transform.position.x - playerPosition.x) <= shooterLaneTolerance;
        if (!withinDepth || !sameLane)
        {
            return;
        }

        _attackReadyAt = Time.time + (attackCooldown * 1.25f);
        GameManager.Instance.Player.TakeHit(contactDamage);
    }

    private void TryContactAttack()
    {
        PlayerController player = GameManager.Instance.Player;
        if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
        {
            return;
        }

        _attackReadyAt = Time.time + attackCooldown;
        player.TakeHit(contactDamage);
    }

    private float GetTargetLaneX(float playerX)
    {
        if (_droneType == DroneType.Chaser || _droneType == DroneType.Kamikaze)
        {
            return Mathf.Lerp(_laneX, playerX, _droneType == DroneType.Kamikaze ? 0.85f : 0.55f);
        }

        return _laneX;
    }

    private float GetLateralTracking()
    {
        if (_droneType == DroneType.Kamikaze)
        {
            return 10f;
        }

        return _droneType == DroneType.Chaser ? 6.5f : 3.5f;
    }

    private float GetForwardDrift()
    {
        if (_droneType == DroneType.Shooter)
        {
            Vector3 playerPosition = GameManager.Instance.Player.transform.position;
            float targetZ = playerPosition.z + shooterStandOffDistance;
            return transform.position.z > targetZ ? backwardDriftSpeed : 0f;
        }

        return _droneType == DroneType.Kamikaze ? backwardDriftSpeed * kamikazeSpeedMultiplier : backwardDriftSpeed;
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
        GameManager.Instance?.RegisterDroneDestroyed(1);
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
