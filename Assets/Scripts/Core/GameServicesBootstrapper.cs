using UnityEngine;

public sealed class GameServicesBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RegisterServices();
    }

    private static void RegisterServices()
    {
        ServiceLocator.Register(GameManager.Instance);
        ServiceLocator.Register(AnalyticsManager.Instance);
        ServiceLocator.Register(ProgressionManager.Instance);
        ServiceLocator.Register(EconomySystem.Instance);
        ServiceLocator.Register(ShopSystem.Instance);
        ServiceLocator.Register(LiveOpsSystem.Instance);
        ServiceLocator.Register(NetworkSessionManager.Instance);
        ServiceLocator.Register(GhostRunManager.Instance);
        ServiceLocator.Register(CloudSaveManager.Instance);
    }
}
