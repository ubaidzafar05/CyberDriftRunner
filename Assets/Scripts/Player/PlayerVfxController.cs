using UnityEngine;

public sealed class PlayerVfxController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private TrailRenderer trailRenderer;

    private Material runtimeMaterial;

    private void Awake()
    {
        targetRenderer = targetRenderer == null ? GetComponentInChildren<Renderer>() : targetRenderer;
        trailRenderer = trailRenderer == null ? CreateTrail() : trailRenderer;
        runtimeMaterial = targetRenderer != null ? targetRenderer.material : null;
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

        if (runtimeMaterial != null && runtimeMaterial.HasProperty("_EmissionColor"))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor("_EmissionColor", active ? new Color(0.4f, 1.2f, 1.2f) : new Color(0.18f, 0.8f, 1f));
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
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.emitting = false;
        return trail;
    }

    private void EmitBurst(Color color, int particles)
    {
        GameObject burstObject = new GameObject("Burst");
        burstObject.transform.SetParent(transform, false);
        burstObject.transform.localPosition = Vector3.up;

        ParticleSystem system = burstObject.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = 0.35f;
        main.startLifetime = 0.25f;
        main.startSpeed = 2.6f;
        main.startSize = 0.12f;
        main.startColor = color;
        main.maxParticles = particles;
        main.loop = false;

        var emission = system.emission;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        system.Emit(particles);
        Object.Destroy(burstObject, 1f);
    }
}
