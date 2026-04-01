using System;
using UnityEngine;

[Serializable]
public struct LeaderboardSubmissionPayload
{
    public string PlayerId;
    public string PlayerName;
    public int Score;
    public int Distance;
    public string DateUtc;
}

[Serializable]
public struct LeaderboardSubmissionReceipt
{
    public bool Accepted;
    public string Message;
}

public interface ILeaderboardTransport
{
    LeaderboardSubmissionReceipt SubmitScore(LeaderboardSubmissionPayload payload);
}

public sealed class MockLeaderboardTransport : MonoBehaviour, ILeaderboardTransport
{
    public LeaderboardSubmissionReceipt SubmitScore(LeaderboardSubmissionPayload payload)
    {
        Debug.Log($"Leaderboard mock submit: {payload.PlayerName} score={payload.Score} distance={payload.Distance}");
        return new LeaderboardSubmissionReceipt
        {
            Accepted = true,
            Message = "Mock transport accepted payload."
        };
    }
}
