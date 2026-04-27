using UnityEngine;

public enum RunFlowOutcome
{
    None,
    ShowRevivePrompt,
    FinalizeGameOver,
    ResumeRun
}

public sealed class RunFlowController
{
    private readonly float _revivePromptTimeout;
    private readonly float _deathTimeout;

    private float _cinematicTimeScale = 1f;
    private float _revivePromptDeadline = -1f;
    private float _deathDeadline = -1f;
    private bool _hasUsedRevive;
    private PlayerController _pendingRevivePlayer;

    public RunFlowController(float revivePromptTimeout, float deathTimeout)
    {
        _revivePromptTimeout = Mathf.Max(1f, revivePromptTimeout);
        _deathTimeout = Mathf.Max(0.35f, deathTimeout);
    }

    public GameState State { get; private set; } = GameState.Menu;
    public PlayerController PendingRevivePlayer => _pendingRevivePlayer;
    public bool HasUsedRevive => _hasUsedRevive;
    public bool IsRunPaused => State == GameState.Paused || State == GameState.RevivePrompt;
    public bool IsRunInteractive => State == GameState.Playing;
    public float CinematicTimeScale => _cinematicTimeScale;

    public void ResetToMenu()
    {
        State = GameState.Menu;
        ClearRunState();
    }

    public void StartRun()
    {
        State = GameState.Playing;
        ClearRunState();
    }

    public bool TryPause()
    {
        if (State != GameState.Playing)
        {
            return false;
        }

        State = GameState.Paused;
        return true;
    }

    public bool Resume()
    {
        if (State != GameState.Paused)
        {
            return false;
        }

        State = GameState.Playing;
        return true;
    }

    public bool BeginDeathSequence(PlayerController player, float cinematicScale, float unscaledNow)
    {
        if (player == null || State != GameState.Playing)
        {
            return false;
        }

        _pendingRevivePlayer = player;
        _cinematicTimeScale = Mathf.Clamp(cinematicScale, 0.05f, 1f);
        _deathDeadline = unscaledNow + _deathTimeout;
        _revivePromptDeadline = -1f;
        State = GameState.Dying;
        return true;
    }

    public RunFlowOutcome Update(float unscaledNow, bool canOfferRevive)
    {
        if (State == GameState.Dying && _deathDeadline > 0f && unscaledNow >= _deathDeadline)
        {
            return CompleteDeathSequence(_pendingRevivePlayer, canOfferRevive, unscaledNow);
        }

        if (State == GameState.RevivePrompt &&
            _revivePromptDeadline > 0f &&
            unscaledNow >= _revivePromptDeadline)
        {
            return FinalizeGameOver();
        }

        return RunFlowOutcome.None;
    }

    public RunFlowOutcome CompleteDeathSequence(PlayerController player, bool canOfferRevive, float unscaledNow)
    {
        if (player == null)
        {
            return FinalizeGameOver();
        }

        _pendingRevivePlayer = player;
        _deathDeadline = -1f;
        _cinematicTimeScale = 1f;

        if (!canOfferRevive)
        {
            return FinalizeGameOver();
        }

        _revivePromptDeadline = unscaledNow + _revivePromptTimeout;
        State = GameState.RevivePrompt;
        return RunFlowOutcome.ShowRevivePrompt;
    }

    public RunFlowOutcome AcceptReviveResult(bool rewarded)
    {
        if (State != GameState.RevivePrompt || _pendingRevivePlayer == null)
        {
            return RunFlowOutcome.None;
        }

        if (!rewarded)
        {
            return FinalizeGameOver();
        }

        _hasUsedRevive = true;
        _revivePromptDeadline = -1f;
        _deathDeadline = -1f;
        _cinematicTimeScale = 1f;
        State = GameState.Playing;
        return RunFlowOutcome.ResumeRun;
    }

    public RunFlowOutcome DeclineRevive()
    {
        return FinalizeGameOver();
    }

    public void ClearPendingRevivePlayer()
    {
        _pendingRevivePlayer = null;
    }

    public void MarkGameOver()
    {
        _pendingRevivePlayer = null;
        _revivePromptDeadline = -1f;
        _deathDeadline = -1f;
        _cinematicTimeScale = 1f;
        State = GameState.GameOver;
    }

    public void NotifySceneLoaded(string sceneName)
    {
        if (sceneName == SceneNames.MainMenu)
        {
            State = GameState.Menu;
            return;
        }

        if (sceneName == SceneNames.GameOver)
        {
            State = GameState.GameOver;
            _pendingRevivePlayer = null;
            return;
        }

        if (sceneName == SceneNames.GameScene && State == GameState.Menu)
        {
            State = GameState.Playing;
        }
    }

    private RunFlowOutcome FinalizeGameOver()
    {
        _revivePromptDeadline = -1f;
        _deathDeadline = -1f;
        _cinematicTimeScale = 1f;
        return RunFlowOutcome.FinalizeGameOver;
    }

    private void ClearRunState()
    {
        _pendingRevivePlayer = null;
        _hasUsedRevive = false;
        _cinematicTimeScale = 1f;
        _revivePromptDeadline = -1f;
        _deathDeadline = -1f;
    }
}
