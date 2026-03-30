using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    RevivePrompt,
    GameOver
}

public struct RunSummary
{
    public int Score;
    public int Credits;
    public float Distance;
    public float SurvivalTime;
}

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Speed")]
    [SerializeField] private float baseForwardSpeed = 9f;
    [SerializeField] private float speedGainPerSecond = 0.12f;
    [SerializeField] private float maxForwardSpeed = 22f;

    [Header("Scoring")]
    [SerializeField] private float scoreRatePerSecond = 12f;
    [SerializeField] private float distanceScale = 1f;

    private float hackTimeScale = 1f;
    private float powerUpTimeScale = 1f;
    private int scoreMultiplier = 1;
    private float scoreRemainder;
    private bool hasUsedRevive;
    private bool runRewardsCommitted;
    private PlayerController pendingRevivePlayer;

    public GameState State { get; private set; } = GameState.Menu;
    public RunSummary LastRunSummary { get; private set; }
    public PlayerController Player { get; private set; }
    public int Score { get; private set; }
    public int Credits { get; private set; }
    public float Distance { get; private set; }
    public float SurvivalTime { get; private set; }
    public float CurrentForwardSpeed => Mathf.Min(baseForwardSpeed + (SurvivalTime * speedGainPerSecond), maxForwardSpeed);
    public int ScoreMultiplier => scoreMultiplier;
    public string ActivePowerUpLabel { get; private set; } = "Ready";
    public float ActivePowerUpTimeLeft { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyTimeScale(1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        SurvivalTime += deltaTime;
        Distance += CurrentForwardSpeed * deltaTime * distanceScale;
        scoreRemainder += scoreRatePerSecond * scoreMultiplier * deltaTime;
        int scoreGain = Mathf.FloorToInt(scoreRemainder);

        if (scoreGain > 0)
        {
            Score += scoreGain;
            scoreRemainder -= scoreGain;
        }

        if (ActivePowerUpTimeLeft > 0f)
        {
            ActivePowerUpTimeLeft = Mathf.Max(0f, ActivePowerUpTimeLeft - Time.unscaledDeltaTime);
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        Player = player;
    }

    public void StartRun()
    {
        ResetRunState();
        State = GameState.Playing;
        SceneManager.LoadScene(SceneNames.GameScene);
    }

    public void RestartRun()
    {
        StartRun();
    }

    public void ReturnToMenu()
    {
        ResetTimeControls();
        State = GameState.Menu;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void HandlePlayerDeath(PlayerController player)
    {
        if (player == null)
        {
            FinalizeGameOver();
            return;
        }

        if (CanOfferRevive())
        {
            pendingRevivePlayer = player;
            State = GameState.RevivePrompt;
            ApplyTimeScale(0f);
            return;
        }

        FinalizeGameOver();
    }

    public void AcceptReviveOffer()
    {
        if (State != GameState.RevivePrompt || pendingRevivePlayer == null)
        {
            return;
        }

        if (MonetizationManager.Instance == null || !MonetizationManager.Instance.CanShowRewardedAd)
        {
            FinalizeGameOver();
            return;
        }

        MonetizationManager.Instance.ShowRewardedRevive(HandleReviveResult);
    }

    public void DeclineReviveOffer()
    {
        FinalizeGameOver();
    }

    private void FinalizeGameOver()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        LastRunSummary = new RunSummary
        {
            Score = Score,
            Credits = Credits,
            Distance = Distance,
            SurvivalTime = SurvivalTime
        };

        CommitRunRewards();
        pendingRevivePlayer = null;
        ResetTimeControls();
        State = GameState.GameOver;
        SceneManager.LoadScene(SceneNames.GameOver);
    }

    public void AddCredits(int amount)
    {
        if (amount > 0)
        {
            Credits += amount;
        }
    }

    public void AddScore(int amount)
    {
        if (amount > 0)
        {
            Score += amount * scoreMultiplier;
        }
    }

    public void SetScoreMultiplier(int multiplier)
    {
        scoreMultiplier = Mathf.Max(1, multiplier);
    }

    public void SetHackTimeScale(bool enabled, float slowedScale)
    {
        hackTimeScale = enabled ? Mathf.Clamp(slowedScale, 0.1f, 1f) : 1f;
        ApplyTimeScale(Mathf.Min(hackTimeScale, powerUpTimeScale));
    }

    public void SetPowerUpSlowMotion(bool enabled, float slowedScale)
    {
        powerUpTimeScale = enabled ? Mathf.Clamp(slowedScale, 0.1f, 1f) : 1f;
        ApplyTimeScale(Mathf.Min(hackTimeScale, powerUpTimeScale));
    }

    public void SetActivePowerUp(string label, float duration)
    {
        ActivePowerUpLabel = string.IsNullOrWhiteSpace(label) ? "Ready" : label;
        ActivePowerUpTimeLeft = Mathf.Max(0f, duration);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == SceneNames.MainMenu)
        {
            State = GameState.Menu;
        }
        else if (scene.name == SceneNames.GameOver)
        {
            State = GameState.GameOver;
            MonetizationManager.Instance?.ShowInterstitialGameOver();
        }
        else if (scene.name == SceneNames.GameScene && State != GameState.Playing)
        {
            State = GameState.Playing;
        }
    }

    private void ResetRunState()
    {
        Score = 0;
        Credits = 0;
        Distance = 0f;
        SurvivalTime = 0f;
        scoreRemainder = 0f;
        Player = null;
        pendingRevivePlayer = null;
        hasUsedRevive = false;
        runRewardsCommitted = false;
        SetScoreMultiplier(1);
        SetActivePowerUp("Ready", 0f);
        ResetTimeControls();
    }

    private void ResetTimeControls()
    {
        hackTimeScale = 1f;
        powerUpTimeScale = 1f;
        ApplyTimeScale(1f);
    }

    private void ApplyTimeScale(float nextTimeScale)
    {
        Time.timeScale = Mathf.Clamp(nextTimeScale, 0f, 1f);
        Time.fixedDeltaTime = Mathf.Max(0.0001f, 0.02f * Mathf.Max(Time.timeScale, 0.1f));
    }

    private bool CanOfferRevive()
    {
        return !hasUsedRevive && MonetizationManager.Instance != null && MonetizationManager.Instance.CanShowRewardedAd;
    }

    private void HandleReviveResult(bool succeeded)
    {
        if (!succeeded || pendingRevivePlayer == null)
        {
            FinalizeGameOver();
            return;
        }

        hasUsedRevive = true;
        State = GameState.Playing;
        pendingRevivePlayer.Revive();
        pendingRevivePlayer = null;
        SetActivePowerUp("Revive Boost", 2f);
        ResetTimeControls();
        AudioManager.Instance?.PlayRevive();
    }

    private void CommitRunRewards()
    {
        if (runRewardsCommitted)
        {
            return;
        }

        runRewardsCommitted = true;
        ProgressionManager.Instance?.AddSoftCurrency(Credits);
    }
}
