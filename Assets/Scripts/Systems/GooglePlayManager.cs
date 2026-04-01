using UnityEngine;

public sealed class GooglePlayManager : MonoBehaviour
{
    private enum ServiceMode
    {
        Disabled,
        Mock,
        Native
    }

    public static GooglePlayManager Instance { get; private set; }

    private const string SignedInKey = "cdr.gpgs.signedIn";
    private const string PlayerNameKey = "cdr.gpgs.playerName";
    private const string CloudPrefix = "cdr.cloud.";

    [SerializeField] private ServiceMode editorMode = ServiceMode.Mock;
    [SerializeField] private bool logMockActions = true;

    public bool IsSignedIn { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string PlayerId { get; private set; } = string.Empty;

    public event System.Action<bool> OnSignInResult;
    public event System.Action OnSignOut;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (PlayerPrefs.GetInt(SignedInKey, 0) == 1)
        {
            SignIn();
        }
    }

    public void SignIn()
    {
        switch (ResolveServiceMode())
        {
            case ServiceMode.Native:
#if UNITY_ANDROID && !UNITY_EDITOR
                TryGooglePlaySignIn();
#endif
                break;
            case ServiceMode.Mock:
                ApplySignedInState("TestPlayer", "mock_player_001");
                LogMock("SignIn");
                break;
            default:
                ClearSignedInState();
                OnSignInResult?.Invoke(false);
                Debug.LogWarning("[GooglePlayManager] Google Play services are disabled on this platform.");
                break;
        }
    }

    public void SignOut()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native)
        {
            TryGooglePlaySignOut();
        }
#endif
        ClearSignedInState();
        OnSignOut?.Invoke();
    }

    public void ShowLeaderboard()
    {
        if (!IsSignedIn)
        {
            Debug.LogWarning("[GooglePlayManager] Not signed in - cannot show leaderboard.");
            return;
        }

        if (ResolveServiceMode() == ServiceMode.Mock)
        {
            LogMock("ShowLeaderboard");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native)
        {
            TryShowNativeLeaderboard();
        }
#endif
    }

    public void ShowAchievements()
    {
        if (!IsSignedIn)
        {
            Debug.LogWarning("[GooglePlayManager] Not signed in - cannot show achievements.");
            return;
        }

        if (ResolveServiceMode() == ServiceMode.Mock)
        {
            LogMock("ShowAchievements");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native)
        {
            TryShowNativeAchievements();
        }
#endif
    }

    public void SubmitScore(int score)
    {
        if (!IsSignedIn)
        {
            return;
        }

        if (ResolveServiceMode() == ServiceMode.Mock)
        {
            LogMock($"SubmitScore {score}");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native)
        {
            TrySubmitScore(score);
        }
#endif
    }

    public void UnlockAchievement(string achievementId)
    {
        if (!IsSignedIn || string.IsNullOrWhiteSpace(achievementId))
        {
            return;
        }

        if (ResolveServiceMode() == ServiceMode.Mock)
        {
            LogMock($"UnlockAchievement {achievementId}");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native)
        {
            TryUnlockAchievement(achievementId);
        }
#endif
    }

    public void SaveToCloud(string key, string jsonData)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(jsonData))
        {
            return;
        }

        PlayerPrefs.SetString(CloudPrefix + key, jsonData);
        PlayerPrefs.Save();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native && IsSignedIn)
        {
            TrySaveToCloud(key, jsonData);
            return;
        }
#endif

        if (ResolveServiceMode() == ServiceMode.Mock)
        {
            LogMock($"SaveToCloud {key}");
        }
    }

    public string LoadFromCloud(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ResolveServiceMode() == ServiceMode.Native && IsSignedIn)
        {
            return TryLoadFromCloud(key);
        }
#endif

        return PlayerPrefs.GetString(CloudPrefix + key, string.Empty);
    }

    private ServiceMode ResolveServiceMode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return ServiceMode.Native;
#elif UNITY_EDITOR
        return editorMode;
#else
        return ServiceMode.Disabled;
#endif
    }

    private void ApplySignedInState(string playerName, string playerId)
    {
        IsSignedIn = true;
        PlayerName = playerName;
        PlayerId = playerId;
        PlayerPrefs.SetInt(SignedInKey, 1);
        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
        PlayerPrefs.Save();
        OnSignInResult?.Invoke(true);
    }

    private void ClearSignedInState()
    {
        IsSignedIn = false;
        PlayerName = "Player";
        PlayerId = string.Empty;
        PlayerPrefs.SetInt(SignedInKey, 0);
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.Save();
    }

    private void LogMock(string action)
    {
        if (logMockActions)
        {
            Debug.Log($"[GooglePlayManager] Mock action: {action}");
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void TryGooglePlaySignIn()
    {
        try
        {
            ApplySignedInState("AndroidPlayer", SystemInfo.deviceUniqueIdentifier);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[GooglePlayManager] Sign-in failed: {exception.Message}");
            ClearSignedInState();
            OnSignInResult?.Invoke(false);
        }
    }

    private void TryGooglePlaySignOut()
    {
    }

    private void TryShowNativeLeaderboard()
    {
        Social.ShowLeaderboardUI();
    }

    private void TryShowNativeAchievements()
    {
        Social.ShowAchievementsUI();
    }

    private void TrySubmitScore(int score)
    {
        Social.ReportScore(score, "CyberDriftRunner_HighScore", success =>
        {
            if (!success)
            {
                Debug.LogWarning("[GooglePlayManager] Native score submission failed.");
            }
        });
    }

    private void TryUnlockAchievement(string achievementId)
    {
        Social.ReportProgress(achievementId, 100.0, success =>
        {
            if (!success)
            {
                Debug.LogWarning($"[GooglePlayManager] Achievement unlock failed: {achievementId}");
            }
        });
    }

    private void TrySaveToCloud(string key, string jsonData)
    {
        PlayerPrefs.SetString(CloudPrefix + key, jsonData);
        PlayerPrefs.Save();
    }

    private string TryLoadFromCloud(string key)
    {
        return PlayerPrefs.GetString(CloudPrefix + key, string.Empty);
    }
#endif
}
