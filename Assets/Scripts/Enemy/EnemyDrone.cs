using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyDrone : MonoBehaviour, IDamageable, IHackable
{
    public static IReadOnlyList<EnemyDrone> ActiveDrones => activeDrones;

    private static readonly List<EnemyDrone> activeDrones = new List<EnemyDrone>();

    [SerializeField] private float backwardDriftSpeed = 4f;
    [SerializeField] private float hoverAmplitude = 0.4f;
    [SerializeField] private float hoverFrequency = 4f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int scoreReward = 40;

    private bool isAlive;
    private float attackReadyAt;
    private float hoverSeed;
    private float laneX;
    private float baseHeight;
    private int currentHealth;
    private PooledObject pooledObject;

    public bool IsAlive => isAlive;
    public bool IsHackable => isAlive;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
        if (!activeDrones.Contains(this))
        {
            activeDrones.Add(this);
        }

        isAlive = true;
        currentHealth = maxHealth;
        hoverSeed = Random.Range(0f, 6.28f);
    }

    private void OnDisable()
    {
        activeDrones.Remove(this);
    }

    private void Update()
    {
        if (!isAlive || GameManager.Instance == null || GameManager.Instance.Player == null)
        {
            return;
        }

        MoveDrone();
        TryAttackPlayer();
        ReturnIfBehindPlayer();
    }

    public void Initialize(float assignedLaneX, float assignedHeight, float driftSpeed, int healthValue, int reward)
    {
        laneX = assignedLaneX;
        baseHeight = assignedHeight;
        backwardDriftSpeed = driftSpeed;
        maxHealth = Mathf.Max(1, healthValue);
        currentHealth = maxHealth;
        scoreReward = reward;
        attackReadyAt = 0f;
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        if (currentHealth <= 0)
        {
            Disable(scoreReward);
        }
    }

    public bool TryHack()
    {
        if (!isAlive)
        {
            return false;
        }

        Disable(scoreReward + 20);
        return true;
    }

    public static void DisableAllActive(int rewardPerDrone)
    {
        EnemyDrone[] snapshot = activeDrones.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null && snapshot[i].isAlive)
            {
                snapshot[i].Disable(rewardPerDrone);
            }
        }
    }

    public static void DisableThreatsNear(Vector3 position, float radius)
    {
        float radiusSquared = radius * radius;
        EnemyDrone[] snapshot = activeDrones.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] == null || !snapshot[i].isAlive)
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
        float hoverY = baseHeight + Mathf.Sin((Time.time * hoverFrequency) + hoverSeed) * hoverAmplitude;
        Vector3 position = transform.position;
        position.x = Mathf.Lerp(position.x, laneX, 6f * Time.deltaTime);
        position.y = hoverY;
        position.z -= backwardDriftSpeed * Time.deltaTime;
        transform.position = position;
    }

    private void TryAttackPlayer()
    {
        if (Time.time < attackReadyAt)
        {
            return;
        }

        PlayerController player = GameManager.Instance.Player;
        if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
        {
            return;
        }

        attackReadyAt = Time.time + attackCooldown;
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
        isAlive = false;
        GameManager.Instance.AddScore(reward);
        ReturnToPool();
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
