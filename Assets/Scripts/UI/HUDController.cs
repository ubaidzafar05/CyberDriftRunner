using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text creditsText;
    [SerializeField] private Text powerUpText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text zoneText;
    [SerializeField] private Text feverText;
    [SerializeField] private Text fpsText;
    [SerializeField] private Text missionText;
    [SerializeField] private Text bossText;

    private int _cachedScore = -1;
    private int _cachedDistance = -1;
    private int _cachedCredits = -1;
    private string _cachedPowerUpLabel;
    private int _cachedPowerUpTime = -1;
    private int _cachedCombo = -1;

    public void Configure(Text score, Text distance, Text credits, Text powerUp)
    {
        scoreText = score;
        distanceText = distance;
        creditsText = credits;
        powerUpText = powerUp;
    }

    private void OnEnable()
    {
        if (MilestoneSystem.Instance != null)
        {
            MilestoneSystem.Instance.OnZoneChanged += HandleZoneChanged;
        }
    }

    private void OnDisable()
    {
        if (MilestoneSystem.Instance != null)
        {
            MilestoneSystem.Instance.OnZoneChanged -= HandleZoneChanged;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        int score = GameManager.Instance.Score;
        if (score != _cachedScore)
        {
            bool bigScoreChange = score - _cachedScore >= 40;
            _cachedScore = score;
            scoreText.text = $"Score {score:000000}";
            if (bigScoreChange && UIAnimator.Instance != null)
            {
                UIAnimator.Instance.PunchScale(scoreText.transform, 1.2f, 0.15f);
            }
        }

        int distance = Mathf.FloorToInt(GameManager.Instance.Distance);
        if (distance != _cachedDistance)
        {
            _cachedDistance = distance;
            distanceText.text = $"Distance {distance}m";
        }

        int credits = GameManager.Instance.Credits;
        if (credits != _cachedCredits)
        {
            _cachedCredits = credits;
            creditsText.text = $"Credits {credits}";
        }

        string label = GameManager.Instance.ActivePowerUpLabel;
        int timeLeft = Mathf.CeilToInt(GameManager.Instance.ActivePowerUpTimeLeft * 10f);
        if (label != _cachedPowerUpLabel || timeLeft != _cachedPowerUpTime)
        {
            _cachedPowerUpLabel = label;
            _cachedPowerUpTime = timeLeft;
            powerUpText.text = GameManager.Instance.ActivePowerUpTimeLeft > 0f
                ? $"{label} {GameManager.Instance.ActivePowerUpTimeLeft:0.0}s"
                : label;
        }

        UpdateComboDisplay();
        UpdateFeverDisplay();
        UpdateMissionDisplay();
        UpdateBossDisplay();
    }

    private void UpdateComboDisplay()
    {
        if (comboText == null || ComboSystem.Instance == null)
        {
            return;
        }

        int multiplier = ComboSystem.Instance.CurrentMultiplier;
        if (multiplier != _cachedCombo)
        {
            _cachedCombo = multiplier;
            if (multiplier > 1)
            {
                comboText.text = $"x{multiplier} COMBO";
                comboText.gameObject.SetActive(true);
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateFeverDisplay()
    {
        if (feverText == null || FeverMode.Instance == null)
        {
            return;
        }

        if (FeverMode.Instance.IsFeverActive)
        {
            float timeLeft = FeverMode.Instance.FeverTimeLeft;
            feverText.text = $"🔥 FEVER {timeLeft:0.0}s 🔥";
            feverText.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 3f, 1f));
            feverText.gameObject.SetActive(true);
        }
        else
        {
            feverText.gameObject.SetActive(false);
        }
    }

    private void HandleZoneChanged(int zone, string zoneName, Color zoneColor)
    {
        if (zoneText == null)
        {
            return;
        }

        zoneText.text = zoneName;
        zoneText.color = zoneColor;
        zoneText.gameObject.SetActive(true);
    }

    private void UpdateMissionDisplay()
    {
        if (missionText == null || MissionSystem.Instance == null)
        {
            return;
        }

        missionText.text = MissionSystem.Instance.GetPrimaryMissionLabel();
        missionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(missionText.text));
    }

    private void UpdateBossDisplay()
    {
        if (bossText == null)
        {
            return;
        }

        BossController boss = GameManager.Instance != null ? GameManager.Instance.ActiveBoss : null;
        if (boss == null || !GameManager.Instance.IsBossEncounterActive)
        {
            bossText.gameObject.SetActive(false);
            return;
        }

        bossText.text = $"BOSS {boss.CurrentHealth}/{boss.MaxHealth}";
        bossText.gameObject.SetActive(true);
    }
}
