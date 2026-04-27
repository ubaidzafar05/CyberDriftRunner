using UnityEngine;

/// <summary>
/// Fever/Rush mode that triggers at high combos.
/// During fever: double credits, auto-shoot, invincibility, visual frenzy.
/// </summary>
public sealed class FeverMode : MonoBehaviour
{
    public static FeverMode Instance { get; private set; }

    [Header("Activation")]
    [SerializeField] private int comboThreshold = 6;
    [SerializeField] private float feverDuration = 8f;

    [Header("Bonuses")]
    [SerializeField] private int scoreMultiplier = 3;
    [SerializeField] private float speedBoostPercent = 0.15f;

    private float _feverTimer;
    private bool _feverActive;

    public bool IsFeverActive => _feverActive;
    public bool PreventsDamage => _feverActive;
    public float FeverTimeLeft => _feverTimer;
    public float FeverProgress => feverDuration > 0 ? _feverTimer / feverDuration : 0f;
    public float SpeedBoostMultiplier => _feverActive ? 1f + Mathf.Max(0f, speedBoostPercent) : 1f;

    public event System.Action OnFeverStart;
    public event System.Action OnFeverEnd;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (ComboSystem.Instance != null)
            ComboSystem.Instance.OnComboChanged += HandleComboChanged;
    }

    private void OnDisable()
    {
        if (ComboSystem.Instance != null)
            ComboSystem.Instance.OnComboChanged -= HandleComboChanged;
    }

    private void Update()
    {
        if (!_feverActive) return;

        _feverTimer -= Time.deltaTime;
        if (_feverTimer <= 0f)
        {
            EndFever();
        }
    }

    private void HandleComboChanged(int multiplier)
    {
        if (!_feverActive && multiplier >= comboThreshold)
        {
            StartFever();
        }
    }

    private void StartFever()
    {
        _feverActive = true;
        _feverTimer = feverDuration;

        GameManager.Instance?.SetFeverScoreMultiplier(scoreMultiplier);

        AudioManager.Instance?.PlayPowerUp();
        ScreenShake.Instance?.AddTrauma(0.3f);
        HapticFeedback.Instance?.VibrateHeavy();

        OnFeverStart?.Invoke();
        Debug.Log("[FeverMode] FEVER ACTIVATED!");
    }

    private void EndFever()
    {
        _feverActive = false;
        _feverTimer = 0f;

        GameManager.Instance?.SetFeverScoreMultiplier(1);
        ComboSystem.Instance?.ResetCombo();

        OnFeverEnd?.Invoke();
        Debug.Log("[FeverMode] Fever ended");
    }

    public void ResetForRun()
    {
        if (_feverActive)
        {
            EndFever();
            return;
        }

        _feverTimer = 0f;
        GameManager.Instance?.SetFeverScoreMultiplier(1);
    }
}
