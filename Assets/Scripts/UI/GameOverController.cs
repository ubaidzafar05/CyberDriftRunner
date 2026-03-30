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
    [SerializeField] private Text feverCountText;

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

        // Show rate prompt after good runs
        if (summary.Score > 500)
        {
            RateAppPrompt.Instance?.TryShowPrompt();
        }
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

    private void ShowTip()
    {
        if (tipText == null || TipSystem.Instance == null) return;
        tipText.text = TipSystem.Instance.GetTip();
    }
}
