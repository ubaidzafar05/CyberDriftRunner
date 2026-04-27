using UnityEngine;

public sealed class BossController : MonoBehaviour, IDamageable, IHackable
{
    private struct BossProfile
    {
        public string Name;
        public string RewardTitle;
        public Color EyeColor;
        public Color CoreColor;
        public Color ShellColor;
        public Vector3 BossScale;
        public float FollowDistance;
        public float HoverHeight;
        public float HoverAmplitude;
        public float HoverFrequency;
        public float AttackInterval;
        public int LaserWeight;
        public int MinionWeight;
        public int SweepWeight;
        public int BonusScore;
        public int BossCredits;
    }

    public static BossController ActiveBoss { get; private set; }

    [SerializeField] private int baseHealth = 30;
    [SerializeField] private int healthPerTier = 15;
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float hoverAmplitude = 0.45f;
    [SerializeField] private float hoverFrequency = 2.8f;
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private float hoverHeight = 3.2f;
    [SerializeField] private float followDistance = 24f;
    [SerializeField] private float introDuration = 1.35f;
    [SerializeField] private float introHeightOffset = 3.8f;
    [SerializeField] private float introDistanceOffset = 8f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private int scoreReward = 350;
    [SerializeField] private int hackDamage = 4;

    private BossEncounterManager _owner;
    private int _tier;
    private int _currentHealth;
    private float _attackTimer;
    private float _attackCycleDuration;
    private float _hoverSeed;
    private float _damageFlash;
    private float _hackFlash;
    private float _introTimer;
    private bool _isAlive;
    private Renderer _eyeRenderer;
    private Renderer _coreRenderer;
    private Renderer[] _shellRenderers;
    private Material _eyeMaterial;
    private Material _coreMaterial;
    private Material[] _shellMaterials;
    private BossProfile _profile;

    private void Awake()
    {
        FlatActorFacade.EnsureBossFacade(gameObject);
        CacheVisuals();
    }

    public bool IsAlive => _isAlive;
    public bool IsHackable => _isAlive;
    public int CurrentHealth => _currentHealth;
    public int MaxHealth { get; private set; }
    public string DisplayName => string.IsNullOrWhiteSpace(_profile.Name) ? "Gatekeeper Drone" : _profile.Name;
    public string RewardTitle => string.IsNullOrWhiteSpace(_profile.RewardTitle) ? "Boss Cache" : _profile.RewardTitle;
    public int Tier => _tier;

    private void OnEnable()
    {
        ActiveBoss = this;
        _hoverSeed = Random.Range(0f, Mathf.PI * 2f);
        _damageFlash = 0f;
        _hackFlash = 0f;
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
        UpdateVisuals();
    }

    public void Initialize(BossEncounterManager owner, int tier, float configuredLaneOffset)
    {
        _owner = owner;
        _tier = Mathf.Max(1, tier);
        _profile = ResolveProfile(_tier);
        ApplyProfileVisuals();
        laneOffset = Mathf.Max(1f, configuredLaneOffset);
        MaxHealth = baseHealth + ((_tier - 1) * healthPerTier);
        _currentHealth = MaxHealth;
        attackInterval = _profile.AttackInterval;
        followDistance = _profile.FollowDistance;
        hoverHeight = _profile.HoverHeight;
        hoverAmplitude = _profile.HoverAmplitude;
        hoverFrequency = _profile.HoverFrequency;
        _attackTimer = attackInterval;
        _attackCycleDuration = attackInterval;
        _introTimer = introDuration;
        _isAlive = true;
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
        _hackFlash = 0.32f;
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
        float introProgress = introDuration > 0f ? 1f - Mathf.Clamp01(_introTimer / introDuration) : 1f;
        float heightOffset = Mathf.Lerp(introHeightOffset, 0f, introProgress);
        float distanceOffset = Mathf.Lerp(introDistanceOffset, 0f, introProgress);
        float hoverY = hoverHeight + heightOffset + Mathf.Sin((Time.time * hoverFrequency) + _hoverSeed) * hoverAmplitude;
        Vector3 targetPosition = new Vector3(0f, hoverY, playerTransform.position.z + followDistance + distanceOffset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, 3.5f * Time.deltaTime);
    }

    private void UpdateAttacks()
    {
        if (_introTimer > 0f)
        {
            _introTimer = Mathf.Max(0f, _introTimer - Time.deltaTime);
            return;
        }

        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f)
        {
            return;
        }

