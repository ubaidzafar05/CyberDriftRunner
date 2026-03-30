using UnityEngine;

/// <summary>
/// Smooth dynamic camera that adjusts FOV and height based on speed and game state.
/// Replaces the static CameraFollow offset with dynamic cinematics.
/// </summary>
public sealed class DynamicCameraController : MonoBehaviour
{
    [Header("Base Position")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 5f, -8f);
    [SerializeField] private float followSmoothing = 8f;

    [Header("Speed Response")]
    [SerializeField] private float fovBase = 60f;
    [SerializeField] private float fovMax = 80f;
    [SerializeField] private float fovSmoothing = 3f;
    [SerializeField] private float heightGainAtMaxSpeed = 1.5f;

    [Header("Fever Zoom")]
    [SerializeField] private float feverFovBoost = 8f;
    [SerializeField] private float feverTiltAngle = 2f;

    [Header("Death Zoom")]
    [SerializeField] private float deathZoomDuration = 0.8f;
    [SerializeField] private float deathFovTarget = 45f;

    private Camera _camera;
    private float _currentFov;
    private float _targetFov;
    private bool _deathZoom;
    private float _deathTimer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null) _camera = Camera.main;
        _currentFov = fovBase;
        _targetFov = fovBase;
    }

    private void LateUpdate()
    {
        if (_camera == null) return;

        UpdateFov();
        UpdatePosition();

        // Apply screen shake
        if (ScreenShake.Instance != null &&
            (SettingsManager.Instance == null || SettingsManager.Instance.ScreenShakeEnabled))
        {
            transform.position += ScreenShake.Instance.GetShakeOffset();
            transform.rotation *= Quaternion.Euler(0f, 0f, ScreenShake.Instance.GetShakeRotation());
        }
    }

    private void UpdateFov()
    {
        if (GameManager.Instance == null || _camera == null) return;

        float speedPercent = Mathf.InverseLerp(9f, 22f, GameManager.Instance.CurrentForwardSpeed);
        _targetFov = Mathf.Lerp(fovBase, fovMax, speedPercent);

        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            _targetFov += feverFovBoost;
        }

        if (_deathZoom)
        {
            _deathTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_deathTimer / deathZoomDuration);
            _currentFov = Mathf.Lerp(_currentFov, deathFovTarget, t);
        }
        else
        {
            _currentFov = Mathf.Lerp(_currentFov, _targetFov, fovSmoothing * Time.deltaTime);
        }

        _camera.fieldOfView = _currentFov;
    }

    private void UpdatePosition()
    {
        if (GameManager.Instance?.Player == null) return;

        Transform playerTransform = GameManager.Instance.Player.transform;
        float speedPercent = Mathf.InverseLerp(9f, 22f, GameManager.Instance.CurrentForwardSpeed);

        Vector3 offset = baseOffset;
        offset.y += heightGainAtMaxSpeed * speedPercent;

        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            offset.z -= 1.5f; // Pull camera back during fever
        }

        Vector3 targetPos = playerTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSmoothing * Time.deltaTime);
        transform.LookAt(playerTransform.position + Vector3.up * 1.2f);
    }

    public void OnPlayerDeath()
    {
        _deathZoom = true;
        _deathTimer = 0f;
    }

    public void ResetCamera()
    {
        _deathZoom = false;
        _deathTimer = 0f;
        _currentFov = fovBase;
    }
}
