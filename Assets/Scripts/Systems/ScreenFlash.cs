using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen color flash effect for impacts, power-ups, near-misses, etc.
/// Requires a full-screen Image on a top-level canvas overlay.
/// </summary>
public sealed class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [SerializeField] private Image flashImage;

    private float _flashDuration;
    private float _flashTimer;
    private Color _flashColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (flashImage != null)
        {
            flashImage.color = Color.clear;
            flashImage.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (flashImage == null || _flashTimer <= 0f)
        {
            return;
        }

        _flashTimer -= Time.unscaledDeltaTime;
        float alpha = Mathf.Clamp01(_flashTimer / _flashDuration) * _flashColor.a;
        flashImage.color = new Color(_flashColor.r, _flashColor.g, _flashColor.b, alpha);

        if (_flashTimer <= 0f)
        {
            flashImage.color = Color.clear;
        }
    }

    public void Flash(Color color, float duration = 0.2f)
    {
        if (flashImage == null)
        {
            return;
        }

        _flashColor = color;
        _flashDuration = Mathf.Max(0.01f, duration);
        _flashTimer = _flashDuration;
        flashImage.color = color;
    }

    public void FlashHit() => Flash(new Color(1f, 0.15f, 0.15f, 0.3f), 0.25f);
    public void FlashPowerUp() => Flash(new Color(0.2f, 1f, 1f, 0.2f), 0.2f);
    public void FlashDeath() => Flash(new Color(1f, 0f, 0f, 0.5f), 0.4f);
    public void FlashNearMiss() => Flash(new Color(1f, 1f, 0f, 0.15f), 0.15f);
    public void FlashRevive() => Flash(new Color(0.4f, 1f, 0.4f, 0.25f), 0.3f);

    public void SetFlashImage(Image image)
    {
        flashImage = image;
        if (flashImage != null)
        {
            flashImage.color = Color.clear;
            flashImage.raycastTarget = false;
        }
    }
}
