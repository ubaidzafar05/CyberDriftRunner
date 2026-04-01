using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    Dying,
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
    private const float DefaultFixedDeltaTime = 0.02f;

    public static GameManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private RunnerBalanceConfig balanceConfig;

    [Header("Speed")]
    [SerializeField] private float baseForwardSpeed = 9f;
    [SerializeField] private float speedGainPerSecond = 0.12f;
    [SerializeField] private float maxForwardSpeed = 22f;

    [Header("Scoring")]
    [SerializeField] private float scoreRatePerSecond = 12f;
    [SerializeField] private float distanceScale = 1f;

    private float hackTimeScale = 1f;
    private float powerUpTimeScale = 1f;
    private float cinematicTimeScale = 1f;
    private int scoreMultiplier = 1;
    private float scoreRemainder;
    private bool hasUsedRevive;
    private bool runRewardsCommitted;
    private PlayerController pendingRevivePlayer;

    public GameState State { get; private set; } = GameState.Menu;
    public RunSummary LastRunSummary { get; private set; }
    public PlayerController Player { get; private set; }
    public BossController ActiveBoss { get; private set; }
    public int Score { get; private set; }
    public int Credits { get; private set; }
    public float Distance { get; private set; }
    public float SurvivalTime { get; private set; }
    public int DronesDestroyedThisRun { get; private set; }
    public int HacksPerformedThisRun { get; private set; }
    public int PowerUpsUsedThisRun { get; private set; }
    public int BossesDefeatedThisRun { get; private set; }
    public bool IsBossEncounterActive { get; private set; }
    public bool IsRunInteractive => State == GameState.Playing;
    public bool IsRunPaused => State == GameState.Paused || State == GameState.RevivePrompt;
    public float CurrentForwardSpeed
    {
        get
        {
            float speed = Mathf.Min(GetBaseForwardSpeed() + (SurvivalTime * GetSpeedGainPerSecond()), GetMaxForwardSpeed());
            if (UpgradeSystem.Instance != null)
            {
                speed *= UpgradeSystem.Instance.GetMultiplier(UpgradeSystem.UpgradeType.BaseSpeed);
            }

            if (Player != null && Player.PowerUps != null && Player.PowerUps.HasSpeedBoost)
            {
                speed *= Player.PowerUps.SpeedBoostMultiplier;
            }

            if (FeverMode.Instance != null && FeverMode.Instance.IsFeverActive)
            {
                speed *= FeverMode.Instance.SpeedBoostMultiplier;
            }

            return speed;
        }
    }

    public int ScoreMultiplier => scoreMultiplier;
    public string ActivePowerUpLabel { get; private set; } = "Ready";
    public float ActivePowerUpTimeLeft { get; private set; }

    public event System.Action<bool, BossController> OnBossEncounterChanged;

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
        RefreshTimeScale();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
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
        Distance += CurrentForwardSpeed * deltaTime * GetDistanceScale();
        scoreRemainder += GetScoreRatePerSecond() * scoreMultiplier * deltaTime;

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
        AnalyticsManager.Instance?.TrackRunStart();
        MonetizationV2.Instance?.OnRunStarted();
        RefreshTimeScale();
        LoadSceneSafe(SceneNames.GameScene);
    }

    public void RestartRun()
    {
        StartRun();
    }

    public void ReturnToMenu()
    {
        ResetRunState();
        State = GameState.Menu;
        RefreshTimeScale();
        LoadSceneSafe(SceneNames.MainMenu);
    }

    public bool TryPauseRun()
    {
        if (State != GameState.Playing)
        {
            return false;
        }

        State = GameState.Paused;
        RefreshTimeScale();
        return true;
    }

    public bool ResumeRun()
    {
        if (State != GameState.Paused)
        {
            return false;
        }

        State = GameState.Playing;
        RefreshTimeScale();
        return true;
    }

    public bool BeginDeathSequence(PlayerController player, float sequenceTimeScale)
    {
        if (player == null || State != GameState.Playing)
        {
            return false;
        }

        pendingRevivePlayer = player;
        State = GameState.Dying;
        cinematicTimeScale = Mathf.Clamp(sequenceTimeScale, 0.05f, 1f);
        RefreshTimeScale();
        return true;
    }

    public void CompleteDeathSequence(PlayerController player)
    {
        if (player == null)
        {
            FinalizeGameOver();
            return;
        }

        pendingRevivePlayer = player;
        HandlePlayerDeath(player);
    }

    public void HandlePlayerDeath(PlayerController player)
    {
        if (player == null)
        {
            FinalizeGameOver();
            return;
        }

        pendingRevivePlayer = player;
        if (CanOfferRevive())
        {
            State = GameState.RevivePrompt;
            cinematicTimeScale = 1f;
            RefreshTimeScale();
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
        RefreshTimeScale();
    }

    public void SetPowerUpSlowMotion(bool enabled, float slowedScale)
    {
        powerUpTimeScale = enabled ? Mathf.Clamp(slowedScale, 0.1f, 1f) : 1f;
        RefreshTimeScale();
    }

    public void SetActivePowerUp(string label, float duration)
    {
        ActivePowerUpLabel = string.IsNullOrWhiteSpace(label) ? "Ready" : label;
        ActivePowerUpTimeLeft = Mathf.Max(0f, duration);
    }

    public void RegisterDroneDestroyed(int count)
    {
        int applied = Mathf.Max(0, count);
        DronesDestroyedThisRun += applied;
        MissionSystem.Instance?.RecordProgress(MissionType.DronesDestroyed, applied);
    }

    public void RegisterHackPerformed(int count)
    {
        int applied = Mathf.Max(0, count);
        HacksPerformedThisRun += applied;
        MissionSystem.Instance?.RecordProgress(MissionType.HacksPerformed, applied);
    }

    public void RegisterPowerUpUsed(int count)
    {
        int applied = Mathf.Max(0, count);
        PowerUpsUsedThisRun += applied;
        MissionSystem.Instance?.RecordProgress(MissionType.PowerUpsUsed, applied);
    }

    public void RegisterBossDefeated(int count)
    {
        int applied = Mathf.Max(0, count);
        BossesDefeatedThisRun += applied;
        MissionSystem.Instance?.RecordProgress(MissionType.BossesDefeated, applied);
    }

    public void SetBossEncounterState(bool active, BossController boss)
    {
        IsBossEncounterActive = active && boss != null;
        ActiveBoss = IsBossEncounterActive ? boss : null;
        OnBossEncounterChanged?.Invoke(IsBossEncounterActive, ActiveBoss);
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

        SetBossEncounterState(false, null);
        CommitRunRewards();
        CommitEndOfRunSystems();
        pendingRevivePlayer = null;
        State = GameState.GameOver;
        RefreshTimeScale();
        RateAppPrompt.Instance?.RecordSession();
        StarterPackOffer.Instance?.TryShowAfterRun();
        LoadSceneSafe(SceneNames.GameOver);
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
        cinematicTimeScale = 1f;
        SetActivePowerUp("Revive Boost", 2f);
        RefreshTimeScale();
        AudioManager.Instance?.PlayRevive();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == SceneNames.MainMenu)
        {
            State = GameState.Menu;
            SetBossEncounterState(false, null);
        }
        else if (scene.name == SceneNames.GameOver)
        {
            State = GameState.GameOver;
            MonetizationManager.Instance?.ShowInterstitialGameOver();
        }
        else if (scene.name == SceneNames.GameScene && State == GameState.Menu)
        {
            State = GameState.Playing;
        }

        RefreshTimeScale();
    }

    private void ResetRunState()
    {
        Score = 0;
        Credits = 0;
        Distance = 0f;
        SurvivalTime = 0f;
        DronesDestroyedThisRun = 0;
        HacksPerformedThisRun = 0;
        PowerUpsUsedThisRun = 0;
        BossesDefeatedThisRun = 0;
        scoreRemainder = 0f;
        Player = null;
        pendingRevivePlayer = null;
        hasUsedRevive = false;
        runRewardsCommitted = false;
        SetBossEncounterState(false, null);
        SetScoreMultiplier(1);
        SetActivePowerUp("Ready", 0f);
        ResetTimeControls();
        MilestoneSystem.Instance?.ResetForRun();
        ComboSystem.Instance?.ResetAll();
        FeverMode.Instance?.ResetForRun();
        NearMissDetector.Instance?.ResetForRun();
    }

    private void ResetTimeControls()
    {
        hackTimeScale = 1f;
        powerUpTimeScale = 1f;
        cinematicTimeScale = 1f;
        RefreshTimeScale();
    }

    private void RefreshTimeScale()
    {
        float nextTimeScale = 1f;
        if (State == GameState.Playing)
        {
            nextTimeScale = Mathf.Min(hackTimeScale, powerUpTimeScale);
        }
        else if (State == GameState.Dying)
        {
            nextTimeScale = cinematicTimeScale;
        }
        else if (State == GameState.Paused || State == GameState.RevivePrompt)
        {
            nextTimeScale = 0f;
        }

        ApplyTimeScale(nextTimeScale);
    }

    private static void ApplyTimeScale(float nextTimeScale)
    {
        Time.timeScale = Mathf.Clamp(nextTimeScale, 0f, 1f);
        Time.fixedDeltaTime = Mathf.Max(0.0001f, DefaultFixedDeltaTime * Mathf.Max(Time.timeScale, 0.1f));
    }

    private bool CanOfferRevive()
    {
        return !hasUsedRevive && MonetizationManager.Instance != null && MonetizationManager.Instance.CanShowRewardedAd;
    }

    private void CommitRunRewards()
    {
        if (runRewardsCommitted)
        {
            return;
        }

        runRewardsCommitted = true;
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.AddSoftCurrency(Credits);
            ProgressionManager.Instance.CommitRunStats(LastRunSummary);
        }

        XpLevelSystem.Instance?.AwardRunXp(LastRunSummary);
    }

    private void CommitEndOfRunSystems()
    {
        AchievementSystem.Instance?.CheckAfterRun(LastRunSummary);
        LeaderboardSystem.Instance?.SubmitRun(LastRunSummary);
        AnalyticsManager.Instance?.TrackRunEnd(LastRunSummary);
        MonetizationV2.Instance?.AddToPiggyBank(LastRunSummary.Credits);
        SeasonPassSystem.Instance?.AddSeasonXp(Mathf.FloorToInt(LastRunSummary.Distance * 0.3f));
        GooglePlayManager.Instance?.SubmitScore(LastRunSummary.Score);
        NotificationScheduler.Instance?.RecordPlaySession();

        int nearMisses = NearMissDetector.Instance != null ? NearMissDetector.Instance.NearMissCount : 0;
        int maxCombo = ComboSystem.Instance != null ? ComboSystem.Instance.MaxCombo : 0;
        DailyChallengeSystem.Instance?.UpdateProgressAfterRun(LastRunSummary, DronesDestroyedThisRun, HacksPerformedThisRun, nearMisses, maxCombo);
        MissionSystem.Instance?.RecordProgress(MissionType.Distance, Mathf.FloorToInt(LastRunSummary.Distance));
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private float GetBaseForwardSpeed()
    {
        return balanceConfig != null ? balanceConfig.BaseForwardSpeed : baseForwardSpeed;
    }

    private float GetSpeedGainPerSecond()
    {
        return balanceConfig != null ? balanceConfig.SpeedGainPerSecond : speedGainPerSecond;
    }

    private float GetMaxForwardSpeed()
    {
        return balanceConfig != null ? balanceConfig.MaxForwardSpeed : maxForwardSpeed;
    }

    private float GetScoreRatePerSecond()
    {
        return balanceConfig != null ? balanceConfig.ScoreRatePerSecond : scoreRatePerSecond;
    }

    private float GetDistanceScale()
    {
        return balanceConfig != null ? balanceConfig.DistanceScale : distanceScale;
    }
}
