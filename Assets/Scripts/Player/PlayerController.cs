using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private ShootingSystem shootingSystem;
    [SerializeField] private PowerUpSystem powerUps;
    [SerializeField] private PlayerVfxController vfxController;
    [SerializeField] private PlayerSkinApplier skinApplier;

    [Header("Movement")]
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private float laneSmoothTime = 0.06f;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float slideDuration = 0.68f;
    [SerializeField] private float slideHeight = 1f;
    [SerializeField] private float jumpBufferWindow = 0.12f;
    [SerializeField] private float groundedGraceWindow = 0.1f;
    [SerializeField] private float safeLaneSnapThreshold = 0.3f;

    [Header("Input")]
    [SerializeField] private float swipeThresholdPixels = 70f;
    [SerializeField] private float tapMaxDuration = 0.2f;
    [SerializeField] private float swipeDominanceRatio = 1.2f;

    [Header("Hacking")]
    [SerializeField] private float hackSlowScale = 0.45f;
    [SerializeField] private float deathSequenceTimeScale = 0.18f;
    [SerializeField] private float hackPulseInterval = 0.55f;
    [SerializeField] private float hackRange = 18f;

    [Header("Revive")]
    [SerializeField] private float reviveForwardOffset = 4f;
    [SerializeField] private float reviveHeightOffset = 0.12f;
    [SerializeField] private float reviveImmunityDuration = 1.25f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 1;

    private int desiredLane;
    private int currentHealth;
    private int trackedTouchId = -1;
    private int lastSafeLane;
    private bool isAlive = true;
    private bool isSliding;
    private bool hackHeld;
    private bool mouseTracking;
    private bool reviveRecoveryPending;
    private float lateralVelocity;
    private float verticalVelocity;
    private float slideTimeLeft;
    private float nextHackPulseTime;
    private float jumpBufferTimeLeft;
    private float groundedGraceTimeLeft;
    private float autoShootCooldown = 0.35f;
    private float autoShootTimer;
    private float damageImmunityTimeLeft;
    private float originalHeight;
    private Vector3 originalCenter;
    private Vector3 lastSafeGroundedPosition;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private Coroutine deathSequenceRoutine;
    private Coroutine reviveRecoveryRoutine;

    public PowerUpSystem PowerUps => powerUps;
    public bool IsAlive => isAlive;

    private void Awake()
    {
        characterController = characterController == null ? GetComponent<CharacterController>() : characterController;
        shootingSystem = shootingSystem == null ? GetComponent<ShootingSystem>() : shootingSystem;
        powerUps = powerUps == null ? GetComponent<PowerUpSystem>() : powerUps;
        skinApplier = skinApplier == null ? GetComponent<PlayerSkinApplier>() : skinApplier;
        if (skinApplier == null)
        {
            skinApplier = gameObject.AddComponent<PlayerSkinApplier>();
        }

        vfxController = vfxController == null ? GetComponent<PlayerVfxController>() : vfxController;
        if (vfxController == null)
        {
            vfxController = gameObject.AddComponent<PlayerVfxController>();
        }

        originalHeight = characterController.height;
        originalCenter = characterController.center;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameManager.Instance?.RegisterPlayer(this);
        skinApplier?.ApplySelectedSkin();
        CacheSafeGroundedState();
    }

    private void OnDisable()
    {
        StopDeathSequence();
        StopReviveRecovery();
        GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
    }

    public void Configure(CharacterController controller, ShootingSystem shooting, PowerUpSystem powerUpSystem)
    {
        characterController = controller;
        shootingSystem = shooting;
        powerUps = powerUpSystem;
    }

    public void Revive()
    {
        StopDeathSequence();
        StopReviveRecovery();
        RestoreStandingCollider();
        ResetActionState();

        desiredLane = lastSafeLane;
        currentHealth = maxHealth;
        isAlive = true;
        reviveRecoveryPending = true;
        damageImmunityTimeLeft = reviveImmunityDuration;

        Vector3 revivePosition = BuildRevivePosition();
        characterController.enabled = false;
        transform.SetPositionAndRotation(revivePosition, Quaternion.identity);
        characterController.enabled = true;
        characterController.Move(Vector3.zero);

        verticalVelocity = -2f;
        groundedGraceTimeLeft = groundedGraceWindow;
        autoShootTimer = autoShootCooldown * 0.5f;
        CacheSafeGroundedState();

        powerUps?.ApplyPowerUp(PowerUpType.Shield, 2f, false);
        EnemyDrone.DisableThreatsNear(transform.position, 8f);
        RunnerObstacle.DisableThreatsNear(transform.position, 8f);
        FindCameraController()?.OnPlayerRevive();
        vfxController?.OnRevive();
        ScreenFlash.Instance?.FlashRevive();

        reviveRecoveryRoutine = StartCoroutine(FinishReviveRecovery());
    }

    private void Update()
    {
        UpdateTransientTimers();
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || !isAlive)
        {
            return;
        }

        UpdateGroundingState();
        if (reviveRecoveryPending)
        {
            UpdateMovement();
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
        HandleAutoShoot();
        UpdateSlideTimer();
        UpdateHackMode();
        UpdateMovement();
    }

    public void SetHackInput(bool shouldHack)
    {
        hackHeld = shouldHack && CanAcceptPlayerActions();
        if (!hackHeld)
        {
            GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
        }
    }

    public void TakeHit(int damage)
    {
        if (!CanReceiveDamage())
        {
            return;
        }

        if (FeverMode.Instance != null && FeverMode.Instance.PreventsDamage)
        {
            ScreenShake.Instance?.ShakeHit();
            HapticFeedback.Instance?.VibrateOnHit();
            ScreenFlash.Instance?.FlashPowerUp();
            return;
        }

        if (powerUps != null && powerUps.ConsumeShieldIfActive())
        {
            ScreenShake.Instance?.ShakeHit();
            HapticFeedback.Instance?.VibrateOnHit();
            ScreenFlash.Instance?.FlashPowerUp();
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        ScreenShake.Instance?.ShakeHit();
        HapticFeedback.Instance?.VibrateOnHit();
        ScreenFlash.Instance?.FlashHit();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateTransientTimers()
    {
        damageImmunityTimeLeft = Mathf.Max(0f, damageImmunityTimeLeft - Time.unscaledDeltaTime);
        jumpBufferTimeLeft = Mathf.Max(0f, jumpBufferTimeLeft - Time.deltaTime);
    }

    private void UpdateGroundingState()
    {
        if (characterController.isGrounded)
        {
            groundedGraceTimeLeft = groundedGraceWindow;
            CacheSafeGroundedState();
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            groundedGraceTimeLeft = Mathf.Max(0f, groundedGraceTimeLeft - Time.deltaTime);
        }
    }

    private void CacheSafeGroundedState()
    {
        float snappedLane = Mathf.Round(transform.position.x / Mathf.Max(0.01f, laneOffset));
        int lane = Mathf.Clamp(Mathf.RoundToInt(snappedLane), -1, 1);
        float laneX = lane * laneOffset;
        if (Mathf.Abs(transform.position.x - laneX) > safeLaneSnapThreshold)
        {
            return;
        }

        lastSafeLane = lane;
        lastSafeGroundedPosition = new Vector3(laneX, transform.position.y, transform.position.z);
    }

    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && CanTrackTouch(touch))
            {
                BeginGesture(touch.fingerId, touch.position);
            }

            if (touch.fingerId == trackedTouchId && IsTouchComplete(touch.phase))
            {
                EndGesture(touch.position);
            }
        }
    }

    private void HandleMouseInput()
    {
        if (Input.touchCount > 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !PointerOverUi())
        {
            mouseTracking = true;
            BeginGesture(-999, Input.mousePosition);
        }

        if (mouseTracking && Input.GetMouseButtonUp(0))
        {
            mouseTracking = false;
            EndGesture(Input.mousePosition);
        }
    }

    private void BeginGesture(int touchId, Vector2 position)
    {
        trackedTouchId = touchId;
        touchStartPosition = position;
        touchStartTime = Time.unscaledTime;
    }

    private void EndGesture(Vector2 endPosition)
    {
        if (!CanAcceptPlayerActions())
        {
            trackedTouchId = -1;
            return;
        }

        Vector2 delta = endPosition - touchStartPosition;
        float duration = Time.unscaledTime - touchStartTime;
        trackedTouchId = -1;

        float sensitivity = SettingsManager.Instance != null ? SettingsManager.Instance.SwipeSensitivity : 1f;
        float adjustedThreshold = swipeThresholdPixels / Mathf.Max(0.5f, sensitivity);

        if (delta.magnitude < adjustedThreshold && duration <= tapMaxDuration)
        {
            FireShot();
            return;
        }

        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);
        if (absX > adjustedThreshold && absX > absY * swipeDominanceRatio)
        {
            QueueLaneShift(delta.x > 0f ? 1 : -1);
            return;
        }

        if (absY > adjustedThreshold && absY > absX * swipeDominanceRatio)
        {
            if (delta.y > 0f)
            {
                QueueJump();
            }
            else
            {
                TrySlide();
            }
        }
    }

    private void QueueLaneShift(int laneDelta)
    {
        if (!CanAcceptPlayerActions())
        {
            return;
        }

        desiredLane = Mathf.Clamp(desiredLane + laneDelta, -1, 1);
    }

    private void QueueJump()
    {
        if (!CanAcceptPlayerActions())
        {
            return;
        }

        jumpBufferTimeLeft = jumpBufferWindow;
        TryConsumeJump();
    }

    private void TryConsumeJump()
    {
        if (jumpBufferTimeLeft <= 0f || groundedGraceTimeLeft <= 0f || isSliding || !CanAcceptPlayerActions())
        {
            return;
        }

        float actualJumpForce = jumpForce;
        if (UpgradeSystem.Instance != null)
        {
            actualJumpForce *= UpgradeSystem.Instance.GetMultiplier(UpgradeSystem.UpgradeType.JumpHeight);
        }

        verticalVelocity = actualJumpForce;
        jumpBufferTimeLeft = 0f;
        groundedGraceTimeLeft = 0f;
        AudioManager.Instance?.PlayJump();
        vfxController?.OnJump();
    }

    private void TrySlide()
    {
        if (groundedGraceTimeLeft <= 0f || isSliding || !CanAcceptPlayerActions())
        {
            return;
        }

        isSliding = true;
        slideTimeLeft = slideDuration;
        characterController.height = slideHeight;
        characterController.center = new Vector3(originalCenter.x, slideHeight * 0.5f, originalCenter.z);
        AudioManager.Instance?.PlaySlide();
        vfxController?.OnSlide();
    }

    private void UpdateSlideTimer()
    {
        if (!isSliding)
        {
            return;
        }

        slideTimeLeft -= Time.deltaTime;
        if (slideTimeLeft > 0f)
        {
            return;
        }

        if (!CanStandUp())
        {
            slideTimeLeft = 0.08f;
            return;
        }

        RestoreStandingCollider();
    }

    private void RestoreStandingCollider()
    {
        isSliding = false;
        slideTimeLeft = 0f;
        characterController.height = originalHeight;
        characterController.center = originalCenter;
    }

    private bool CanStandUp()
    {
        float radius = Mathf.Max(0.05f, characterController.radius - 0.02f);
        Vector3 worldCenter = transform.position + originalCenter;
        float halfHeight = Mathf.Max(radius, (originalHeight * 0.5f) - radius);
        Vector3 bottom = worldCenter + Vector3.down * halfHeight;
        Vector3 top = worldCenter + Vector3.up * halfHeight;
        return !Physics.CheckCapsule(bottom, top, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
    }

    private void UpdateHackMode()
    {
        if (!CanAcceptPlayerActions())
        {
            GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
            vfxController?.SetHackState(false);
            return;
        }

        GameManager.Instance?.SetHackTimeScale(hackHeld, hackSlowScale);
        vfxController?.SetHackState(hackHeld);
        if (!hackHeld || Time.unscaledTime < nextHackPulseTime)
        {
            return;
        }

        nextHackPulseTime = Time.unscaledTime + hackPulseInterval;
        if (TryHackNearestThreat())
        {
            vfxController?.OnPowerUp(PowerUpType.EmpBlast);
        }

        AudioManager.Instance?.PlayHackPulse();
    }

    private void UpdateMovement()
    {
        TryConsumeJump();

        float targetX = desiredLane * laneOffset;
        float nextX = Mathf.SmoothDamp(transform.position.x, targetX, ref lateralVelocity, laneSmoothTime);
        float moveX = nextX - transform.position.x;
        if (!characterController.isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 move = new Vector3(moveX, verticalVelocity * Time.deltaTime, GameManager.Instance.CurrentForwardSpeed * Time.deltaTime);
        characterController.Move(move);
    }

    private void HandleAutoShoot()
    {
        if (!CanAcceptPlayerActions() || SettingsManager.Instance == null || !SettingsManager.Instance.AutoShootEnabled)
        {
            return;
        }

        autoShootTimer -= Time.deltaTime;
        if (autoShootTimer > 0f)
        {
            return;
        }

        float cadence = FeverMode.Instance != null && FeverMode.Instance.IsFeverActive ? 0.5f : 1f;
        autoShootTimer = autoShootCooldown * cadence;
        FireShot();
    }

    private void FireShot()
    {
        if (!CanAcceptPlayerActions())
        {
            return;
        }

        if (shootingSystem != null && shootingSystem.TryShootNearestTarget())
        {
            AudioManager.Instance?.PlayShoot();
            vfxController?.OnShoot(shootingSystem.CurrentWeapon);
        }
    }

    private bool TryHackNearestThreat()
    {
        float actualHackRange = hackRange;
        if (UpgradeSystem.Instance != null)
        {
            actualHackRange *= UpgradeSystem.Instance.GetMultiplier(UpgradeSystem.UpgradeType.HackRange);
        }

        float bestDistance = actualHackRange * actualHackRange;
        Vector3 origin = transform.position;
        IHackable bestTarget = GetClosestHackable(EnemyDrone.ActiveDrones, origin, ref bestDistance, null);
        bestTarget = GetClosestHackable(RunnerObstacle.ActiveObstacles, origin, ref bestDistance, bestTarget);
        BossController boss = BossController.ActiveBoss;
        if (boss != null && boss.IsHackable)
        {
            MonoBehaviour bossBehaviour = boss as MonoBehaviour;
            Vector3 offset = bossBehaviour.transform.position - origin;
            if (offset.z >= -0.5f)
            {
                float distance = offset.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = boss;
                }
            }
        }

        return bestTarget != null && bestTarget.TryHack();
    }

    private static IHackable GetClosestHackable<T>(IEnumerable<T> candidates, Vector3 origin, ref float bestDistance, IHackable fallback) where T : MonoBehaviour, IHackable
    {
        IHackable bestTarget = fallback;
        foreach (T candidate in candidates)
        {
            if (candidate == null || !candidate.IsHackable)
            {
                continue;
            }

            Vector3 offset = candidate.transform.position - origin;
            if (offset.z < -0.5f)
            {
                continue;
            }

            float distance = offset.sqrMagnitude;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private bool CanTrackTouch(Touch touch)
    {
        return trackedTouchId == -1 && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId));
    }

    private static bool IsTouchComplete(TouchPhase phase)
    {
        return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
    }

    private static bool PointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool CanAcceptPlayerActions()
    {
        return isAlive && !reviveRecoveryPending && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
    }

    private bool CanReceiveDamage()
    {
        return isAlive && !reviveRecoveryPending && damageImmunityTimeLeft <= 0f && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
    }

    private void ResetActionState()
    {
        hackHeld = false;
        trackedTouchId = -1;
        mouseTracking = false;
        nextHackPulseTime = 0f;
        jumpBufferTimeLeft = 0f;
        groundedGraceTimeLeft = 0f;
        verticalVelocity = 0f;
        lateralVelocity = 0f;
        GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
    }

    private Vector3 BuildRevivePosition()
    {
        Vector3 basePosition = lastSafeGroundedPosition == Vector3.zero ? transform.position : lastSafeGroundedPosition;
        float laneX = lastSafeLane * laneOffset;
        float safeZ = Mathf.Max(basePosition.z + reviveForwardOffset, transform.position.z + 1.5f);
        float safeY = Mathf.Max(basePosition.y, originalHeight * 0.5f) + reviveHeightOffset;
        return new Vector3(laneX, safeY, safeZ);
    }

    private void Die()
    {
        GameManager.Instance?.RegisterDeathReason("damage");
        if (!GameManager.Instance.BeginDeathSequence(this, deathSequenceTimeScale))
        {
            return;
        }

        isAlive = false;
        StopReviveRecovery();
        ResetActionState();
        AudioManager.Instance?.PlayHit();
        vfxController?.OnHit();
        FindCameraController()?.OnPlayerDeath();
        ScreenShake.Instance?.ShakeDeath();
        HapticFeedback.Instance?.VibrateOnDeath();
        ScreenFlash.Instance?.FlashDeath();
        ComboSystem.Instance?.ResetAll();
        deathSequenceRoutine = StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        float tumbleTimer = 0f;
        Vector3 tumbleAxis = new Vector3(0.3f, 0.1f, 1f).normalized;
        while (tumbleTimer < 0.8f)
        {
            tumbleTimer += Time.unscaledDeltaTime;
            transform.Rotate(tumbleAxis * (720f * Time.unscaledDeltaTime), Space.Self);
            transform.position += Vector3.down * (3f * Time.unscaledDeltaTime);
            yield return null;
        }

        deathSequenceRoutine = null;
        GameManager.Instance?.CompleteDeathSequence(this);
    }

    private IEnumerator FinishReviveRecovery()
    {
        yield return null;
        reviveRecoveryPending = false;
        reviveRecoveryRoutine = null;
    }

    private void StopDeathSequence()
    {
        if (deathSequenceRoutine == null)
        {
            return;
        }

        StopCoroutine(deathSequenceRoutine);
        deathSequenceRoutine = null;
    }

    private void StopReviveRecovery()
    {
        if (reviveRecoveryRoutine == null)
        {
            return;
        }

        StopCoroutine(reviveRecoveryRoutine);
        reviveRecoveryRoutine = null;
        reviveRecoveryPending = false;
    }

    private DynamicCameraController FindCameraController()
    {
        Camera camera = Camera.main;
        return camera != null ? camera.GetComponent<DynamicCameraController>() : null;
    }
}
