using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class NeonPropAnimator : MonoBehaviour
{
    [SerializeField] private Color pulseColor = Color.cyan;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float minEmission = 0.7f;
    [SerializeField] private float maxEmission = 1.3f;

    private Renderer _targetRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private Color _baseEmission = Color.black;
    private bool _animationEnabled = true;
    private float _emissionScale = 1f;

    public void Configure(Color color, float speed, float min, float max)
    {
        pulseColor = color;
        pulseSpeed = speed;
        minEmission = min;
        maxEmission = max;
    }

    public void SetQuality(bool animationEnabled, float emissionScale)
    {
        _animationEnabled = animationEnabled;
        _emissionScale = Mathf.Max(0f, emissionScale);
    }

    private void Awake()
    {
        _targetRenderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
        Material sharedMaterial = _targetRenderer != null ? _targetRenderer.sharedMaterial : null;
        if (sharedMaterial != null && sharedMaterial.HasProperty("_EmissionColor"))
        {
            _baseEmission = sharedMaterial.GetColor("_EmissionColor");
        }
    }

    private void Update()
    {
        if (_targetRenderer == null)
        {
            return;
        }

        float pulse = _animationEnabled
            ? Mathf.Lerp(minEmission, maxEmission, (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f) + 0.5f)
            : minEmission;
        Color emission = _baseEmission == Color.black ? pulseColor * pulse : _baseEmission * pulse;
        _targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_EmissionColor", emission * _emissionScale);
        _targetRenderer.SetPropertyBlock(_propertyBlock);
    }
}
