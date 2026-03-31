using UnityEngine;

public sealed class PlayerVfxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private Material trailMaterial;

    private const int BurstPoolSize = 4;

    private Material _runtimeMaterial;
    private ParticleSystem[] _burstPool;
    private int _burstIndex;

    private void Awake()
    {
        targetRenderer = targetRenderer == null ? GetComponentInChildren<Renderer>() : targetRenderer;
        trailRenderer = trailRenderer == null ? CreateTrail() : trailRenderer;
        _runtimeMaterial = targetRenderer != null ? targetRenderer.material : null;
        _burstPool = new ParticleSystem[BurstPoolSize];
        for (int i = 0; i < BurstPoolSize; i++)
        {
            _burstPool[i] = CreatePooledBurst(i);
        }

        SetHackState(false);
    }

    public void OnJump() => EmitBurst(Color.cyan, 18);
    public void OnSlide() => EmitBurst(new Color(1f, 0.4f, 1f), 16);
    public void OnShoot() => EmitBurst(new Color(0.2f, 1f, 1f), 12);
    public void OnHit() => EmitBurst(new Color(1f, 0.2f, 0.4f), 20);
    public void OnPowerUp() => EmitBurst(new Color(1f, 0.9f, 0.2f), 24);
    public void OnRevive() => EmitBurst(new Color(0.6f, 1f, 0.6f), 28);

    public void SetHackState(bool active)
    {
        if (trailRenderer != null)
        {
            trailRenderer.emitting = active;
            trailRenderer.time = active ? 0.35f : 0.08f;
            trailRenderer.startColor = active ? new Color(0.2f, 1f, 1f, 0.9f) : new Color(0.2f, 0.8f, 1f, 0.25f);
            trailRenderer.endColor = active ? new Color(1f, 0.2f, 1f, 0f) : new Color(0.2f, 1f, 1f, 0f);
        }

        if (_runtimeMaterial != null && _runtimeMaterial.HasProperty("_EmissionColor"))
        {
            _runtimeMaterial.EnableKeyword("_EMISSION");
            _runtimeMaterial.SetColor("_EmissionColor", active ? new Color(0.4f, 1.2f, 1.2f) : new Color(0.18f, 0.8f, 1f));
        }
    }

    private TrailRenderer CreateTrail()
    {
        GameObject trailObject = new GameObject("DashTrail");
        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = Vector3.zero;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.08f;
        trail.startWidth = 0.45f;
        trail.endWidth = 0.05f;

        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            Shader fallbackShader = Shader.Find("Sprites/Default");
            if (fallbackShader == null) fallbackShader = Shader.Find("UI/Default");
            if (fallbackShader == null) fallbackShader = Shader.Find("Hidden/InternalErrorShader");
            trail.material = new Material(fallbackShader);
        }

        trail.emitting = false;
        return trail;
    }

    private ParticleSystem CreatePooledBurst(int index)
    {
        GameObject burstObject = new GameObject($"BurstPool_{index}");
        burstObject.transform.SetParent(transform, false);
        burstObject.transform.localPosition = Vector3.up;

        ParticleSystem system = burstObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = 0.35f;
        main.startLifetime = 0.25f;
        main.startSpeed = 2.6f;
        main.startSize = 0.12f;
        main.startColor = Color.white;
        main.maxParticles = 32;
        main.loop = false;
        main.playOnAwake = false;

        var emission = system.emission;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var renderer = burstObject.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        if (particleShader == null) particleShader = Shader.Find("Hidden/InternalErrorShader");
        renderer.material = new Material(particleShader);

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return system;
    }

    private void EmitBurst(Color color, int particles)
    {
        if (_burstPool == null || _burstPool.Length == 0)
        {
            return;
        }

        ParticleSystem system = _burstPool[_burstIndex];
        _burstIndex = (_burstIndex + 1) % _burstPool.Length;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.startColor = color;
        main.maxParticles = particles;
        system.Emit(particles);
    }
}
