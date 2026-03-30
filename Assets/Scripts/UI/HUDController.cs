using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text creditsText;
    [SerializeField] private Text powerUpText;

    public void Configure(Text score, Text distance, Text credits, Text powerUp)
    {
        scoreText = score;
        distanceText = distance;
        creditsText = credits;
        powerUpText = powerUp;
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        scoreText.text = $"Score {GameManager.Instance.Score:000000}";
        distanceText.text = $"Distance {GameManager.Instance.Distance:0}m";
        creditsText.text = $"Credits {GameManager.Instance.Credits}";
        powerUpText.text = GameManager.Instance.ActivePowerUpTimeLeft > 0f
            ? $"{GameManager.Instance.ActivePowerUpLabel} {GameManager.Instance.ActivePowerUpTimeLeft:0.0}s"
            : GameManager.Instance.ActivePowerUpLabel;
    }
}
