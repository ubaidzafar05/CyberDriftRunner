using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player stats and profile screen accessible from main menu.
/// Shows lifetime stats, achievements progress, and upgrade levels.
/// </summary>
public sealed class StatsProfileController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private Text playerNameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text xpProgressText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text bestDistanceText;
    [SerializeField] private Text totalRunsText;
    [SerializeField] private Text totalDistanceText;
    [SerializeField] private Text totalDronesText;
    [SerializeField] private Text totalCreditsText;
    [SerializeField] private Text achievementCountText;
    [SerializeField] private Text dailyStreakText;
    [SerializeField] private Text tipText;

    public void Configure(
        Text playerName, Text level, Text xpProgress,
        Text highScore, Text bestDistance,
        Text totalRuns, Text totalDist, Text totalDrones, Text totalCredits,
        Text achievementCount, Text dailyStreak, Text tip)
    {
        playerNameText = playerName;
        levelText = level;
        xpProgressText = xpProgress;
        highScoreText = highScore;
        bestDistanceText = bestDistance;
        totalRunsText = totalRuns;
        totalDistanceText = totalDist;
        totalDronesText = totalDrones;
        totalCreditsText = totalCredits;
        achievementCountText = achievementCount;
        dailyStreakText = dailyStreak;
        tipText = tip;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (ProgressionManager.Instance != null)
        {
            var pm = ProgressionManager.Instance;
            SetText(highScoreText, $"High Score: {pm.HighScore:N0}");
            SetText(bestDistanceText, $"Best Distance: {pm.BestDistance:N0}m");
            SetText(totalRunsText, $"Total Runs: {pm.TotalRuns:N0}");
            SetText(totalDistanceText, $"Total Distance: {pm.TotalDistance:N0}m");
            SetText(totalDronesText, $"Drones Destroyed: {pm.TotalDronesDestroyed:N0}");
            SetText(totalCreditsText, $"Credits: {pm.SoftCurrency:N0}");
        }

        if (XpLevelSystem.Instance != null)
        {
            var xp = XpLevelSystem.Instance;
            SetText(levelText, $"Level {xp.CurrentLevel}");
            SetText(xpProgressText, $"XP: {xp.XpInCurrentLevel}/{xp.XpNeededForNext} ({xp.LevelProgress * 100:0}%)");
        }

        if (GooglePlayManager.Instance != null && GooglePlayManager.Instance.IsSignedIn)
        {
            SetText(playerNameText, GooglePlayManager.Instance.PlayerName);
        }
        else
        {
            SetText(playerNameText, "Runner");
        }

        if (AchievementSystem.Instance != null)
        {
            int total = AchievementSystem.Instance.Definitions.Count;
            int unlocked = 0;
            for (int i = 0; i < total; i++)
            {
                if (AchievementSystem.Instance.IsUnlocked(AchievementSystem.Instance.Definitions[i].Id))
                    unlocked++;
            }
            SetText(achievementCountText, $"Achievements: {unlocked}/{total}");
        }

        if (DailyRewardSystem.Instance != null)
        {
            SetText(dailyStreakText, $"Daily Streak: {DailyRewardSystem.Instance.CurrentStreak} day(s)");
        }

        if (tipText != null && TipSystem.Instance != null)
        {
            tipText.text = TipSystem.Instance.GetTip();
        }
    }

    private static void SetText(Text textComponent, string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
    }
}
