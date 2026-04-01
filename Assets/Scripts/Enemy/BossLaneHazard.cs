using UnityEngine;

public sealed class BossLaneHazard : MonoBehaviour
{
    private enum HazardMode
    {
        LaneStrike,
        Sweep
    }

    [SerializeField] private Collider damageCollider;
    [SerializeField] private Renderer telegraphRenderer;
    [SerializeField] private Renderer activeRenderer;
    [SerializeField] private float telegraphPulseSpeed = 9f;

    private HazardMode _mode;
    private float _timer;
    private float _telegraphDuration;
    private float _activeDuration;
    private float _startX;
    private float _endX;
    private int _damage;
    private bool _isActive;
    private PooledObject _pooledObject;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        damageCollider = damageCollider == null ? GetComponent<Collider>() : damageCollider;
        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        SetDamageActive(false);
        SetVisualAlpha(0.45f);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (!_isActive)
        {
            UpdateTelegraph();
            return;
        }

        UpdateActivePhase();
    }

    public void ConfigureLaneStrike(float laneX, float zPosition, float telegraphDuration, float activeDuration, int damage)
    {
        _mode = HazardMode.LaneStrike;
        _startX = laneX;
        _endX = laneX;
        ConfigureCommon(zPosition, telegraphDuration, activeDuration, damage);
    }

    public void ConfigureSweep(float startX, float endX, float zPosition, float telegraphDuration, float activeDuration, int damage)
    {
        _mode = HazardMode.Sweep;
        _startX = startX;
        _endX = endX;
        ConfigureCommon(zPosition, telegraphDuration, activeDuration, damage);
    }

    private void ConfigureCommon(float zPosition, float telegraphDuration, float activeDuration, int damage)
    {
        _telegraphDuration = Mathf.Max(0.1f, telegraphDuration);
        _activeDuration = Mathf.Max(0.1f, activeDuration);
        _damage = Mathf.Max(1, damage);
        _timer = 0f;
        _isActive = false;
        transform.position = new Vector3(_startX, transform.position.y, zPosition);
        SetDamageActive(false);
        SetVisualAlpha(0.4f);
    }

    private void UpdateTelegraph()
    {
        float t = Mathf.Clamp01(_timer / _telegraphDuration);
        SetVisualAlpha(Mathf.Lerp(0.25f, 0.85f, Mathf.PingPong(Time.time * telegraphPulseSpeed, 1f)));
        if (t >= 1f)
        {
            _isActive = true;
            _timer = 0f;
            SetDamageActive(true);
            SetVisualAlpha(1f);
        }
    }

    private void UpdateActivePhase()
    {
        float t = Mathf.Clamp01(_timer / _activeDuration);
        if (_mode == HazardMode.Sweep)
        {
            float nextX = Mathf.Lerp(_startX, _endX, t);
            Vector3 position = transform.position;
            position.x = nextX;
            transform.position = position;
        }

        if (t >= 1f)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || !_isActive)
        {
            return;
        }

        player.TakeHit(_damage);
    }

    private void SetDamageActive(bool enabled)
    {
        if (damageCollider != null)
        {
            damageCollider.enabled = enabled;
        }

        if (telegraphRenderer != null)
        {
            telegraphRenderer.enabled = !enabled;
        }

        if (activeRenderer != null)
        {
            activeRenderer.enabled = enabled;
        }
    }

    private void SetVisualAlpha(float alpha)
    {
        if (telegraphRenderer == null)
        {
            return;
        }

        Color color = telegraphRenderer.sharedMaterial != null ? telegraphRenderer.sharedMaterial.color : Color.red;
        color.a = alpha;
        telegraphRenderer.material.color = color;
    }

    private void ReturnToPool()
    {
        _isActive = false;
        SetDamageActive(false);
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
