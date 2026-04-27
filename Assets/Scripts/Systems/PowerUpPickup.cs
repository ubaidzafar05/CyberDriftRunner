using UnityEngine;

public sealed class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpType powerUpType = PowerUpType.Shield;
    [SerializeField] private float duration = 5f;
    [SerializeField] private int scoreBonus = 20;
    [SerializeField] private float spinSpeed = 180f;

    private PooledObject pooledObject;
    private Vector3 initialScale;
    private Vector3 initialPosition;
    private Vector3 spinAxis;
    private float pulseSeed;
    private Renderer[] cachedRenderers;
    private Material[] cachedMaterials;
    private Color currentPrimaryColor;
    private Color currentAccentColor;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        FlatActorFacade.EnsurePickupFacade(gameObject, new Color(0.18f, 0.82f, 0.94f), new Color(1f, 0.9f, 0.32f), true);
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        CacheMaterials();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
        initialScale = transform.localScale;
        initialPosition = transform.position;
        pulseSeed = Random.Range(0f, Mathf.PI * 2f);
        ApplyVisualProfile();
    }

    private void Update()
    {
        transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
        float pulse = 1f + (Mathf.Sin((Time.time * GetPulseFrequency()) + pulseSeed) * 0.08f);
        transform.localScale = initialScale * pulse;
        float bobOffset = Mathf.Sin((Time.time * 2.8f) + pulseSeed) * GetBobAmplitude();
        transform.position = new Vector3(initialPosition.x, initialPosition.y + bobOffset, initialPosition.z);
        UpdateEmissionPulse();
        ReturnWhenBehindPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.PowerUps.ApplyPowerUp(powerUpType, duration);
        GameManager.Instance?.AddScore(scoreBonus);
        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateOnPowerUp();
        player.GetComponent<PlayerVfxController>()?.OnPowerUp(powerUpType);
        ScreenFlash.Instance?.FlashPowerUp();
        ReturnToPool();
    }

    private void ReturnWhenBehindPlayer()
    {
        if (GameManager.Instance?.Player == null)
        {
            return;
        }

        if (transform.position.z < GameManager.Instance.Player.transform.position.z - 8f)
        {
            ReturnToPool();
        }
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

    private void ApplyVisualProfile()
    {
        Color primary;
        Color accent;
        switch (powerUpType)
        {
            case PowerUpType.DoubleScore:
                primary = new Color(1f, 0.82f, 0.16f);
                accent = new Color(1f, 0.45f, 0.18f);
                spinAxis = Vector3.forward;
                break;
            case PowerUpType.SlowMotion:
                primary = new Color(0.44f, 0.82f, 1f);
                accent = new Color(0.58f, 0.24f, 1f);
                spinAxis = new Vector3(0.4f, 1f, 0.35f).normalized;
                break;
            case PowerUpType.EmpBlast:
                primary = new Color(1f, 0.22f, 0.72f);
                accent = new Color(0.22f, 0.96f, 1f);
                spinAxis = Vector3.right;
                break;
            case PowerUpType.Magnet:
                primary = new Color(1f, 0.7f, 0.22f);
                accent = new Color(0.34f, 1f, 0.7f);
                spinAxis = new Vector3(1f, 0.45f, 0f).normalized;
                break;
            case PowerUpType.SpeedBoost:
                primary = new Color(1f, 0.5f, 0.16f);
                accent = new Color(1f, 0.08f, 0.5f);
                spinAxis = new Vector3(0.5f, 0.2f, 1f).normalized;
                break;
            default:
                primary = new Color(0.18f, 0.82f, 0.94f);
                accent = new Color(0.8f, 1f, 1f);
                spinAxis = Vector3.up;
                break;
        }

        if (cachedRenderers == null)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        CacheMaterials();

        currentPrimaryColor = primary;
        currentAccentColor = accent;
        spinSpeed = GetSpinSpeedForType();

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material material = renderer.material;
            string lowerName = renderer.transform.name.ToLowerInvariant();
            Color color = lowerName.Contains("aura") ? primary * 0.85f
                : lowerName.Contains("core") ? Color.Lerp(primary, accent, 0.28f)
                : lowerName.Contains("cross") || lowerName.Contains("glyph") ? accent
                : primary;

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                float emission = lowerName.Contains("aura") ? 2.8f : lowerName.Contains("core") ? 2.1f : 1.7f;
                material.SetColor("_EmissionColor", color * emission);
            }
        }
    }

    private float GetPulseFrequency()
    {
        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                return 6f;
            case PowerUpType.SlowMotion:
                return 3f;
            default:
                return 4.5f;
        }
    }

    private float GetBobAmplitude()
    {
        switch (powerUpType)
        {
            case PowerUpType.EmpBlast:
                return 0.16f;
            case PowerUpType.Magnet:
                return 0.12f;
            default:
                return 0.1f;
        }
    }

    private void CacheMaterials()
    {
        if (cachedRenderers == null)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        cachedMaterials = new Material[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            cachedMaterials[i] = cachedRenderers[i] != null ? cachedRenderers[i].material : null;
        }
    }

    private float GetSpinSpeedForType()
    {
        switch (powerUpType)
        {
            case PowerUpType.EmpBlast:
                return 260f;
            case PowerUpType.SpeedBoost:
                return 320f;
            case PowerUpType.SlowMotion:
                return 120f;
            default:
                return spinSpeed;
        }
    }

    private void UpdateEmissionPulse()
    {
        if (cachedMaterials == null || cachedRenderers == null)
        {
            return;
        }

        float basePulse = 1.2f + (Mathf.Sin((Time.time * GetPulseFrequency()) + pulseSeed) * 0.35f);
        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            Material material = cachedMaterials[i];
            Renderer renderer = cachedRenderers[i];
            if (material == null || renderer == null || !material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            string lowerName = renderer.transform.name.ToLowerInvariant();
            Color emissionColor = lowerName.Contains("aura")
                ? currentPrimaryColor
                : lowerName.Contains("core") || lowerName.Contains("pulse")
                    ? Color.Lerp(currentPrimaryColor, currentAccentColor, 0.4f)
                    : currentAccentColor;
            float boost = lowerName.Contains("aura") ? 1.9f : lowerName.Contains("core") ? 2.4f : 1.7f;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor * (boost * basePulse));
        }
    }
}
