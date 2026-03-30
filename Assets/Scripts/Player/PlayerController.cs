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
    [SerializeField] private float laneOffset = 3f;
    [SerializeField] private float laneSmoothTime = 0.08f;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideHeight = 1f;

    [Header("Input")]
    [SerializeField] private float swipeThresholdPixels = 70f;
    [SerializeField] private float tapMaxDuration = 0.2f;

    [Header("Hacking")]
    [SerializeField] private float hackSlowScale = 0.45f;
    [SerializeField] private float hackPulseInterval = 0.55f;
    [SerializeField] private float hackRange = 18f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 1;

    private int desiredLane;
    private int currentHealth;
    private int trackedTouchId = -1;
    private bool isAlive = true;
    private bool isSliding;
    private bool hackHeld;
    private bool mouseTracking;
    private float lateralVelocity;
    private float verticalVelocity;
    private float slideTimeLeft;
    private float nextHackPulseTime;
    private float originalHeight;
    private Vector3 originalCenter;
    private Vector2 touchStartPosition;
    private float touchStartTime;

    public PowerUpSystem PowerUps => powerUps;
    public bool IsAlive => isAlive;

    private void Awake()
    {
        characterController = characterController == null ? GetComponent<CharacterController>() : characterController;
        shootingSystem = shootingSystem == null ? GetComponent<ShootingSystem>() : shootingSystem;
        powerUps = powerUps == null ? GetComponent<PowerUpSystem>() : powerUps;
        vfxController = vfxController == null ? GetComponent<PlayerVfxController>() : vfxController;
        skinApplier = skinApplier == null ? GetComponent<PlayerSkinApplier>() : skinApplier;
        originalHeight = characterController.height;
        originalCenter = characterController.center;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameManager.Instance?.RegisterPlayer(this);
        skinApplier?.ApplySelectedSkin();
    }

    private void OnDisable()
    {
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
        currentHealth = maxHealth;
        isAlive = true;
        hackHeld = false;
        verticalVelocity = 0f;
        characterController.enabled = false;
        transform.position += Vector3.up * 0.2f;
        characterController.enabled = true;
        powerUps?.ApplyPowerUp(PowerUpType.Shield, 2f);
        EnemyDrone.DisableThreatsNear(transform.position, 8f);
        RunnerObstacle.DisableThreatsNear(transform.position, 8f);
        vfxController?.OnRevive();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing || !isAlive)
        {
            return;
        }

        HandleTouchInput();
        HandleMouseInput();
        UpdateSlideTimer();
        UpdateHackMode();
        UpdateMovement();
    }

    public void SetHackInput(bool shouldHack)
    {
        hackHeld = shouldHack;
        if (!hackHeld)
        {
            GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
        }
    }

    public void TakeHit(int damage)
    {
        if (!isAlive)
        {
            return;
        }

        if (powerUps != null && powerUps.ConsumeShieldIfActive())
        {
            ScreenShake.Instance?.ShakeHit();
            HapticFeedback.Instance?.VibrateOnHit();
            return;
        }

        currentHealth -= Mathf.Max(1, damage);
        ScreenShake.Instance?.ShakeHit();
        HapticFeedback.Instance?.VibrateOnHit();
        if (currentHealth <= 0)
        {
            Die();
        }
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
        Vector2 delta = endPosition - touchStartPosition;
        float duration = Time.unscaledTime - touchStartTime;
        trackedTouchId = -1;

        if (delta.magnitude < swipeThresholdPixels && duration <= tapMaxDuration)
        {
            if (shootingSystem != null && shootingSystem.TryShootNearestTarget())
            {
                AudioManager.Instance?.PlayShoot();
                vfxController?.OnShoot();
            }

            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            desiredLane = Mathf.Clamp(desiredLane + (delta.x > 0f ? 1 : -1), -1, 1);
            return;
        }

        if (delta.y > 0f)
        {
            TryJump();
        }
        else
        {
            TrySlide();
        }
    }

    private void TryJump()
    {
        if (!characterController.isGrounded || isSliding)
        {
            return;
        }

        verticalVelocity = jumpForce;
        AudioManager.Instance?.PlayJump();
        vfxController?.OnJump();
    }

    private void TrySlide()
    {
        if (!characterController.isGrounded || isSliding)
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

        isSliding = false;
        characterController.height = originalHeight;
        characterController.center = originalCenter;
    }

    private void UpdateHackMode()
    {
        GameManager.Instance?.SetHackTimeScale(hackHeld, hackSlowScale);
        vfxController?.SetHackState(hackHeld);
        if (!hackHeld || Time.unscaledTime < nextHackPulseTime)
        {
            return;
        }

        nextHackPulseTime = Time.unscaledTime + hackPulseInterval;
        TryHackNearestThreat();
        AudioManager.Instance?.PlayHackPulse();
    }

    private void UpdateMovement()
    {
        float targetX = desiredLane * laneOffset;
        float nextX = Mathf.SmoothDamp(transform.position.x, targetX, ref lateralVelocity, laneSmoothTime);
        float moveX = nextX - transform.position.x;
        verticalVelocity = characterController.isGrounded && verticalVelocity < 0f ? -2f : verticalVelocity - (gravity * Time.deltaTime);

        Vector3 move = new Vector3(moveX, verticalVelocity * Time.deltaTime, GameManager.Instance.CurrentForwardSpeed * Time.deltaTime);
        characterController.Move(move);
    }

    private void TryHackNearestThreat()
    {
        IHackable bestTarget = null;
        float bestDistance = hackRange * hackRange;
        Vector3 origin = transform.position;

        bestTarget = GetClosestHackable(EnemyDrone.ActiveDrones, origin, bestDistance, ref bestDistance);
        bestTarget = GetClosestHackable(RunnerObstacle.ActiveObstacles, origin, bestDistance, ref bestDistance, bestTarget);
        bestTarget?.TryHack();
    }

    private static IHackable GetClosestHackable<T>(System.Collections.Generic.IEnumerable<T> candidates, Vector3 origin, float initialBest, ref float bestDistance, IHackable fallback = null) where T : MonoBehaviour, IHackable
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
            if (distance > initialBest || distance >= bestDistance)
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

    private void Die()
    {
        isAlive = false;
        hackHeld = false;
        GameManager.Instance?.SetHackTimeScale(false, hackSlowScale);
        AudioManager.Instance?.PlayHit();
        vfxController?.OnHit();
        ScreenShake.Instance?.ShakeDeath();
        HapticFeedback.Instance?.VibrateOnDeath();
        ComboSystem.Instance?.ResetAll();
        GameManager.Instance?.HandlePlayerDeath(this);
    }
}
