using UnityEngine;

public sealed class BossController : MonoBehaviour, IDamageable, IHackable
{
    public static BossController ActiveBoss { get; private set; }

    [SerializeField] private int baseHealth = 30;
    [SerializeField] private int healthPerTier = 15;
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float hoverAmplitude = 0.45f;
    [SerializeField] private float hoverFrequency = 2.8f;
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private float hoverHeight = 3.2f;
    [SerializeField] private float followDistance = 24f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private int scoreReward = 350;
    [SerializeField] private int hackDamage = 4;

    private BossEncounterManager _owner;
    private int _tier;
    private int _currentHealth;
    private float _attackTimer;
    private float _hoverSeed;
    private bool _isAlive;

    public bool IsAlive => _isAlive;
    public bool IsHackable => _isAlive;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth { get; private set; }

    private void OnEnable()
    {
        ActiveBoss = this;
        _hoverSeed = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnDisable()
    {
        if (ActiveBoss == this)
        {
            ActiveBoss = null;
        }
    }

    private void Update()
    {
        if (!_isAlive || GameManager.Instance?.Player == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        UpdatePosition();
        UpdateAttacks();
    }

    public void Initialize(BossEncounterManager owner, int tier, float configuredLaneOffset)
    {
        _owner = owner;
        _tier = Mathf.Max(1, tier);
        laneOffset = Mathf.Max(1f, configuredLaneOffset);
        MaxHealth = baseHealth + ((_tier - 1) * healthPerTier);
        _currentHealth = MaxHealth;
        _attackTimer = attackInterval;
        _isAlive = true;
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
            DefeatBoss();
        }
    }

    public bool TryHack()
    {
        if (!_isAlive)
        {
            return false;
        }

        _currentHealth = Mathf.Max(0, _currentHealth - hackDamage);
        _attackTimer = Mathf.Max(_attackTimer, attackInterval * 0.75f);
        GameManager.Instance?.RegisterHackPerformed(1);
        if (_currentHealth == 0)
        {
            DefeatBoss();
        }

        return true;
    }

    private void UpdatePosition()
    {
        Transform playerTransform = GameManager.Instance.Player.transform;
        float hoverY = hoverHeight + Mathf.Sin((Time.time * hoverFrequency) + _hoverSeed) * hoverAmplitude;
        Vector3 targetPosition = new Vector3(0f, hoverY, playerTransform.position.z + followDistance);
        transform.position = Vector3.Lerp(transform.position, targetPosition, 3.5f * Time.deltaTime);
    }

    private void UpdateAttacks()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f)
        {
            return;
        }

        _attackTimer = Mathf.Max(0.8f, attackInterval - (_tier * 0.08f));
        int pattern = Random.Range(0, 3);
        if (pattern == 0)
        {
            FireLaserPattern();
            return;
        }

        if (pattern == 1)
        {
            SpawnMinionPattern();
            return;
        }

        SweepPattern();
    }

    private void FireLaserPattern()
    {
        int lane = Random.Range(-1, 2);
        _owner?.SpawnLaneStrike(lane * laneOffset, transform.position.z - 5f, projectileDamage, _tier);
    }

    private void SpawnMinionPattern()
    {
        _owner?.SpawnBossMinions(transform.position.z - 4f, _tier);
    }

    private void SweepPattern()
    {
        _owner?.SpawnSweep(-laneOffset, laneOffset, transform.position.z - 3f, projectileDamage, _tier);
    }

    private void DefeatBoss()
    {
        _isAlive = false;
        GameManager.Instance?.AddScore(scoreReward + ((_tier - 1) * 120));
        GameManager.Instance?.RegisterBossDefeated(1);
        _owner?.CompleteEncounter(this);
        gameObject.SetActive(false);
    }
}
