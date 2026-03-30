using UnityEngine;

public sealed class HapticFeedback : MonoBehaviour
{
    public static HapticFeedback Instance { get; private set; }

    private const string VibrationPrefKey = "cdr.vibration";

    public bool IsEnabled => PlayerPrefs.GetInt(VibrationPrefKey, 1) == 1;

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

    public void VibrateLight()
    {
        if (!IsEnabled || !Application.isMobilePlatform)
        {
            return;
        }

#if UNITY_ANDROID
        VibrateAndroid(15);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void VibrateMedium()
    {
        if (!IsEnabled || !Application.isMobilePlatform)
        {
            return;
        }

#if UNITY_ANDROID
        VibrateAndroid(30);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void VibrateHeavy()
    {
        if (!IsEnabled || !Application.isMobilePlatform)
        {
            return;
        }

#if UNITY_ANDROID
        VibrateAndroid(60);
#elif UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void VibrateOnHit() => VibrateMedium();
    public void VibrateOnCollect() => VibrateLight();
    public void VibrateOnDeath() => VibrateHeavy();
    public void VibrateOnPowerUp() => VibrateMedium();

#if UNITY_ANDROID
    private static void VibrateAndroid(long milliseconds)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                if (vibrator != null)
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
        catch (System.Exception)
        {
            // Vibration not available on this device
        }
    }
#endif
}
