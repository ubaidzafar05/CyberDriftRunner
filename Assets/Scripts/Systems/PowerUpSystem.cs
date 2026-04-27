using UnityEngine;

public sealed class PowerUpSystem : MonoBehaviour
{
    [SerializeField] private float slowMotionScale = 0.65f;
    [SerializeField] private float speedBoostMultiplier = 1.5f;

    private float shieldTimeLeft;
    private float doubleScoreTimeLeft;
    private float slowMotionTimeLeft;
    private float magnetTimeLeft;
    private float speedBoostTimeLeft;

    public bool HasShield => shieldTimeLeft > 0f;
    public bool HasSpeedBoost => speedBoostTimeLeft > 0f;
    public float SpeedBoostMultiplier => HasSpeedBoost ? speedBoostMultiplier : 1f;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        UpdateTimers(Time.unscaledDeltaTime);
        RefreshGameManagerState();
    }

    public void ApplyPowerUp(PowerUpType type, float duration, bool recordUsage = true)
    {
        if (recordUsage)
        {
            GameManager.Instance?.RegisterPowerUpUsed(1);
        }

        switch (type)
        {
            case PowerUpType.Shield:
                shieldTimeLeft = Mathf.Max(shieldTimeLeft, duration);
                break;
            case PowerUpType.DoubleScore:
                doubleScoreTimeLeft = Mathf.Max(doubleScoreTimeLeft, duration);
                break;
            case PowerUpType.SlowMotion:
                slowMotionTimeLeft = Mathf.Max(slowMotionTimeLeft, duration);
                break;
            case PowerUpType.EmpBlast:
                EnemyDrone.DisableAllActive(75);
                GameManager.Instance?.AddScore(30);
                ScreenShake.Instance?.ShakeEmp();
                HapticFeedback.Instance?.VibrateHeavy();
                break;
            case PowerUpType.Magnet:
                magnetTimeLeft = Mathf.Max(magnetTimeLeft, duration);
                MagnetField magnet = GameManager.Instance?.Player?.GetComponent<MagnetField>();
                if (magnet != null)
                {
                    magnet.Activate(duration);
                }
                break;
            case PowerUpType.SpeedBoost:
                speedBoostTimeLeft = Mathf.Max(speedBoostTimeLeft, duration);
                break;
        }

        RefreshGameManagerState();
    }

    public bool ConsumeShieldIfActive()
    {
        if (!HasShield)
        {
            return false;
        }

        shieldTimeLeft = 0f;
        RefreshGameManagerState();
        return true;
    }

    private void UpdateTimers(float deltaTime)
    {
        shieldTimeLeft = Mathf.Max(0f, shieldTimeLeft - deltaTime);
        doubleScoreTimeLeft = Mathf.Max(0f, doubleScoreTimeLeft - deltaTime);
        slowMotionTimeLeft = Mathf.Max(0f, slowMotionTimeLeft - deltaTime);
        magnetTimeLeft = Mathf.Max(0f, magnetTimeLeft - deltaTime);
        speedBoostTimeLeft = Mathf.Max(0f, speedBoostTimeLeft - deltaTime);
    }

    private void RefreshGameManagerState()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetPowerUpScoreMultiplier(doubleScoreTimeLeft > 0f ? 2 : 1);
        GameManager.Instance.SetPowerUpSlowMotion(slowMotionTimeLeft > 0f, slowMotionScale);
        GameManager.Instance.SetActivePowerUp(GetPowerUpLabel(), GetLongestRemainingTime());
    }

    private string GetPowerUpLabel()
    {
        if (shieldTimeLeft > 0f) return "Shield";
        if (doubleScoreTimeLeft > 0f) return "Double Score";
        if (slowMotionTimeLeft > 0f) return "Slow Motion";
        if (magnetTimeLeft > 0f) return "Magnet";
        if (speedBoostTimeLeft > 0f) return "Speed Boost";
        return "Ready";
    }

    private float GetLongestRemainingTime()
    {
        return Mathf.Max(shieldTimeLeft,
            Mathf.Max(doubleScoreTimeLeft,
                Mathf.Max(slowMotionTimeLeft,
                    Mathf.Max(magnetTimeLeft, speedBoostTimeLeft))));
    }
}
