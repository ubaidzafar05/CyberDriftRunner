using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text creditsText;
    [SerializeField] private Text survivalText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text bestDistanceText;
    [SerializeField] private Text newHighScoreLabel;
    [SerializeField] private Text nearBestText;
    [SerializeField] private Text leaderboardRankText;
    [SerializeField] private Text xpGainText;
    [SerializeField] private Text dailyChallengeText;
    [SerializeField] private Text tipText;
    [SerializeField] private Text doubleRewardStatusText;
    [SerializeField] private Button doubleRewardButton;
    [SerializeField] private Text rewardTitleText;
    [SerializeField] private Text rewardDetailText;
    [SerializeField] private Text districtText;
    [SerializeField] private Text gradeText;

    public void Configure(Text score, Text distance, Text credits, Text survival)
    {
        scoreText = score;
        distanceText = distance;
        creditsText = credits;
        survivalText = survival;
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!ValidateBindings())
        {
            enabled = false;
            return;
        }

        RunSummary summary = GameManager.Instance.LastRunSummary;
        scoreText.text = $"Score {summary.Score:000000}";
        distanceText.text = $"Distance {summary.Distance:0}m";
        creditsText.text = $"Credits {summary.Credits}";
        survivalText.text = $"Survival {summary.SurvivalTime:0.0}s";

        ShowBestScores(summary);
        ShowNearBestEncouragement(summary);
        ShowLeaderboardRank(summary);
        ShowXpGain(summary);
        ShowDailyChallengeProgress();
        ShowTip();
        ShowRewardSummary();
        RefreshDoubleRewardState();
        ShowPostRunPrompts(summary);
    }

    private void ShowBestScores(RunSummary summary)
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"Best Score {ProgressionManager.Instance.HighScore:000000}";
        }

        if (bestDistanceText != null)
        {
            bestDistanceText.text = $"Best Distance {ProgressionManager.Instance.BestDistance:0}m";
        }

        bool isNewBest = summary.Score >= ProgressionManager.Instance.HighScore && summary.Score > 0;
        if (newHighScoreLabel != null)
        {
            newHighScoreLabel.gameObject.SetActive(isNewBest);
        }
    }

    private void ShowNearBestEncouragement(RunSummary summary)
    {
        if (nearBestText == null || ProgressionManager.Instance == null)
        {
            return;
        }

        int bestScore = ProgressionManager.Instance.HighScore;
        float bestDist = ProgressionManager.Instance.BestDistance;
        bool isNewBest = summary.Score >= bestScore;

        if (isNewBest)
        {
            nearBestText.text = "NEW RECORD!";
            nearBestText.gameObject.SetActive(true);
            return;
        }

        // "You were Xm from your best!" psychology
        float distDiff = bestDist - summary.Distance;
        int scoreDiff = bestScore - summary.Score;

        if (distDiff > 0 && distDiff < bestDist * 0.25f)
        {
            nearBestText.text = $"Only {distDiff:0}m from your best!";
            nearBestText.gameObject.SetActive(true);
        }
        else if (scoreDiff > 0 && scoreDiff < bestScore * 0.2f)
        {
            nearBestText.text = $"Only {scoreDiff} points from your best!";
            nearBestText.gameObject.SetActive(true);
        }
        else
        {
            nearBestText.gameObject.SetActive(false);
        }
    }

    private void ShowLeaderboardRank(RunSummary summary)
    {
        if (leaderboardRankText == null || LeaderboardSystem.Instance == null)
        {
            return;
        }

        int rank = LeaderboardSystem.Instance.GetRank(summary.Score);
        leaderboardRankText.text = $"Rank #{rank}";
    }

    private void ShowXpGain(RunSummary summary)
    {
        if (xpGainText == null || XpLevelSystem.Instance == null)
        {
            return;
        }

        int xpEarned = Mathf.FloorToInt(summary.Distance * 0.5f) + Mathf.FloorToInt(summary.Score * 0.1f);
        xpGainText.text = $"+{xpEarned} XP (Level {XpLevelSystem.Instance.CurrentLevel})";
    }

    private void ShowDailyChallengeProgress()
    {
        if (dailyChallengeText == null || DailyChallengeSystem.Instance == null)
        {
            return;
        }

        var challenge = DailyChallengeSystem.Instance.ActiveChallenge;
        if (challenge == null)
        {
            dailyChallengeText.gameObject.SetActive(false);
            return;
        }

        int progress = DailyChallengeSystem.Instance.Progress;
        if (DailyChallengeSystem.Instance.IsCompleted)
        {
            dailyChallengeText.text = $"CHALLENGE COMPLETE: {challenge.Description}";
        }
        else
        {
            dailyChallengeText.text = $"Challenge: {challenge.Description} ({progress}/{challenge.Target})";
        }

        dailyChallengeText.gameObject.SetActive(true);
    }

    public void Retry()
    {
        GameManager.Instance?.RestartRun();
    }

    public void BackToMenu()
    {
        GameManager.Instance?.ReturnToMenu();
    }

    public void ShareScore()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        ShareManager.Instance?.ShareScore(GameManager.Instance.LastRunSummary);
    }

    public void ClaimDoubleRewards()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.ClaimDoubleRewards(succeeded =>
        {
            if (!succeeded)
            {
                RefreshDoubleRewardState();
                return;
            }

            RunSummary summary = GameManager.Instance.LastRunSummary;
            creditsText.text = $"Credits {summary.Credits * 2}";
            if (doubleRewardStatusText != null)
            {
                doubleRewardStatusText.text = "Double rewards claimed!";
                doubleRewardStatusText.color = Color.green;
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.interactable = false;
            }
        });
    }

    private void RefreshDoubleRewardState()
    {
        bool canClaim = GameManager.Instance != null && GameManager.Instance.CanClaimDoubleRewards;
        if (doubleRewardButton != null)
        {
            doubleRewardButton.interactable = canClaim;
            doubleRewardButton.gameObject.SetActive(GameManager.Instance != null && GameManager.Instance.LastRunSummary.Credits > 0);
        }

        if (doubleRewardStatusText != null)
        {
            doubleRewardStatusText.text = canClaim ? "Watch an ad to double your credits." : "Double rewards unavailable.";
            doubleRewardStatusText.color = canClaim ? new Color(1f, 0.85f, 0.2f) : Color.gray;
        }
    }

    private void ShowTip()
    {
        if (tipText == null || TipSystem.Instance == null) return;
        tipText.text = TipSystem.Instance.GetTip();
    }

    private void ShowRewardSummary()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (rewardTitleText != null)
        {
            rewardTitleText.text = GameManager.Instance.LastRunRewardTitle;
            rewardTitleText.color = GameManager.Instance.LastRunBossCredits > 0
                ? new Color(1f, 0.84f, 0.24f)
                : new Color(0.72f, 0.86f, 1f);
        }

        if (rewardDetailText != null)
        {
            rewardDetailText.text = GameManager.Instance.LastRunRewardDetail;
        }

        if (districtText != null)
        {
            districtText.text = $"District  //  {GameManager.Instance.LastRunDistrictName}";
        }

        if (gradeText != null)
        {
            gradeText.text = $"Grade {GameManager.Instance.LastRunGrade}";
            gradeText.color = ResolveGradeColor(GameManager.Instance.LastRunGrade);
            if (GameManager.Instance.LastRunGrade == "S" || GameManager.Instance.LastRunGrade == "A")
            {
                UIAnimator.Instance?.PunchScale(gradeText.transform, 1.15f, 0.18f);
            }
        }

        UIAnimator.Instance?.PunchScale(rewardTitleText != null ? rewardTitleText.transform : null, 1.08f, 0.16f);
    }

    private bool ValidateBindings()
    {
        bool hasRequiredBindings = scoreText != null &&
                                   distanceText != null &&
                                   creditsText != null &&
                                   survivalText != null;
        if (hasRequiredBindings)
        {
            return true;
        }

        Debug.LogError("[GameOverController] Missing required core score bindings. GameOver scene must provide authored UI references.");
        return false;
    }

    private static void ShowPostRunPrompts(RunSummary summary)
    {
        // REASONING:
        // 1. Game-over is a critical authored screen and must not be visually hijacked by runtime-generated prompts.
        // 2. Session accounting can still happen here without surfacing extra UI.
        // 3. Rating and starter-pack prompts stay available for explicit future authored integrations.
        RateAppPrompt.Instance?.RecordSession();
    }

    public void BindDoubleRewards(Button button, Text status)
    {
        doubleRewardButton = button;
        doubleRewardStatusText = status;
    }

    private static Color ResolveGradeColor(string grade)
    {
        switch (grade)
        {
            case "S":
                return new Color(1f, 0.88f, 0.2f);
            case "A":
                return new Color(0.34f, 1f, 0.72f);
            case "B":
                return new Color(0.32f, 0.88f, 1f);
            case "C":
                return new Color(1f, 0.72f, 0.24f);
            default:
                return new Color(1f, 0.36f, 0.42f);
        }
    }
}
