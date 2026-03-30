using UnityEngine;

public sealed class MobilePerformanceManager : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool disableVsync = true;
    [SerializeField] private bool disableAntialiasingOnMobile = true;
    [SerializeField] private bool keepScreenAwake = true;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        if (disableVsync)
        {
            QualitySettings.vSyncCount = 0;
        }

        if (disableAntialiasingOnMobile && Application.isMobilePlatform)
        {
            QualitySettings.antiAliasing = 0;
        }

        if (keepScreenAwake)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
