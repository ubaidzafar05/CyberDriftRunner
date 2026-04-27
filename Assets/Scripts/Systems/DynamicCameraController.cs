using UnityEngine;

public sealed class DynamicCameraController : MonoBehaviour
{
    [Header("Base Position")]
    [SerializeField] private Vector3 baseOffset = new Vector3(2.05f, 4.7f, -11.2f);
    [SerializeField] private float followSmoothing = 7.5f;
    [SerializeField] private float lookHeight = 1.22f;
    [SerializeField] private float lookAheadDistance = 13.8f;
    [SerializeField] private float laneFramingOffset = 0.54f;
    [SerializeField] private float sideBias = 1.6f;
    [SerializeField] private float laneCenteringWeight = 0.38f;

    [Header("Speed Response")]
    [SerializeField] private float fovBase = 57f;
    [SerializeField] private float fovMax = 70f;
    [SerializeField] private float fovSmoothing = 3f;
    [SerializeField] private float heightGainAtMaxSpeed = 1.15f;
    [SerializeField] private float dollyBackAtMaxSpeed = 1.2f;
    [SerializeField] private float pitchDownAtMaxSpeed = 2.4f;

    [Header("Lean")]
    [SerializeField] private float laneLeanAngle = 3.4f;
    [SerializeField] private float laneLeanSmoothing = 7f;
    [SerializeField] private float feverTiltAngle = 1f;
    [SerializeField] private float yawIntoTurnAngle = 3.2f;

    [Header("Motion Bob")]
    [SerializeField] private float motionBobAmplitude = 0.08f;
    [SerializeField] private float motionBobFrequency = 1.8f;

    [Header("Fever Zoom")]
    [SerializeField] private float feverFovBoost = 4f;

    [Header("Boss Framing")]
    [SerializeField] private float bossFovBoost = 6f;
    [SerializeField] private float bossCameraPullback = 2f;
    [SerializeField] private float bossHeightBoost = 0.8f;
    [SerializeField] private float bossLookBlend = 0.42f;
    [SerializeField] private float bossSidePull = 0.9f;

    [Header("Death Zoom")]
    [SerializeField] private float deathZoomDuration = 0.8f;
    [SerializeField] private float deathFovTarget = 43f;

    [Header("Revive Beat")]
    [SerializeField] private float reviveFovBoost = 3.5f;
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
    private float _currentYaw;
    private float _currentPitch;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera != null)
        {
            _camera.orthographic = false;
            _camera.fieldOfView = fovBase;
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
        offset.z -= dollyBackAtMaxSpeed * speedPercent;
        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            offset.z -= 1.6f;
        }

        if (GameManager.Instance.IsBossEncounterActive)
        {
            offset.z -= bossCameraPullback;
            offset.y += bossHeightBoost;
            offset.x -= bossSidePull;
        }

        offset.x += sideBias;
        offset.x += playerTransform.position.x * laneCenteringWeight;
        offset.x += Mathf.Clamp(_smoothedLateralVelocity * 0.015f, -laneFramingOffset, laneFramingOffset);
        offset.y += Mathf.Sin(Time.unscaledTime * motionBobFrequency) * motionBobAmplitude * Mathf.Lerp(0.45f, 1f, speedPercent);
        Vector3 targetPosition = playerTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothing * Time.unscaledDeltaTime);
    }

    private void UpdateRotation(Transform playerTransform)
    {
        Vector3 lookTarget = playerTransform.position + Vector3.up * lookHeight + Vector3.forward * lookAheadDistance;
        if (GameManager.Instance.IsBossEncounterActive && GameManager.Instance.ActiveBoss != null)
        {
            Vector3 bossTarget = GameManager.Instance.ActiveBoss.transform.position + (Vector3.up * 0.55f);
            lookTarget = Vector3.Lerp(lookTarget, (lookTarget + bossTarget) * 0.5f, bossLookBlend);
        }

        Quaternion lookRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);

        float rollTarget = Mathf.Clamp(-_smoothedLateralVelocity * 0.18f, -laneLeanAngle, laneLeanAngle);
        float yawTarget = Mathf.Clamp(-_smoothedLateralVelocity * 0.12f, -yawIntoTurnAngle, yawIntoTurnAngle);
        float speedPercent = Mathf.InverseLerp(9f, 22f, GameManager.Instance.CurrentForwardSpeed);
        float pitchTarget = Mathf.Lerp(0f, -pitchDownAtMaxSpeed, speedPercent);
        if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
        {
            rollTarget += Mathf.Sin(Time.unscaledTime * 4f) * feverTiltAngle;
        }

        _currentRoll = Mathf.Lerp(_currentRoll, rollTarget, laneLeanSmoothing * Time.unscaledDeltaTime);
        _currentYaw = Mathf.Lerp(_currentYaw, yawTarget, laneLeanSmoothing * Time.unscaledDeltaTime);
        _currentPitch = Mathf.Lerp(_currentPitch, pitchTarget, laneLeanSmoothing * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation * Quaternion.Euler(_currentPitch, _currentYaw, _currentRoll),
            laneLeanSmoothing * Time.unscaledDeltaTime);
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
        _currentYaw = 0f;
        _currentPitch = 0f;
        _smoothedLateralVelocity = 0f;
        _currentFov = fovBase;
    }
}
