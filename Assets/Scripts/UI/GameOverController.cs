using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text creditsText;
    [SerializeField] private Text survivalText;

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
    }

    public void Retry()
    {
        GameManager.Instance.RestartRun();
    }

    public void BackToMenu()
    {
        GameManager.Instance.ReturnToMenu();
    }
}
