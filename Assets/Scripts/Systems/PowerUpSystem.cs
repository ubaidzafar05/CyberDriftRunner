using UnityEngine;

public sealed class PowerUpSystem : MonoBehaviour
{
    [SerializeField] private float slowMotionScale = 0.65f;

    private float shieldTimeLeft;
    private float doubleScoreTimeLeft;
    private float slowMotionTimeLeft;

    public bool HasShield => shieldTimeLeft > 0f;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        UpdateTimers(Time.unscaledDeltaTime);
        RefreshGameManagerState();
    }

    public void ApplyPowerUp(PowerUpType type, float duration)
    {
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
                GameManager.Instance.AddScore(30);
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
    }

    private void RefreshGameManagerState()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetScoreMultiplier(doubleScoreTimeLeft > 0f ? 2 : 1);
        GameManager.Instance.SetPowerUpSlowMotion(slowMotionTimeLeft > 0f, slowMotionScale);
        GameManager.Instance.SetActivePowerUp(GetPowerUpLabel(), GetLongestRemainingTime());
    }

    private string GetPowerUpLabel()
    {
        if (shieldTimeLeft > 0f)
        {
            return "Shield";
        }

        if (doubleScoreTimeLeft > 0f)
        {
            return "Double Score";
        }

        if (slowMotionTimeLeft > 0f)
        {
            return "Slow Motion";
        }

        return "Ready";
    }

    private float GetLongestRemainingTime()
    {
        return Mathf.Max(shieldTimeLeft, Mathf.Max(doubleScoreTimeLeft, slowMotionTimeLeft));
    }
}
