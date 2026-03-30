using UnityEngine;

public sealed class ShareManager : MonoBehaviour
{
    public static ShareManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShareScore(RunSummary summary)
    {
        string shareText = $"I scored {summary.Score} and ran {summary.Distance:0}m in Cyber Drift Runner! Can you beat me? #CyberDriftRunner";
        CaptureAndShare(shareText);
    }

    private void CaptureAndShare(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ShareAndroid(text);
#elif UNITY_IOS && !UNITY_EDITOR
        ShareIOS(text);
#else
        Debug.Log($"[Share] {text}");
#endif
    }

#if UNITY_ANDROID
    private static void ShareAndroid(string text)
    {
        using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
        using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
        {
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("setType", "text/plain");
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

            using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Share Score");
                activity.Call("startActivity", chooser);
            }
        }
    }
#endif

#if UNITY_IOS
    private static void ShareIOS(string text)
    {
        // iOS native share via Social framework — requires a native plugin bridge
        // For now, log and skip
        Debug.Log($"[Share iOS] {text}");
    }
#endif
}
