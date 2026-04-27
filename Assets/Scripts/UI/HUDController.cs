using UnityEngine;
using UnityEngine.UI;

public sealed class HUDController : MonoBehaviour
{
    private const float MissionIntroDelaySeconds = 4.5f;
    private const float MissionHighlightDuration = 2.5f;
    private const float ComboIntroDelaySeconds = 3.5f;
    private const float ZoneMessageDuration = 3f;
    private const int DistanceDisplayStepMeters = 3;
    private static readonly Color InactiveKitColor = new Color(0.72f, 0.88f, 1f);
    private static readonly Color ActiveKitColor = new Color(0.22f, 0.95f, 1f);

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
    private int _cachedBossHealth = -1;
    private int _cachedDistrictIndex = -1;
    private string _cachedBossName;
    private string _cachedMissionLabel;
    private string _cachedBossStatus;
    private float _missionHighlightUntil;
    private float _missionCompletionUntil;
    private string _missionCompletionText;
    private float _zoneVisibleUntil;

    private void Start()
    {
        EnsureHudLayout();
        _missionHighlightUntil = Time.unscaledTime + MissionIntroDelaySeconds + 1.25f;
        RefreshDistrictBadge(false);
    }

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

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionCompleted += HandleMissionCompleted;
            MissionSystem.Instance.OnMissionsChanged += HandleMissionsChanged;
        }
    }

    private void OnDisable()
    {
        if (MilestoneSystem.Instance != null)
        {
            MilestoneSystem.Instance.OnZoneChanged -= HandleZoneChanged;
        }

        if (MissionSystem.Instance != null)
        {
            MissionSystem.Instance.OnMissionCompleted -= HandleMissionCompleted;
            MissionSystem.Instance.OnMissionsChanged -= HandleMissionsChanged;
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
                bool earlyRun = GameManager.Instance.SurvivalTime < 20f;
                UIAnimator.Instance.PunchScale(scoreText.transform, earlyRun ? 1.3f : 1.2f, earlyRun ? 0.2f : 0.15f);
            }
        }

        int distance = Mathf.FloorToInt(GameManager.Instance.Distance);
        if (_cachedDistance < 0 || Mathf.Abs(distance - _cachedDistance) >= DistanceDisplayStepMeters)
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
        if (GameManager.Instance.ActivePowerUpTimeLeft <= 0f && GameManager.Instance.Player != null)
        {
            ShootingSystem shooting = GameManager.Instance.Player.GetComponent<ShootingSystem>();
            if (shooting != null)
            {
                label = shooting.GetWeaponDisplayName();
            }
        }

        int timeLeft = Mathf.CeilToInt(GameManager.Instance.ActivePowerUpTimeLeft * 10f);
        if (label != _cachedPowerUpLabel || timeLeft != _cachedPowerUpTime)
        {
            _cachedPowerUpLabel = label;
            _cachedPowerUpTime = timeLeft;
            bool hasActivePowerUp = GameManager.Instance.ActivePowerUpTimeLeft > 0f;
            powerUpText.text = hasActivePowerUp
                ? $"KIT // {label.ToUpperInvariant()}  {GameManager.Instance.ActivePowerUpTimeLeft:0.0}s"
                : $"WEAPON // {label.ToUpperInvariant()}";
            powerUpText.color = hasActivePowerUp ? ActiveKitColor : InactiveKitColor;

            if (hasActivePowerUp && UIAnimator.Instance != null)
            {
                UIAnimator.Instance.PunchScale(powerUpText.transform, 1.04f, 0.12f);
            }
        }

        UpdateDistrictDisplay();
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

        if (GameManager.Instance.SurvivalTime < ComboIntroDelaySeconds)
        {
            comboText.gameObject.SetActive(false);
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

        RunDistrictCatalog.DistrictInfo district = RunDistrictCatalog.GetByIndex(zone);
        _cachedDistrictIndex = zone;
        zoneText.text = $"ENTERING {zoneName.ToUpperInvariant()}\n<size=16>{district.Subtitle.ToUpperInvariant()}</size>";
        zoneText.color = zoneColor;
        zoneText.gameObject.SetActive(true);
        _zoneVisibleUntil = Time.unscaledTime + ZoneMessageDuration;
        if (UIAnimator.Instance != null)
        {
            UIAnimator.Instance.PunchScale(zoneText.transform, 1.08f, 0.14f);
        }
    }

    private void UpdateMissionDisplay()
    {
        if (missionText == null || MissionSystem.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.SurvivalTime < MissionIntroDelaySeconds)
        {
            missionText.gameObject.SetActive(false);
            return;
        }

        if (Time.unscaledTime < _missionCompletionUntil && !string.IsNullOrWhiteSpace(_missionCompletionText))
        {
            missionText.text = _missionCompletionText;
            missionText.color = new Color(0.4f, 1f, 0.72f);
            missionText.gameObject.SetActive(true);
            return;
        }

        string label = MissionSystem.Instance.GetPrimaryMissionLabel();
        if (string.IsNullOrWhiteSpace(label))
        {
            missionText.gameObject.SetActive(false);
            return;
        }

        if (label != _cachedMissionLabel)
        {
            _cachedMissionLabel = label;
            _missionHighlightUntil = Time.unscaledTime + MissionHighlightDuration;
            if (UIAnimator.Instance != null)
            {
                UIAnimator.Instance.PunchScale(missionText.transform, 1.06f, 0.12f);
            }
        }

        bool isHighlighted = Time.unscaledTime < _missionHighlightUntil;
        missionText.text = isHighlighted ? $"MISSION // {label}" : label;
        missionText.color = isHighlighted ? new Color(1f, 0.9f, 0.32f) : new Color(0.68f, 0.84f, 0.98f);
        missionText.gameObject.SetActive(true);
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
            BossEncounterManager encounterManager = BossEncounterManager.Instance;
            if (encounterManager != null && encounterManager.IsEncounterImminent)
            {
                int distanceToBoss = Mathf.CeilToInt(encounterManager.DistanceToNextEncounter);
                string warningText = $"BOSS SIGNAL // {distanceToBoss}m";
                if (_cachedBossStatus != warningText)
                {
                    _cachedBossStatus = warningText;
                    bossText.text = warningText;
                    bossText.color = Color.Lerp(new Color(1f, 0.92f, 0.32f), new Color(1f, 0.4f, 0.32f), Mathf.PingPong(Time.unscaledTime * 1.6f, 1f));
                    if (UIAnimator.Instance != null)
                    {
                        UIAnimator.Instance.PunchScale(bossText.transform, 1.06f, 0.14f);
                    }
                }
                else
                {
                    bossText.color = Color.Lerp(new Color(1f, 0.92f, 0.32f), new Color(1f, 0.4f, 0.32f), Mathf.PingPong(Time.unscaledTime * 1.6f, 1f));
                }

                bossText.gameObject.SetActive(true);
                _cachedBossHealth = -1;
                _cachedBossName = string.Empty;
                return;
            }

            bossText.gameObject.SetActive(false);
            _cachedBossHealth = -1;
            _cachedBossName = string.Empty;
            _cachedBossStatus = string.Empty;
            return;
        }

        if (_cachedBossHealth != boss.CurrentHealth || _cachedBossName != boss.DisplayName)
        {
            bool tookDamage = _cachedBossHealth > boss.CurrentHealth && _cachedBossHealth >= 0;
            _cachedBossHealth = boss.CurrentHealth;
            _cachedBossName = boss.DisplayName;
            _cachedBossStatus = string.Empty;
            bossText.text = $"{boss.DisplayName}  {boss.CurrentHealth}/{boss.MaxHealth}";
            bossText.color = new Color(1f, 0.78f, 0.32f);
            if (tookDamage && UIAnimator.Instance != null)
            {
                UIAnimator.Instance.PunchScale(bossText.transform, 1.08f, 0.12f);
                UIAnimator.Instance.FlashColor(bossText, new Color(1f, 0.45f, 0.45f), 0.18f);
            }
        }

        bossText.gameObject.SetActive(true);
    }

    private void EnsureHudLayout()
    {
        AnchorTopLeft(scoreText, new Vector2(150f, -40f));
        AnchorTopLeft(distanceText, new Vector2(150f, -74f));
        AnchorTopLeft(creditsText, new Vector2(150f, -108f));
        AnchorTopLeft(powerUpText, new Vector2(146f, -150f));
        AnchorTopLeft(missionText, new Vector2(146f, -188f));
        AnchorTopRight(fpsText, new Vector2(-112f, -34f));
        AnchorTopCenter(zoneText, new Vector2(0f, -36f));
        AnchorTopCenter(bossText, new Vector2(0f, -108f));
        AnchorTopCenter(comboText, new Vector2(0f, -156f));
        AnchorTopCenter(feverText, new Vector2(0f, -202f));

        if (missionText != null)
        {
            missionText.gameObject.SetActive(false);
        }

        if (zoneText != null)
        {
            zoneText.gameObject.SetActive(true);
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }

        if (feverText != null)
        {
            feverText.gameObject.SetActive(false);
        }

        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(Application.isEditor || Debug.isDebugBuild);
        }
    }

    private void LateUpdate()
    {
        if (zoneText != null && Time.unscaledTime > _zoneVisibleUntil)
        {
            RefreshDistrictBadge(false);
        }
    }

    private void HandleMissionCompleted(MissionProgress mission)
    {
        if (mission == null || missionText == null)
        {
            return;
        }

        _cachedMissionLabel = mission.Description;
        _missionCompletionText = $"MISSION COMPLETE // {mission.Description}";
        _missionCompletionUntil = Time.unscaledTime + (MissionHighlightDuration + 1f);
        _missionHighlightUntil = _missionCompletionUntil;
        missionText.text = _missionCompletionText;
        missionText.color = new Color(0.4f, 1f, 0.72f);
        missionText.gameObject.SetActive(true);
        if (UIAnimator.Instance != null)
        {
            UIAnimator.Instance.PunchScale(missionText.transform, 1.12f, 0.16f);
        }
    }

    private void HandleMissionsChanged()
    {
        _cachedMissionLabel = null;
    }

    private void UpdateDistrictDisplay()
    {
        if (zoneText == null || GameManager.Instance == null || Time.unscaledTime <= _zoneVisibleUntil)
        {
            return;
        }

        RefreshDistrictBadge(false);
    }

    private void RefreshDistrictBadge(bool force)
    {
        if (zoneText == null || GameManager.Instance == null)
        {
            return;
        }

        RunDistrictCatalog.DistrictInfo district = RunDistrictCatalog.Resolve(GameManager.Instance.Distance);
        if (!force && _cachedDistrictIndex == district.Index && zoneText.text == BuildDistrictBadgeText(district))
        {
            return;
        }

        _cachedDistrictIndex = district.Index;
        zoneText.text = BuildDistrictBadgeText(district);
        zoneText.color = district.AccentColor;
        zoneText.gameObject.SetActive(true);
    }

    private static string BuildDistrictBadgeText(RunDistrictCatalog.DistrictInfo district)
    {
        return $"DISTRICT // {district.Name.ToUpperInvariant()}\n<size=16>{district.Subtitle.ToUpperInvariant()}</size>";
    }

    private static void AnchorTopLeft(Text target, Vector2 anchoredPosition)
    {
        if (target == null)
        {
            return;
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void AnchorTopRight(Text target, Vector2 anchoredPosition)
    {
        if (target == null)
        {
            return;
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
    }

    private static void AnchorTopCenter(Text target, Vector2 anchoredPosition)
    {
        if (target == null)
        {
            return;
        }

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
    }
}
