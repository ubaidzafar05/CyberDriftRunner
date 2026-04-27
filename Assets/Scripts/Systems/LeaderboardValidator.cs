using System.Security.Cryptography;
using System.Text;

public static class LeaderboardValidator
{
    private const string HashPepper = "CyberDriftRunner.Leaderboard.v1";
    private const float MaxAverageRunSpeed = 35f;

    public static bool TryValidateRun(RunSummary summary, out string reason)
    {
        if (summary.Score < 0 || summary.Credits < 0 || summary.Distance < 0f || summary.SurvivalTime < 0f)
        {
            reason = "Run contains negative values.";
            return false;
        }

        if (summary.Score == 0 && summary.Distance == 0f && summary.SurvivalTime == 0f)
        {
            reason = "Empty run.";
            return false;
        }

        float duration = summary.SurvivalTime <= 0.01f ? 0.01f : summary.SurvivalTime;
        float averageSpeed = summary.Distance / duration;
        if (averageSpeed > MaxAverageRunSpeed)
        {
            reason = "Average speed exceeded validation threshold.";
            return false;
        }

        float scoreCeiling = (summary.Distance * 65f) + (summary.SurvivalTime * 220f) + 2500f;
        if (summary.Score > scoreCeiling)
        {
            reason = "Score exceeded validation threshold.";
            return false;
        }

        float creditCeiling = (summary.Distance * 1.2f) + 1500f;
        if (summary.Credits > creditCeiling)
        {
            reason = "Credits exceeded validation threshold.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static string BuildSignature(LeaderboardSubmissionPayload payload)
    {
        using SHA256 sha = SHA256.Create();
        string source = $"{payload.PlayerId}|{payload.PlayerName}|{payload.Score}|{payload.Distance}|{payload.SurvivalTime:0.000}|{payload.DateUtc}|{HashPepper}";
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        StringBuilder builder = new StringBuilder(hash.Length * 2);
        for (int i = 0; i < hash.Length; i++)
        {
            builder.Append(hash[i].ToString("x2"));
        }

        return builder.ToString();
    }
}
