using System;
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
    [SerializeField] private float openingScoreBoostDuration = 12f;
    [SerializeField] private float openingScoreBoostMultiplier = 1.25f;

    [Header("Death Flow")]
    [SerializeField] private float revivePromptTimeout = 8f;
    [SerializeField] private float deathResolutionTimeout = 1.15f;

    private float hackTimeScale = 1f;
    private float powerUpTimeScale = 1f;
    private int powerUpScoreMultiplier = 1;
    private int feverScoreMultiplier = 1;
    private float scoreRemainder;
    private bool doubleRewardsGranted;
    private bool runRewardsCommitted;
    private string pendingDeathReason = "unknown";
    private int nextDistanceAnalyticsMilestone = 500;
    private RunFlowController _runFlow;

    public GameState State => _runFlow != null ? _runFlow.State : GameState.Menu;
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
    public bool IsRunInteractive => _runFlow != null && _runFlow.IsRunInteractive;
    public bool IsRunPaused => _runFlow != null && _runFlow.IsRunPaused;
    public bool HasUsedRevive => _runFlow != null && _runFlow.HasUsedRevive;
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

    public int ScoreMultiplier => GetResolvedScoreMultiplier();
    public bool IsOpeningMoments => SurvivalTime < openingScoreBoostDuration;
    public string ActivePowerUpLabel { get; private set; } = "Ready";
    public float ActivePowerUpTimeLeft { get; private set; }
    public string LastRunRewardTitle { get; private set; } = "Run Cache";
    public string LastRunRewardDetail { get; private set; } = "Push deeper to unlock district rewards.";
    public string LastRunDistrictName { get; private set; } = "Neon Gateway";
    public string LastRunGrade { get; private set; } = "C";
    public int LastRunBossCredits { get; private set; }
    public string CurrentDistrictName => RunDistrictCatalog.ResolveName(Distance);

    public event System.Action<bool, BossController> OnBossEncounterChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _runFlow = new RunFlowController(revivePromptTimeout, deathResolutionTimeout);
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
        HandleRunFlowOutcome(_runFlow.Update(Time.unscaledTime, CanOfferRevive()));
        if (State != GameState.Playing)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        SurvivalTime += deltaTime;
        Distance += CurrentForwardSpeed * deltaTime * GetDistanceScale();
        scoreRemainder += GetScoreRatePerSecond() * ScoreMultiplier * deltaTime;

        int scoreGain = Mathf.FloorToInt(scoreRemainder);
        if (scoreGain > 0)
        {
            Score += scoreGain;
            scoreRemainder -= scoreGain;
        }

        int distanceInt = Mathf.FloorToInt(Distance);
        if (distanceInt >= nextDistanceAnalyticsMilestone)
        {
            AnalyticsManager.Instance?.TrackDistanceReached(nextDistanceAnalyticsMilestone);
            nextDistanceAnalyticsMilestone += 500;
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        Player = player;
        GhostRunManager.Instance?.BindPlayer(player);
    }

    public void StartRun()
    {
        ResetRunState();
        LimitedTimeEventSystem.Instance?.RefreshNow();
        LiveOpsSystem.Instance?.Refresh();
        GhostRunManager.Instance?.PrepareForNewRun();
        _runFlow.StartRun();
        MonetizationV2.Instance?.OnRunStarted();
        RefreshTimeScale();
        EventBus.Publish(new RunStartedEvent((ProgressionManager.Instance?.TotalRuns ?? 0) + 1));
        LoadSceneSafe(SceneNames.GameScene);
    }

    public void RestartRun()
    {
        StartRun();
    }

    public void ReturnToMenu()
    {
        ResetRunState();
        _runFlow.ResetToMenu();
        RefreshTimeScale();
        LoadSceneSafe(SceneNames.MainMenu);
    }

    public bool TryPauseRun()
    {
        if (!_runFlow.TryPause())
        {
            return false;
        }

        RefreshTimeScale();
        return true;
    }

    public bool ResumeRun()
    {
        if (!_runFlow.Resume())
        {
            return false;
        }

        RefreshTimeScale();
        return true;
    }

    public bool BeginDeathSequence(PlayerController player, float sequenceTimeScale)
    {
        if (!_runFlow.BeginDeathSequence(player, sequenceTimeScale, Time.unscaledTime))
        {
            return false;
        }

        RefreshTimeScale();
        return true;
    }

    public void CompleteDeathSequence(PlayerController player)
    {
        HandleRunFlowOutcome(_runFlow.CompleteDeathSequence(player, CanOfferRevive(), Time.unscaledTime));
    }

    public void AcceptReviveOffer()
    {
        if (State != GameState.RevivePrompt || _runFlow.PendingRevivePlayer == null)
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
        HandleRunFlowOutcome(_runFlow.DeclineRevive());
    }

    public bool CanClaimDoubleRewards => !doubleRewardsGranted && LastRunSummary.Credits > 0 && MonetizationManager.Instance != null && MonetizationManager.Instance.CanShowRewardedAd;

    public void ClaimDoubleRewards(Action<bool> onCompleted)
    {
        if (!CanClaimDoubleRewards)
        {
            onCompleted?.Invoke(false);
            return;
        }

        MonetizationManager.Instance.ShowRewardedDoubleRewards(succeeded =>
        {
            if (!succeeded)
            {
                onCompleted?.Invoke(false);
                return;
            }

            doubleRewardsGranted = true;
            if (EconomySystem.Instance != null)
            {
                EconomySystem.Instance.AddCredits(LastRunSummary.Credits, "double_rewards");
            }
            else
            {
                ProgressionManager.Instance?.AddSoftCurrency(LastRunSummary.Credits);
            }
            onCompleted?.Invoke(true);
        });
    }

    public void AddCredits(int amount)
    {
        if (amount > 0)
        {
            float multiplier = LimitedTimeEventSystem.Instance != null ? LimitedTimeEventSystem.Instance.CreditMultiplier : 1f;
            float liveOpsMultiplier = LiveOpsSystem.Instance != null ? LiveOpsSystem.Instance.GetRewardMultiplier() : 1f;
            Credits += Mathf.Max(1, Mathf.RoundToInt(amount * multiplier * liveOpsMultiplier));
        }
    }

    public void AddScore(int amount)
    {
        if (amount > 0)
        {
            float multiplier = LimitedTimeEventSystem.Instance != null ? LimitedTimeEventSystem.Instance.ScoreMultiplier : 1f;
            float liveOpsMultiplier = LiveOpsSystem.Instance != null ? LiveOpsSystem.Instance.GetRewardMultiplier() : 1f;
            float openingBoost = IsOpeningMoments ? openingScoreBoostMultiplier : 1f;
            Score += Mathf.RoundToInt(amount * multiplier * liveOpsMultiplier * openingBoost) * ScoreMultiplier;
        }
    }

    public void RegisterDeathReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        pendingDeathReason = reason;
    }

    public void SetPowerUpScoreMultiplier(int multiplier)
    {
        powerUpScoreMultiplier = Mathf.Max(1, multiplier);
    }

    public void SetFeverScoreMultiplier(int multiplier)
    {
        feverScoreMultiplier = Mathf.Max(1, multiplier);
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

    public void SetBossRewardPresentation(string bossName, string rewardTitle, int bossCredits, int tier)
    {
        if (!string.IsNullOrWhiteSpace(rewardTitle))
        {
            LastRunRewardTitle = rewardTitle;
        }
        else
        {
            LastRunRewardTitle = "Boss Cache";
        }

        LastRunBossCredits = Mathf.Max(0, bossCredits);
        int resolvedTier = Mathf.Max(1, tier);
        string clearanceLabel = resolvedTier switch
        {
            1 => "Gateway clearance secured.",
            2 => "Market route unlocked.",
            3 => "Security grid broken open.",
            _ => "Citadel access forced wide open."
        };
        LastRunRewardDetail = $"{bossName} neutralized // +{LastRunBossCredits} cache credits // Tier {resolvedTier} clearance earned. {clearanceLabel}";
    }

    private void FinalizeGameOver()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        try
        {
            LastRunSummary = new RunSummary
            {
                Score = Score,
                Credits = Credits,
                Distance = Distance,
                SurvivalTime = SurvivalTime
            };
            BuildRunPresentationSummary();
            SetBossEncounterState(false, null);
            SafeRunEndStep("ghost_complete", () => GhostRunManager.Instance?.CompleteRun(LastRunSummary));
            SafeRunEndStep("run_analytics", () => RunAnalyticsStore.Instance?.RecordRun(LastRunSummary, pendingDeathReason));
            CommitRunRewards();
            CommitEndOfRunSystems();
            SafeRunEndStep("run_ended_event", () => EventBus.Publish(new RunEndedEvent(LastRunSummary, pendingDeathReason)));
        }
        finally
        {
            _runFlow.MarkGameOver();
            RefreshTimeScale();
            LoadSceneSafe(SceneNames.GameOver);
        }
    }

    private void HandleReviveResult(bool succeeded)
    {
        PlayerController revivePlayer = _runFlow.PendingRevivePlayer;
        RunFlowOutcome outcome = _runFlow.AcceptReviveResult(succeeded);
        if (outcome != RunFlowOutcome.ResumeRun || revivePlayer == null)
        {
            FinalizeGameOver();
            return;
        }

        revivePlayer.Revive();
        _runFlow.ClearPendingRevivePlayer();
        RefreshTimeScale();
        AudioManager.Instance?.PlayRevive();
        AnalyticsManager.Instance?.TrackReviveUsed(Distance);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        _runFlow.NotifySceneLoaded(scene.name);
        if (scene.name == SceneNames.MainMenu)
        {
            SetBossEncounterState(false, null);
        }
        else if (scene.name == SceneNames.GameOver)
        {
            MonetizationManager.Instance?.ShowInterstitialGameOver();
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
        doubleRewardsGranted = false;
        runRewardsCommitted = false;
        pendingDeathReason = "unknown";
        nextDistanceAnalyticsMilestone = 500;
        SetBossEncounterState(false, null);
        SetPowerUpScoreMultiplier(1);
        SetFeverScoreMultiplier(1);
        SetActivePowerUp("Ready", 0f);
        LastRunRewardTitle = "Run Cache";
        LastRunRewardDetail = "Push deeper to unlock district rewards.";
        LastRunDistrictName = RunDistrictCatalog.ResolveName(0f);
        LastRunGrade = "C";
        LastRunBossCredits = 0;
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
            nextTimeScale = _runFlow.CinematicTimeScale;
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
        return !HasUsedRevive &&
               MonetizationManager.Instance != null &&
               MonetizationManager.Instance.CanOfferRevive(Distance) &&
               ReviveOverlayController.Instance != null &&
               ReviveOverlayController.Instance.IsReady;
    }

    private void CommitRunRewards()
    {
        if (runRewardsCommitted)
        {
            return;
        }

        runRewardsCommitted = true;
        SafeRunEndStep("progression_rewards", () =>
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.AddSoftCurrency(Credits);
                ProgressionManager.Instance.CommitRunStats(LastRunSummary);
            }
        });
        SafeRunEndStep("xp_rewards", () => XpLevelSystem.Instance?.AwardRunXp(LastRunSummary));
    }

    private void CommitEndOfRunSystems()
    {
        int nearMisses = NearMissDetector.Instance != null ? NearMissDetector.Instance.NearMissCount : 0;
        int maxCombo = ComboSystem.Instance != null ? ComboSystem.Instance.MaxCombo : 0;
        SafeRunEndStep("achievements", () => AchievementSystem.Instance?.CheckAfterRun(LastRunSummary));
        SafeRunEndStep("leaderboard", () => LeaderboardSystem.Instance?.SubmitRun(LastRunSummary));
        SafeRunEndStep("piggy_bank", () => MonetizationV2.Instance?.AddToPiggyBank(LastRunSummary.Credits));
        SafeRunEndStep("season_pass", () => SeasonPassSystem.Instance?.AddSeasonXp(Mathf.FloorToInt(LastRunSummary.Distance * 0.3f)));
        SafeRunEndStep("platform_score", () => GooglePlayManager.Instance?.SubmitScore(LastRunSummary.Score));
        SafeRunEndStep("notification_session", () => NotificationScheduler.Instance?.RecordPlaySession());
        SafeRunEndStep("cloud_save", () => CloudSaveManager.Instance?.SaveProgress());
        SafeRunEndStep("daily_challenges", () => DailyChallengeSystem.Instance?.UpdateProgressAfterRun(LastRunSummary, DronesDestroyedThisRun, HacksPerformedThisRun, nearMisses, maxCombo));
        SafeRunEndStep("mission_distance", () => MissionSystem.Instance?.RecordProgress(MissionType.Distance, Mathf.FloorToInt(LastRunSummary.Distance)));
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[GameManager] Scene '{sceneName}' is not in build settings or cannot be loaded.");
            return;
        }

        try
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(sceneName);
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[GameManager] SceneLoader failed for '{sceneName}': {exception}");
        }

        SceneManager.LoadScene(sceneName);
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
        float rate = balanceConfig != null ? balanceConfig.ScoreRatePerSecond : scoreRatePerSecond;
        if (IsOpeningMoments)
        {
            rate *= openingScoreBoostMultiplier;
        }

        return rate;
    }

    private float GetDistanceScale()
    {
        return balanceConfig != null ? balanceConfig.DistanceScale : distanceScale;
    }

    private void BuildRunPresentationSummary()
    {
        LastRunDistrictName = RunDistrictCatalog.ResolveName(Distance);
        LastRunGrade = ResolveRunGrade(Score, Distance, BossesDefeatedThisRun, HasUsedRevive);

        if (LastRunBossCredits <= 0)
        {
            LastRunRewardTitle = BossesDefeatedThisRun > 0 ? "District Cache" : "Courier Payout";
            LastRunRewardDetail = BossesDefeatedThisRun > 0
                ? $"Cleared {BossesDefeatedThisRun} boss encounter{(BossesDefeatedThisRun == 1 ? string.Empty : "s")} in {LastRunDistrictName}."
                : $"Reached {LastRunDistrictName}. Travel farther for boss chests and premium drops.";
        }
        else
        {
            LastRunRewardDetail = $"{LastRunRewardDetail} Final district: {LastRunDistrictName}.";
        }
    }

    private int GetResolvedScoreMultiplier()
    {
        return Mathf.Max(1, powerUpScoreMultiplier) * Mathf.Max(1, feverScoreMultiplier);
    }

    private void HandleRunFlowOutcome(RunFlowOutcome outcome)
    {
        switch (outcome)
        {
            case RunFlowOutcome.ShowRevivePrompt:
                RefreshTimeScale();
                ReviveOverlayController.Instance?.FocusPrompt();
                break;
            case RunFlowOutcome.FinalizeGameOver:
                FinalizeGameOver();
                break;
            case RunFlowOutcome.ResumeRun:
            case RunFlowOutcome.None:
            default:
                break;
        }
    }

    private static void SafeRunEndStep(string stepName, Action action)
    {
        if (action == null)
        {
            return;
        }

        try
        {
            action();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[GameManager] Run end step failed: {stepName}\n{exception}");
        }
    }

    private static string ResolveRunGrade(int score, float distance, int bossesDefeated, bool revived)
    {
        int gradeValue = 0;
        if (distance >= 3000f) gradeValue += 3;
        else if (distance >= 1800f) gradeValue += 2;
        else if (distance >= 900f) gradeValue += 1;

        if (score >= 15000) gradeValue += 2;
        else if (score >= 7000) gradeValue += 1;

        gradeValue += Mathf.Clamp(bossesDefeated, 0, 2);
        if (revived) gradeValue -= 1;

        if (gradeValue >= 6) return "S";
        if (gradeValue >= 5) return "A";
        if (gradeValue >= 3) return "B";
        if (gradeValue >= 1) return "C";
        return "D";
    }
}
