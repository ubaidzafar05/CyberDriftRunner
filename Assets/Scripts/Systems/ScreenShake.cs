using UnityEngine;

public sealed class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Shake Settings")]
    [SerializeField] private float traumaDecayRate = 1.8f;
    [SerializeField] private float maxAngle = 4f;
    [SerializeField] private float maxOffset = 0.35f;
    [SerializeField] private float frequency = 22f;

    [Header("Presets")]
    [SerializeField] private float hitTrauma = 0.6f;
    [SerializeField] private float deathTrauma = 1f;
    [SerializeField] private float empTrauma = 0.45f;
    [SerializeField] private float collectTrauma = 0.12f;

    private float _trauma;
    private float _seed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _seed = Random.Range(0f, 1000f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddTrauma(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    public void ShakeHit() => AddTrauma(hitTrauma);
    public void ShakeDeath() => AddTrauma(deathTrauma);
    public void ShakeEmp() => AddTrauma(empTrauma);
    public void ShakeCollect() => AddTrauma(collectTrauma);

    public Vector3 GetShakeOffset()
    {
        if (_trauma <= 0.001f)
        {
            return Vector3.zero;
        }

        float shake = _trauma * _trauma;
        float time = Time.unscaledTime * frequency;

        float offsetX = maxOffset * shake * (Mathf.PerlinNoise(_seed, time) * 2f - 1f);
        float offsetY = maxOffset * shake * (Mathf.PerlinNoise(_seed + 1f, time) * 2f - 1f);

        _trauma = Mathf.Max(0f, _trauma - (traumaDecayRate * Time.unscaledDeltaTime));

        return new Vector3(offsetX, offsetY, 0f);
    }

    public float GetShakeRotation()
    {
        if (_trauma <= 0.001f)
        {
            return 0f;
        }

        float shake = _trauma * _trauma;
        float time = Time.unscaledTime * frequency;
        return maxAngle * shake * (Mathf.PerlinNoise(_seed + 2f, time) * 2f - 1f);
    }
}
