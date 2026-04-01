using UnityEngine;

/// <summary>
/// Dynamic runner camera with speed FOV, lane lean, fever framing, and death/revive beats.
/// </summary>
public sealed class DynamicCameraController : MonoBehaviour
{
    [Header("Base Position")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 5.2f, -8.5f);
    [SerializeField] private float followSmoothing = 8f;
    [SerializeField] private float lookHeight = 1.35f;
    [SerializeField] private float lookAheadDistance = 8f;

    [Header("Speed Response")]
    [SerializeField] private float fovBase = 60f;
    [SerializeField] private float fovMax = 80f;
    [SerializeField] private float fovSmoothing = 3f;
    [SerializeField] private float heightGainAtMaxSpeed = 1.6f;

    [Header("Lean")]
    [SerializeField] private float laneLeanAngle = 5f;
    [SerializeField] private float laneLeanSmoothing = 7f;
    [SerializeField] private float feverTiltAngle = 1.5f;

    [Header("Fever Zoom")]
    [SerializeField] private float feverFovBoost = 7f;

    [Header("Boss Framing")]
    [SerializeField] private float bossFovBoost = 9f;
    [SerializeField] private float bossCameraPullback = 2.4f;
    [SerializeField] private float bossHeightBoost = 1f;

    [Header("Death Zoom")]
    [SerializeField] private float deathZoomDuration = 0.8f;
    [SerializeField] private float deathFovTarget = 45f;

    [Header("Revive Beat")]
    [SerializeField] private float reviveFovBoost = 6f;
    [SerializeField] private float reviveBoostDuration = 0.55f;

    private Camera _camera;
    private float _currentFov;
    private float _targetFov;
    private float _currentRoll;
    private float _smoothedLateralVelocity;
    private float _lastPlayerX;
    private float _reviveBoostTimer;
    private bool _deathZoom;
    private bool _initializedPlayerTrack;
    private float _deathTimer;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _currentFov = fovBase;
        _targetFov = fovBase;
    }

    private void LateUpdate()
    {
        if (_camera == null || GameManager.Instance?.Player == null)
        {
            return;
        }

        Transform playerTransform = GameManager.Instance.Player.transform;
        UpdatePlayerTrack(playerTransform.position.x);
        UpdateFov();
        UpdatePosition(playerTransform);
        UpdateRotation(playerTransform);
        ApplyShake();
    }

    private void UpdatePlayerTrack(float playerX)
    {
        if (!_initializedPlayerTrack)
        {
            _lastPlayerX = playerX;
            _initializedPlayerTrack = true;
            return;
        }

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float lateralVelocity = (playerX - _lastPlayerX) / deltaTime;
        _lastPlayerX = playerX;
        _smoothedLateralVelocity = Mathf.Lerp(_smoothedLateralVelocity, lateralVelocity, laneLeanSmoothing * deltaTime);
    }

    private void UpdateFov()
    {
        float speedPercent = Mathf.InverseLerp(9f, 22f, GameManager.Instance.CurrentForwardSpeed);
        _targetFov = Mathf.Lerp(fovBase, fovMax, speedPercent);
        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            _targetFov += feverFovBoost;
        }

        if (GameManager.Instance.IsBossEncounterActive)
        {
            _targetFov += bossFovBoost;
        }

        if (_reviveBoostTimer > 0f)
        {
            _reviveBoostTimer = Mathf.Max(0f, _reviveBoostTimer - Time.unscaledDeltaTime);
            _targetFov += Mathf.Lerp(0f, reviveFovBoost, _reviveBoostTimer / Mathf.Max(0.01f, reviveBoostDuration));
        }

        if (_deathZoom)
        {
            _deathTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_deathTimer / deathZoomDuration);
            _currentFov = Mathf.Lerp(_currentFov, deathFovTarget, t);
        }
        else
        {
            _currentFov = Mathf.Lerp(_currentFov, _targetFov, fovSmoothing * Time.unscaledDeltaTime);
        }

        _camera.fieldOfView = _currentFov;
    }

    private void UpdatePosition(Transform playerTransform)
    {
        float speedPercent = Mathf.InverseLerp(9f, 22f, GameManager.Instance.CurrentForwardSpeed);
        Vector3 offset = baseOffset;
        offset.y += heightGainAtMaxSpeed * speedPercent;
        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            offset.z -= 1.6f;
        }

        if (GameManager.Instance.IsBossEncounterActive)
        {
            offset.z -= bossCameraPullback;
            offset.y += bossHeightBoost;
        }

        Vector3 targetPosition = playerTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothing * Time.unscaledDeltaTime);
    }

    private void UpdateRotation(Transform playerTransform)
    {
        Vector3 lookTarget = playerTransform.position + Vector3.up * lookHeight + Vector3.forward * lookAheadDistance;
        Quaternion lookRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);

        float rollTarget = Mathf.Clamp(-_smoothedLateralVelocity * 0.18f, -laneLeanAngle, laneLeanAngle);
        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            rollTarget += Mathf.Sin(Time.unscaledTime * 4f) * feverTiltAngle;
        }

        _currentRoll = Mathf.Lerp(_currentRoll, rollTarget, laneLeanSmoothing * Time.unscaledDeltaTime);
        transform.rotation = lookRotation * Quaternion.Euler(0f, 0f, _currentRoll);
    }

    private void ApplyShake()
    {
        if (ScreenShake.Instance == null || (SettingsManager.Instance != null && !SettingsManager.Instance.ScreenShakeEnabled))
        {
            return;
        }

        transform.position += ScreenShake.Instance.GetShakeOffset();
        transform.rotation *= Quaternion.Euler(0f, 0f, ScreenShake.Instance.GetShakeRotation());
    }

    public void OnPlayerDeath()
    {
        _deathZoom = true;
        _deathTimer = 0f;
    }

    public void OnPlayerRevive()
    {
        ResetCamera();
        _reviveBoostTimer = reviveBoostDuration;
    }

    public void ResetCamera()
    {
        _deathZoom = false;
        _deathTimer = 0f;
        _currentRoll = 0f;
        _smoothedLateralVelocity = 0f;
        _currentFov = fovBase;
    }
}
