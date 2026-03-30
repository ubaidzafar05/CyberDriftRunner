using UnityEngine;

/// <summary>
/// Shows helpful gameplay tips on loading screens and game over.
/// Tips rotate based on player progress and behavior.
/// </summary>
public sealed class TipSystem : MonoBehaviour
{
    public static TipSystem Instance { get; private set; }

    private static readonly string[] BeginnerTips =
    {
        "Swipe LEFT or RIGHT to change lanes and avoid obstacles.",
        "Swipe UP to jump over low barriers.",
        "Swipe DOWN to slide under high obstacles.",
        "TAP anywhere to shoot the nearest drone.",
        "HOLD the hack button to slow time and disable threats.",
        "Collect credits to unlock new skins and upgrades.",
        "Power-ups glow brightly — grab them for temporary boosts!",
        "The shield power-up absorbs one hit. Use it wisely!",
    };

    private static readonly string[] IntermediateTips =
    {
        "Chain credit pickups without missing to build a combo multiplier.",
        "Near-miss obstacles to earn bonus points — get close but don't touch!",
        "Watch for new zones every 500m — each zone has its own visual style.",
        "Complete daily challenges for bonus credits and XP.",
        "Login daily to build your reward streak — day 7 has the biggest prize!",
        "Upgrade your hack range to disable more threats at once.",
        "The EMP blast power-up destroys ALL nearby drones instantly.",
        "Your combo resets after 2.5 seconds — keep collecting!",
    };

    private static readonly string[] AdvancedTips =
    {
        "Reach a 6x combo to trigger FEVER MODE — invincibility + 3x score!",
        "Upgrade Credit Magnet to collect credits from further away.",
        "Check your achievements — some grant massive credit rewards.",
        "The speed increases over time — stay focused at high distances.",
        "Use the hack ability right before a dense obstacle cluster.",
        "Kill streaks give escalating score bonuses per consecutive drone.",
        "Sign into Google Play to sync progress across devices.",
        "Check the season pass for exclusive limited-time rewards.",
    };

    private int _lastTipIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GetTip()
    {
        string[] pool = SelectTipPool();
        int index;
        do
        {
            index = Random.Range(0, pool.Length);
        } while (index == _lastTipIndex && pool.Length > 1);

        _lastTipIndex = index;
        return pool[index];
    }

    private string[] SelectTipPool()
    {
        if (ProgressionManager.Instance == null)
            return BeginnerTips;

        int totalRuns = ProgressionManager.Instance.TotalRuns;
        if (totalRuns < 5)
            return BeginnerTips;
        if (totalRuns < 25)
            return IntermediateTips;
        return AdvancedTips;
    }
}
