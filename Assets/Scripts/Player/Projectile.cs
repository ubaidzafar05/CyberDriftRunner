using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float maxLifetime = 3f;

    private int _damage;
    private float _speed;
    private float _despawnAt;
    private IDamageable _target;
    private MonoBehaviour _targetBehaviour;
    private PooledObject _pooledObject;
    private Renderer _renderer;
    private Material _runtimeMaterial;
    private Vector3 _baseScale;
    private Renderer _coreRenderer;
    private Renderer _finLeftRenderer;
    private Renderer _finRightRenderer;
    private Renderer _tailRenderer;
    private Renderer _ringRenderer;
    private Renderer _haloRenderer;
    private Material _coreMaterial;
    private Material _finLeftMaterial;
    private Material _finRightMaterial;
    private Material _tailMaterial;
    private Material _ringMaterial;
    private Material _haloMaterial;
    private TrailRenderer _trailRenderer;
    private WeaponType _weaponType;
    private Vector3 _coreBaseScale;
    private Vector3 _ringBaseScale;
    private Vector3 _haloBaseScale;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        _renderer = GetComponent<Renderer>();
        _runtimeMaterial = _renderer != null ? _renderer.material : null;
        _baseScale = transform.localScale;
        EnsureVisualRig();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        _despawnAt = Time.time + maxLifetime;
        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
        }
    }

    private void Update()
    {
        if (_targetBehaviour == null || !_target.IsAlive || Time.time >= _despawnAt)
        {
            ReturnToPool();
            return;
        }

        Transform targetTransform = _targetBehaviour.transform;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetTransform.position, _speed * Time.deltaTime);
        transform.position = nextPosition;
        transform.LookAt(targetTransform.position);
        UpdateVisualMotion();

        if (Vector3.SqrMagnitude(transform.position - targetTransform.position) <= hitRadius * hitRadius)
        {
            _target.TakeDamage(_damage, transform.position);
            ReturnToPool();
        }
    }

    public void Launch(IDamageable nextTarget, float projectileSpeed, int projectileDamage, WeaponType weaponType, float scaleMultiplier = 1f, Color? tint = null)
    {
        _target = nextTarget;
        _targetBehaviour = nextTarget as MonoBehaviour;
        _speed = projectileSpeed;
        _damage = projectileDamage;
        _weaponType = weaponType;
        _despawnAt = Time.time + maxLifetime;
        ApplyWeaponProfile(scaleMultiplier, tint ?? new Color(0.15f, 0.95f, 1f));
    }

    private void EnsureVisualRig()
    {
        _coreRenderer = EnsurePrimitiveChild("Core", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.2f), Vector3.one * 0.45f);
        _finLeftRenderer = EnsurePrimitiveChild("FinLeft", PrimitiveType.Cube, new Vector3(-0.18f, 0f, -0.02f), new Vector3(0.08f, 0.08f, 0.42f));
        _finRightRenderer = EnsurePrimitiveChild("FinRight", PrimitiveType.Cube, new Vector3(0.18f, 0f, -0.02f), new Vector3(0.08f, 0.08f, 0.42f));
        _tailRenderer = EnsurePrimitiveChild("Tail", PrimitiveType.Cube, new Vector3(0f, 0f, -0.24f), new Vector3(0.12f, 0.12f, 0.24f));
        _ringRenderer = EnsurePrimitiveChild("Ring", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.02f), new Vector3(0.42f, 0.05f, 0.42f));
        _haloRenderer = EnsurePrimitiveChild("Halo", PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), Vector3.one * 0.7f);

        _coreMaterial = _coreRenderer != null ? _coreRenderer.material : null;
        _finLeftMaterial = _finLeftRenderer != null ? _finLeftRenderer.material : null;
        _finRightMaterial = _finRightRenderer != null ? _finRightRenderer.material : null;
        _tailMaterial = _tailRenderer != null ? _tailRenderer.material : null;
        _ringMaterial = _ringRenderer != null ? _ringRenderer.material : null;
        _haloMaterial = _haloRenderer != null ? _haloRenderer.material : null;
        _coreBaseScale = _coreRenderer != null ? _coreRenderer.transform.localScale : Vector3.one * 0.45f;
        _ringBaseScale = _ringRenderer != null ? _ringRenderer.transform.localScale : new Vector3(0.42f, 0.05f, 0.42f);
        _haloBaseScale = _haloRenderer != null ? _haloRenderer.transform.localScale : Vector3.one * 0.7f;
        _trailRenderer = EnsureTrailRenderer();
    }

    private Renderer EnsurePrimitiveChild(string childName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject childObject = GameObject.CreatePrimitive(primitiveType);
            childObject.name = childName;
            childObject.transform.SetParent(transform, false);
            childObject.transform.localPosition = localPosition;
            childObject.transform.localScale = localScale;
            Collider collider = childObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            child = childObject.transform;
        }

        return child.GetComponent<Renderer>();
    }

    private TrailRenderer EnsureTrailRenderer()
    {
        Transform existing = transform.Find("Trail");
        if (existing != null)
        {
            return existing.GetComponent<TrailRenderer>();
        }

        GameObject trailObject = new GameObject("Trail");
        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = new Vector3(0f, 0f, -0.2f);
        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.startWidth = 0.14f;
        trail.endWidth = 0.02f;
        trail.emitting = true;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        trail.material = new Material(shader);
        return trail;
    }

    private void ApplyWeaponProfile(float scaleMultiplier, Color color)
    {
        float profileScale = Mathf.Max(0.5f, scaleMultiplier);
        Vector3 shellScale = _baseScale * profileScale;
        Vector3 coreScale = Vector3.one * 0.38f;
        Vector3 finScale = new Vector3(0.08f, 0.08f, 0.42f);
        Vector3 tailScale = new Vector3(0.12f, 0.12f, 0.24f);
        Vector3 ringScale = _ringBaseScale;
        Vector3 haloScale = Vector3.one * 0.7f;
        bool finsVisible = false;
        bool ringVisible = true;
        bool haloVisible = true;
        float trailTime = 0.18f;
        float trailWidth = 0.14f;

        switch (_weaponType)
        {
            case WeaponType.Burst:
                shellScale = _baseScale * (profileScale * 0.9f);
                coreScale = new Vector3(0.22f, 0.22f, 0.68f);
                finScale = new Vector3(0.06f, 0.1f, 0.48f);
                tailScale = new Vector3(0.08f, 0.08f, 0.34f);
                ringScale = new Vector3(0.24f, 0.08f, 0.52f);
                haloScale = new Vector3(0.48f, 0.22f, 0.48f);
                finsVisible = true;
                trailTime = 0.12f;
                trailWidth = 0.1f;
                break;
            case WeaponType.Spread:
                shellScale = new Vector3(_baseScale.x * profileScale * 1.35f, _baseScale.y * profileScale * 0.7f, _baseScale.z * profileScale * 0.82f);
                coreScale = new Vector3(0.62f, 0.12f, 0.54f);
                finScale = new Vector3(0.12f, 0.04f, 0.34f);
                tailScale = new Vector3(0.08f, 0.04f, 0.24f);
                ringScale = new Vector3(0.62f, 0.04f, 0.32f);
                haloScale = new Vector3(0.92f, 0.16f, 0.62f);
                finsVisible = true;
                trailTime = 0.16f;
                trailWidth = 0.16f;
                break;
            case WeaponType.Plasma:
                shellScale = _baseScale * (profileScale * 1.2f);
                coreScale = Vector3.one * 0.52f;
                tailScale = new Vector3(0.14f, 0.14f, 0.44f);
                ringScale = new Vector3(0.78f, 0.08f, 0.78f);
                haloScale = Vector3.one * 1.08f;
                trailTime = 0.28f;
                trailWidth = 0.22f;
                break;
            default:
                ringVisible = false;
                haloScale = new Vector3(0.52f, 0.3f, 0.52f);
                break;
        }

        transform.localScale = shellScale;
        if (_coreRenderer != null)
        {
            _coreRenderer.transform.localScale = coreScale;
            _coreBaseScale = coreScale;
        }

        if (_finLeftRenderer != null)
        {
            _finLeftRenderer.transform.localScale = finScale;
            _finLeftRenderer.enabled = finsVisible;
        }

        if (_finRightRenderer != null)
        {
            _finRightRenderer.transform.localScale = finScale;
            _finRightRenderer.enabled = finsVisible;
        }

        if (_tailRenderer != null)
        {
            _tailRenderer.transform.localScale = tailScale;
            _tailRenderer.enabled = _weaponType != WeaponType.Pulse;
        }

        if (_ringRenderer != null)
        {
            _ringRenderer.transform.localScale = ringScale;
            _ringBaseScale = ringScale;
            _ringRenderer.enabled = ringVisible;
        }

        if (_haloRenderer != null)
        {
            _haloRenderer.transform.localScale = haloScale;
            _haloBaseScale = haloScale;
            _haloRenderer.enabled = haloVisible;
        }

        if (_trailRenderer != null)
        {
            _trailRenderer.time = trailTime;
            _trailRenderer.startWidth = trailWidth;
            _trailRenderer.endWidth = trailWidth * 0.12f;
            _trailRenderer.startColor = new Color(color.r, color.g, color.b, 0.9f);
            _trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            _trailRenderer.Clear();
        }

        ApplyTint(color);
    }

    private void ApplyTint(Color color)
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        _runtimeMaterial.color = color;
        if (_runtimeMaterial.HasProperty("_EmissionColor"))
        {
            _runtimeMaterial.EnableKeyword("_EMISSION");
            _runtimeMaterial.SetColor("_EmissionColor", color * 1.8f);
        }

        ApplyMaterialTint(_coreMaterial, Color.Lerp(color, Color.white, 0.18f), 2.4f);
        ApplyMaterialTint(_finLeftMaterial, color * 0.92f, 1.5f);
        ApplyMaterialTint(_finRightMaterial, color * 0.92f, 1.5f);
        ApplyMaterialTint(_tailMaterial, Color.Lerp(color, new Color(0.2f, 0.08f, 0.4f), 0.25f), 1.25f);
        ApplyMaterialTint(_ringMaterial, Color.Lerp(color, Color.white, 0.32f), 1.9f);
        ApplyMaterialTint(_haloMaterial, Color.Lerp(color, new Color(0.32f, 0.12f, 0.58f), 0.3f), 1.2f);
        if (_trailRenderer != null && _trailRenderer.material != null)
        {
            ApplyMaterialTint(_trailRenderer.material, color, 1.4f);
        }
    }

    private void ApplyMaterialTint(Material material, Color color, float emissionBoost)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

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

    private void UpdateVisualMotion()
    {
        float pulse = 0.92f + (Mathf.Sin(Time.time * (_weaponType == WeaponType.Plasma ? 18f : 12f)) * 0.08f);
        if (_coreRenderer != null)
        {
            _coreRenderer.transform.localScale = _coreBaseScale * pulse;
        }

        if (_ringRenderer != null && _ringRenderer.enabled)
        {
            float ringPulse = 0.92f + (Mathf.Sin(Time.time * (_weaponType == WeaponType.Plasma ? 10f : 16f)) * 0.12f);
            _ringRenderer.transform.localScale = _ringBaseScale * ringPulse;
        }

        if (_haloRenderer != null && _haloRenderer.enabled)
        {
            float haloPulse = 0.94f + (Mathf.Sin((Time.time * 8f) + 0.7f) * 0.08f);
            _haloRenderer.transform.localScale = _haloBaseScale * haloPulse;
        }

        if (_weaponType == WeaponType.Spread)
        {
            transform.Rotate(0f, 0f, 420f * Time.deltaTime, Space.Self);
            if (_ringRenderer != null)
            {
                _ringRenderer.transform.Rotate(0f, 0f, -640f * Time.deltaTime, Space.Self);
            }
        }
        else if (_weaponType == WeaponType.Plasma)
        {
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
            if (_haloRenderer != null)
            {
                _haloRenderer.transform.Rotate(480f * Time.deltaTime, 0f, 240f * Time.deltaTime, Space.Self);
            }
        }
        else if (_weaponType == WeaponType.Burst && _ringRenderer != null)
        {
            _ringRenderer.transform.Rotate(520f * Time.deltaTime, 0f, 0f, Space.Self);
        }
    }

    private void ReturnToPool()
    {
        _target = null;
        _targetBehaviour = null;
        _weaponType = WeaponType.Pulse;
        transform.localScale = _baseScale;
        if (_coreRenderer != null)
        {
            _coreRenderer.transform.localScale = Vector3.one * 0.45f;
        }

        if (_ringRenderer != null)
        {
            _ringRenderer.transform.localScale = _ringBaseScale;
        }

        if (_haloRenderer != null)
        {
            _haloRenderer.transform.localScale = _haloBaseScale;
        }

        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
        }

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
