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
    private float _damageFlash;
    private float _hackFlash;
    private Renderer[] _cachedRenderers;
    private Material[] _cachedMaterials;
    private Renderer _eyeRenderer;
    private Renderer _leftEngineRenderer;
    private Renderer _rightEngineRenderer;
    private Vector3 _eyeBaseScale;

    public bool IsAlive => _isAlive;
    public bool IsHackable => _isAlive;
    public DroneType Type => _droneType;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        FlatActorFacade.EnsureDroneFacade(gameObject);
        CacheVisuals();
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
        _damageFlash = 0f;
        _hackFlash = 0f;
        ApplyTypePalette();
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
        UpdateVisuals();
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
        ApplyTypePalette();
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!_isAlive)
        {
            return;
        }

        _currentHealth -= Mathf.Max(1, damage);
        _damageFlash = 0.22f;
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
        _hackFlash = 0.28f;
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
        nextPosition.x = Mathf.Lerp(nextPosition.x, GetTargetLaneX(playerPosition.x) + GetFormationOffset(), GetLateralTracking() * Time.deltaTime);
        transform.position = nextPosition;
    }

    private float GetFormationOffset()
    {
        float offset = 0f;
        for (int i = 0; i < ActiveDroneList.Count; i++)
        {
            EnemyDrone other = ActiveDroneList[i];
            if (other == null || other == this || !other._isAlive)
            {
                continue;
            }

            float zDelta = Mathf.Abs(other.transform.position.z - transform.position.z);
            if (zDelta > 3f)
            {
                continue;
            }

            float xDelta = transform.position.x - other.transform.position.x;
            float distance = Mathf.Abs(xDelta);
            if (distance < 0.01f)
            {
                offset += transform.position.z <= other.transform.position.z ? -0.12f : 0.12f;
                continue;
            }

            if (distance < 1.15f)
            {
                offset += Mathf.Sign(xDelta) * (1.15f - distance) * 0.22f;
            }
        }

        return Mathf.Clamp(offset, -0.6f, 0.6f);
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
        GameManager.Instance?.RegisterDeathReason("drone_shot");
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
        GameManager.Instance?.RegisterDeathReason(_droneType == DroneType.Kamikaze ? "drone_kamikaze" : "drone_collision");
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

    private void CacheVisuals()
    {
        _cachedRenderers = GetComponentsInChildren<Renderer>(true);
        _cachedMaterials = new Material[_cachedRenderers.Length];
        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            Renderer renderer = _cachedRenderers[i];
            _cachedMaterials[i] = renderer != null ? renderer.material : null;
            if (renderer == null)
            {
                continue;
            }

            string lowerName = renderer.transform.name.ToLowerInvariant();
            if (lowerName.Contains("eye"))
            {
                _eyeRenderer = renderer;
                _eyeBaseScale = renderer.transform.localScale;
            }
            else if (lowerName.Contains("engineleft") || lowerName.Contains("engineglowleft"))
            {
                _leftEngineRenderer = renderer;
            }
            else if (lowerName.Contains("engineright") || lowerName.Contains("engineglowright"))
            {
                _rightEngineRenderer = renderer;
            }
        }
    }

    private void ApplyTypePalette()
    {
        if (_cachedMaterials == null || _cachedMaterials.Length == 0)
        {
            CacheVisuals();
        }

        Color shellColor;
        Color accentColor;
        Color engineColor;
        switch (_droneType)
        {
            case DroneType.Shooter:
                shellColor = new Color(0.12f, 0.1f, 0.18f);
                accentColor = new Color(1f, 0.32f, 0.84f);
                engineColor = new Color(0.28f, 0.92f, 1f);
                break;
            case DroneType.Kamikaze:
                shellColor = new Color(0.18f, 0.08f, 0.08f);
                accentColor = new Color(1f, 0.32f, 0.24f);
                engineColor = new Color(1f, 0.62f, 0.18f);
                break;
            default:
                shellColor = new Color(0.08f, 0.12f, 0.16f);
                accentColor = new Color(0.2f, 0.92f, 1f);
                engineColor = new Color(1f, 0.34f, 0.38f);
                break;
        }

        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            Renderer renderer = _cachedRenderers[i];
            Material material = _cachedMaterials[i];
            if (renderer == null || material == null)
            {
                continue;
            }

            string lowerName = renderer.transform.name.ToLowerInvariant();
            Color albedo = lowerName.Contains("eye") || lowerName.Contains("wingedge") || lowerName.Contains("sensor") ? accentColor
                : lowerName.Contains("engine") || lowerName.Contains("tail") ? engineColor
                : shellColor;
            float emissionBoost = lowerName.Contains("eye") ? 2.6f : lowerName.Contains("engine") ? 2.1f : 0.6f;
            ApplyMaterial(material, albedo, emissionBoost);
        }
    }

    private void UpdateVisuals()
    {
        _damageFlash = Mathf.Max(0f, _damageFlash - Time.deltaTime);
        _hackFlash = Mathf.Max(0f, _hackFlash - Time.deltaTime);

        float pulse = 0.82f + (Mathf.Sin((Time.time * 10f) + _hoverSeed) * 0.18f);
        Color threatColor = GetThreatColor();
        threatColor = Color.Lerp(threatColor, Color.white, _damageFlash * 0.85f);
        threatColor = Color.Lerp(threatColor, new Color(0.28f, 1f, 1f), _hackFlash);

        if (_eyeRenderer != null)
        {
            ApplyMaterial(_eyeRenderer.material, threatColor, 2.6f + (pulse * 0.7f));
            _eyeRenderer.transform.localScale = new Vector3(
                _eyeBaseScale.x + (pulse * 0.08f),
                _eyeBaseScale.y + (pulse * 0.02f),
                _eyeBaseScale.z);
        }

        if (_leftEngineRenderer != null)
        {
            ApplyMaterial(_leftEngineRenderer.material, Color.Lerp(threatColor, new Color(1f, 0.72f, 0.22f), 0.35f), 1.8f + (pulse * 0.4f));
        }

        if (_rightEngineRenderer != null)
        {
            ApplyMaterial(_rightEngineRenderer.material, Color.Lerp(threatColor, new Color(1f, 0.72f, 0.22f), 0.35f), 1.8f + (pulse * 0.4f));
        }
    }

    private Color GetThreatColor()
    {
        switch (_droneType)
        {
            case DroneType.Shooter:
                return new Color(1f, 0.28f, 0.78f);
            case DroneType.Kamikaze:
                return new Color(1f, 0.34f, 0.18f);
            default:
                return new Color(0.22f, 0.94f, 1f);
        }
    }

    private static void ApplyMaterial(Material material, Color color, float emissionBoost)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionBoost);
        }
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
