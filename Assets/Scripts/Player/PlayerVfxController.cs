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
    private ParticleSystem _speedStream;
    private int _burstIndex;

    private void Awake()
    {
        targetRenderer = targetRenderer == null ? GetComponentInChildren<Renderer>() : targetRenderer;
        trailRenderer = trailRenderer == null ? CreateTrail() : trailRenderer;
        _runtimeMaterial = targetRenderer != null ? targetRenderer.material : null;
        _speedStream = CreateSpeedStream();
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
            trailRenderer.time = active ? 0.52f : 0.12f;
            trailRenderer.startWidth = active ? 0.7f : 0.38f;
            trailRenderer.endWidth = active ? 0.12f : 0.04f;
            trailRenderer.startColor = active ? new Color(0.2f, 1f, 1f, 0.96f) : new Color(0.2f, 0.8f, 1f, 0.32f);
            trailRenderer.endColor = active ? new Color(0.42f, 0f, 1f, 0f) : new Color(0.2f, 1f, 1f, 0f);
        }

        if (_speedStream != null)
        {
            var emission = _speedStream.emission;
            emission.rateOverTime = active ? 200f : 0f;
            if (active && !_speedStream.isPlaying)
            {
                _speedStream.Play(true);
            }

            if (!active && _speedStream.isPlaying)
            {
                _speedStream.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
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
        trail.time = 0.12f;
        trail.startWidth = 0.38f;
        trail.endWidth = 0.04f;

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

    private ParticleSystem CreateSpeedStream()
    {
        GameObject speedObject = new GameObject("SpeedStream");
        speedObject.transform.SetParent(transform, false);
        speedObject.transform.localPosition = new Vector3(0f, 0.9f, -0.55f);

        ParticleSystem system = speedObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = system.emission;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.25f;
        shape.angle = 8f;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0f, 0.96f, 1f), 0f), new GradientColorKey(new Color(0.42f, 0f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.z = new ParticleSystem.MinMaxCurve(-12f, -18f);

        var renderer = speedObject.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
        if (particleShader == null) particleShader = Shader.Find("Hidden/InternalErrorShader");
        renderer.material = new Material(particleShader);
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5f;
        renderer.velocityScale = 0.3f;
        return system;
    }

    private ParticleSystem CreatePooledBurst(int index)
    {
        GameObject burstObject = new GameObject($"BurstPool_{index}");
        burstObject.transform.SetParent(transform, false);
        burstObject.transform.localPosition = Vector3.up;

        ParticleSystem system = burstObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = 0.42f;
        main.startLifetime = 0.3f;
        main.startSpeed = 3.2f;
        main.startSize = 0.14f;
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
