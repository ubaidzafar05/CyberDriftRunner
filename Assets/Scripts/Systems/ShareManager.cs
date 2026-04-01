using UnityEngine;

public sealed class ShareManager : MonoBehaviour
{
    private enum ShareMode
    {
        Disabled,
        Mock,
        Native
    }

    public static ShareManager Instance { get; private set; }

    [SerializeField] private ShareMode editorMode = ShareMode.Mock;
    [SerializeField] private bool logMockShares = true;

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

    public void ShareScore(RunSummary summary)
    {
        string shareText = $"I scored {summary.Score} and ran {summary.Distance:0}m in Cyber Drift Runner! Can you beat me? #CyberDriftRunner";
        CaptureAndShare(shareText);
    }

    private void CaptureAndShare(string text)
    {
        switch (ResolveShareMode())
        {
            case ShareMode.Mock:
                if (logMockShares)
                {
                    Debug.Log($"[Share] {text}");
                }
                break;
            case ShareMode.Native:
#if UNITY_ANDROID && !UNITY_EDITOR
                ShareAndroid(text);
#elif UNITY_IOS && !UNITY_EDITOR
                ShareIOS(text);
#endif
                break;
            default:
                Debug.LogWarning("[ShareManager] Native sharing is disabled on this platform.");
                break;
        }
    }

    private ShareMode ResolveShareMode()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        return ShareMode.Native;
#elif UNITY_EDITOR
        return editorMode;
#else
        return ShareMode.Disabled;
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
        Debug.Log($"[Share iOS] {text}");
    }
#endif
}
