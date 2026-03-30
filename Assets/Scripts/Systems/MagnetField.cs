using UnityEngine;

/// <summary>
/// Magnet power-up that auto-collects credits within a radius.
/// Spawned by ObstacleSpawner as a new power-up type.
/// </summary>
public sealed class MagnetField : MonoBehaviour
{
    [SerializeField] private float magnetRadius = 6f;
    [SerializeField] private float pullSpeed = 18f;

    private float _timer;
    private bool _active;

    public void Activate(float duration)
    {
        _active = true;
        _timer = duration;
    }

    public void Deactivate()
    {
        _active = false;
        _timer = 0f;
    }

    private void Update()
    {
        if (!_active) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Deactivate();
            return;
        }

        PullCredits();
    }

    private void PullCredits()
    {
        if (GameManager.Instance?.Player == null) return;

        Vector3 playerPos = GameManager.Instance.Player.transform.position;
        float radiusSqr = magnetRadius * magnetRadius;

        // Apply magnet upgrade bonus
        float bonusRadius = UpgradeSystem.Instance != null
            ? UpgradeSystem.Instance.GetMultiplier(UpgradeSystem.UpgradeType.CreditMagnet)
            : 0f;
        float effectiveRadius = magnetRadius + bonusRadius;
        float effectiveRadiusSqr = effectiveRadius * effectiveRadius;

        CreditPickup[] credits = FindObjectsByType<CreditPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < credits.Length; i++)
        {
            if (credits[i] == null || !credits[i].gameObject.activeInHierarchy)
                continue;

            Vector3 toPlayer = playerPos - credits[i].transform.position;
            if (toPlayer.sqrMagnitude <= effectiveRadiusSqr)
            {
                credits[i].transform.position = Vector3.MoveTowards(
                    credits[i].transform.position,
                    playerPos,
                    pullSpeed * Time.deltaTime
                );
            }
        }
    }
}
