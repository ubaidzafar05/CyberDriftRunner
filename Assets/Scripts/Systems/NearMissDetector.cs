using UnityEngine;

/// <summary>
/// Adds visual/audio feedback to near-miss events detected by ComboSystem.
/// Subscribes to ComboSystem.OnNearMiss and shows floating text + screen flash.
/// </summary>
public sealed class NearMissDetector : MonoBehaviour
{
    public static NearMissDetector Instance { get; private set; }

    private int _nearMissCount;

    public int NearMissCount => _nearMissCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.OnNearMiss += HandleNearMiss;
        }
    }

    private void OnDisable()
    {
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.OnNearMiss -= HandleNearMiss;
        }
    }

    private void HandleNearMiss(int bonus)
    {
        _nearMissCount++;

        if (GameManager.Instance?.Player != null)
        {
            Vector3 pos = GameManager.Instance.Player.transform.position + Vector3.up * 2.5f;
            FloatingTextManager.Instance?.SpawnAt(pos, $"CLOSE CALL +{bonus}", Color.yellow);
        }

        ScreenFlash.Instance?.FlashNearMiss();
    }

    public void ResetForRun()
    {
        _nearMissCount = 0;
    }
}