        _attackCycleDuration = Mathf.Max(0.8f, attackInterval - (_tier * 0.08f));
        _attackTimer = _attackCycleDuration;
        int totalWeight = Mathf.Max(1, _profile.LaserWeight + _profile.MinionWeight + _profile.SweepWeight);
        int roll = Random.Range(0, totalWeight);
        if (roll < _profile.LaserWeight)
        {
            FireLaserPattern();
            return;
        }

        if (roll < _profile.LaserWeight + _profile.MinionWeight)
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
        GameManager.Instance?.AddScore(scoreReward + ((_tier - 1) * 120) + _profile.BonusScore);
        GameManager.Instance?.AddCredits(_profile.BossCredits);
        GameManager.Instance?.SetBossRewardPresentation(DisplayName, RewardTitle, _profile.BossCredits, _tier);
        GameManager.Instance?.RegisterBossDefeated(1);
        _owner?.CompleteEncounter(this);
        gameObject.SetActive(false);
    }

    private void CacheVisuals()
    {
        _eyeRenderer = FindNamedRenderer("BossEye") ?? FindNamedRenderer("Eye");
        _coreRenderer = FindNamedRenderer("BossCore") ?? FindNamedRenderer("Core");
        _shellRenderers = GetComponentsInChildren<Renderer>(true);
        _eyeMaterial = _eyeRenderer != null ? _eyeRenderer.material : null;
        _coreMaterial = _coreRenderer != null ? _coreRenderer.material : null;
        _shellMaterials = new Material[_shellRenderers.Length];
        for (int i = 0; i < _shellRenderers.Length; i++)
        {
            _shellMaterials[i] = _shellRenderers[i] != null ? _shellRenderers[i].material : null;
        }
    }

    private Renderer FindNamedRenderer(string name)
    {
        Transform child = transform.Find(name);
        return child != null ? child.GetComponent<Renderer>() : null;
    }

    private void UpdateVisuals()
    {
        float deltaTime = Time.deltaTime;
        _damageFlash = Mathf.Max(0f, _damageFlash - deltaTime);
        _hackFlash = Mathf.Max(0f, _hackFlash - deltaTime);

        float healthRatio = MaxHealth > 0 ? Mathf.Clamp01((float)_currentHealth / MaxHealth) : 1f;
        float attackCharge = _attackCycleDuration > 0f ? Mathf.Clamp01(1f - (_attackTimer / _attackCycleDuration)) : 0f;
        float pulse = 0.75f + (Mathf.Sin((Time.time * 9f) + _hoverSeed) * 0.25f);
        float introCharge = introDuration > 0f ? 1f - Mathf.Clamp01(_introTimer / introDuration) : 1f;

        Color threat = Color.Lerp(_profile.EyeColor == default ? new Color(0.1f, 0.86f, 1f) : _profile.EyeColor, new Color(1f, 0.2f, 0.28f), attackCharge);
        threat = Color.Lerp(threat, new Color(0.3f, 1f, 1f), _hackFlash);
        threat = Color.Lerp(threat * 0.6f, threat, introCharge);
        threat = Color.Lerp(threat, Color.white, _damageFlash * 0.85f);

        ApplyEmission(_eyeMaterial, threat, Mathf.Lerp(1.2f, 4.6f, Mathf.Max(attackCharge, introCharge)) + (_hackFlash * 1.2f));
        ApplyEmission(_coreMaterial, Color.Lerp(_profile.CoreColor == default ? new Color(1f, 0.22f, 0.32f) : _profile.CoreColor, new Color(1f, 0.55f, 0.18f), 1f - healthRatio), 1.2f + (pulse * 0.8f) + (introCharge * 0.9f));

        if (_eyeRenderer != null)
        {
            _eyeRenderer.transform.localScale = new Vector3(1.62f + (attackCharge * 0.15f) + (introCharge * 0.12f), 0.22f + (pulse * 0.04f), 0.18f);
        }

        for (int i = 0; i < _shellRenderers.Length; i++)
        {
            Renderer renderer = _shellRenderers[i];
            Material material = _shellMaterials[i];
            if (renderer == null || material == null || renderer == _eyeRenderer || renderer == _coreRenderer)
            {
                continue;
            }

            float shellGlow = 0.24f + (attackCharge * 0.65f) + (_damageFlash * 0.4f);
            Color shellColor = _profile.ShellColor == default ? new Color(0.12f, 0.06f, 0.14f) : _profile.ShellColor;
            ApplyEmission(material, Color.Lerp(shellColor, new Color(0.32f, 0.08f, 0.18f), 1f - healthRatio), shellGlow);
        }
    }

    private void ApplyProfileVisuals()
    {
        Vector3 targetScale = _profile.BossScale == default ? transform.localScale : _profile.BossScale;
        transform.localScale = targetScale;

        if (_eyeMaterial != null)
        {
            ApplyBaseColor(_eyeMaterial, _profile.EyeColor == default ? new Color(0.08f, 0.9f, 1f) : _profile.EyeColor);
        }

        if (_coreMaterial != null)
        {
            ApplyBaseColor(_coreMaterial, _profile.CoreColor == default ? new Color(1f, 0.22f, 0.34f) : _profile.CoreColor);
        }

        for (int i = 0; i < _shellMaterials.Length; i++)
        {
            Material material = _shellMaterials[i];
            if (material == null || material == _eyeMaterial || material == _coreMaterial)
            {
                continue;
            }

            ApplyBaseColor(material, _profile.ShellColor == default ? new Color(0.1f, 0.08f, 0.14f) : _profile.ShellColor);
        }
    }

    private static BossProfile ResolveProfile(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 2:
                return new BossProfile
                {
                    Name = "Freight Serpent",
                    RewardTitle = "Cargo Vault",
                    EyeColor = new Color(1f, 0.66f, 0.2f),
                    CoreColor = new Color(1f, 0.42f, 0.18f),
                    ShellColor = new Color(0.16f, 0.1f, 0.06f),
                    BossScale = new Vector3(3.5f, 1.1f, 5.4f),
                    FollowDistance = 28f,
                    HoverHeight = 2.8f,
                    HoverAmplitude = 0.28f,
                    HoverFrequency = 2f,
                    AttackInterval = 1.75f,
                    LaserWeight = 1,
                    MinionWeight = 1,
                    SweepWeight = 4,
                    BonusScore = 180,
                    BossCredits = 160
                };
            case 3:
                return new BossProfile
                {
                    Name = "Warden Pair",
                    RewardTitle = "Security Cache",
                    EyeColor = new Color(1f, 0.18f, 0.54f),
                    CoreColor = new Color(0.32f, 0.94f, 1f),
                    ShellColor = new Color(0.12f, 0.06f, 0.18f),
                    BossScale = new Vector3(2.5f, 1.3f, 4.2f),
                    FollowDistance = 25f,
                    HoverHeight = 3.4f,
                    HoverAmplitude = 0.56f,
                    HoverFrequency = 3.8f,
                    AttackInterval = 1.45f,
                    LaserWeight = 3,
                    MinionWeight = 1,
                    SweepWeight = 2,
                    BonusScore = 260,
                    BossCredits = 220
                };
            case 4:
                return new BossProfile
                {
                    Name = "Citadel Core Rider",
                    RewardTitle = "Citadel Reactor Chest",
                    EyeColor = new Color(1f, 0.16f, 0.22f),
                    CoreColor = new Color(1f, 0.82f, 0.26f),
                    ShellColor = new Color(0.18f, 0.04f, 0.1f),
                    BossScale = new Vector3(3.9f, 1.6f, 4.8f),
                    FollowDistance = 30f,
                    HoverHeight = 3.7f,
                    HoverAmplitude = 0.62f,
                    HoverFrequency = 4.4f,
                    AttackInterval = 1.2f,
                    LaserWeight = 3,
                    MinionWeight = 2,
                    SweepWeight = 3,
                    BonusScore = 420,
                    BossCredits = 320
                };
            default:
                return new BossProfile
                {
                    Name = "Gatekeeper Drone",
                    RewardTitle = "Checkpoint Crate",
                    EyeColor = new Color(0.1f, 0.86f, 1f),
                    CoreColor = new Color(1f, 0.22f, 0.34f),
                    ShellColor = new Color(0.12f, 0.06f, 0.14f),
                    BossScale = Vector3.zero,
                    FollowDistance = 24f,
                    HoverHeight = 3.2f,
                    HoverAmplitude = 0.45f,
                    HoverFrequency = 2.8f,
                    AttackInterval = 2f,
                    LaserWeight = 2,
                    MinionWeight = 1,
                    SweepWeight = 1,
                    BonusScore = 0,
                    BossCredits = 100
                };
        }
    }

    private static void ApplyBaseColor(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
    }

    private void ApplyEmission(Material material, Color color, float intensity)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }
    }
}
