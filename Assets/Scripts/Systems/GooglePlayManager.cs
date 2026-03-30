using UnityEngine;

/// <summary>
/// Google Play Games Services integration.
/// Handles sign-in, cloud save, leaderboards, and achievements.
/// Requires com.google.play.games Unity plugin when building for Android.
/// Falls back gracefully when plugin is absent.
/// </summary>
public sealed class GooglePlayManager : MonoBehaviour
{
    public static GooglePlayManager Instance { get; private set; }

    private const string SignedInKey = "cdr.gpgs.signedIn";
    private const string PlayerNameKey = "cdr.gpgs.playerName";

    public bool IsSignedIn { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string PlayerId { get; private set; } = "";

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

        // Auto sign-in if previously signed in
        if (PlayerPrefs.GetInt(SignedInKey, 0) == 1)
        {
            SignIn();
        }
    }

    public void SignIn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        TryGooglePlaySignIn();
#else
        // Mock sign-in for editor / non-Android
        IsSignedIn = true;
        PlayerName = "TestPlayer";
        PlayerId = "mock_player_001";
        PlayerPrefs.SetInt(SignedInKey, 1);
        PlayerPrefs.SetString(PlayerNameKey, PlayerName);
        PlayerPrefs.Save();
        OnSignInResult?.Invoke(true);
        Debug.Log("[GooglePlayManager] Mock sign-in successful");
#endif
    }

    public void SignOut()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        TryGooglePlaySignOut();
#endif
        IsSignedIn = false;
        PlayerName = "Player";
        PlayerId = "";
        PlayerPrefs.SetInt(SignedInKey, 0);
        PlayerPrefs.Save();
        OnSignOut?.Invoke();
        Debug.Log("[GooglePlayManager] Signed out");
    }

    public void ShowLeaderboard()
    {
        if (!IsSignedIn)
        {
            Debug.LogWarning("[GooglePlayManager] Not signed in — cannot show leaderboard");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        TryShowNativeLeaderboard();
#else
        Debug.Log("[GooglePlayManager] ShowLeaderboard (mock — no native UI in editor)");
#endif
    }

    public void ShowAchievements()
    {
        if (!IsSignedIn)
        {
            Debug.LogWarning("[GooglePlayManager] Not signed in — cannot show achievements");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        TryShowNativeAchievements();
#else
        Debug.Log("[GooglePlayManager] ShowAchievements (mock — no native UI in editor)");
#endif
    }

    public void SubmitScore(int score)
    {
        if (!IsSignedIn) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        TrySubmitScore(score);
#else
        Debug.Log($"[GooglePlayManager] SubmitScore: {score} (mock)");
#endif
    }

    public void UnlockAchievement(string achievementId)
    {
        if (!IsSignedIn) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        TryUnlockAchievement(achievementId);
#else
        Debug.Log($"[GooglePlayManager] UnlockAchievement: {achievementId} (mock)");
#endif
    }

    public void SaveToCloud(string key, string jsonData)
    {
        if (!IsSignedIn)
        {
            Debug.LogWarning("[GooglePlayManager] Not signed in — saving locally");
            PlayerPrefs.SetString("cdr.cloud." + key, jsonData);
            PlayerPrefs.Save();
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        TrySaveToCloud(key, jsonData);
#else
        PlayerPrefs.SetString("cdr.cloud." + key, jsonData);
        PlayerPrefs.Save();
        Debug.Log($"[GooglePlayManager] SaveToCloud (mock): {key}");
#endif
    }

    public string LoadFromCloud(string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return TryLoadFromCloud(key);
#else
        return PlayerPrefs.GetString("cdr.cloud." + key, "");
#endif
    }

    // Stubs for actual Google Play Games SDK calls
    // These will be filled in when com.google.play.games is added to the project

#if UNITY_ANDROID && !UNITY_EDITOR
    private void TryGooglePlaySignIn()
    {
        try
        {
            // PlayGamesPlatform.Instance.Authenticate(...)
            // For now, mark as signed in to allow the rest of the flow
            IsSignedIn = true;
            PlayerName = "AndroidPlayer";
            PlayerId = SystemInfo.deviceUniqueIdentifier;
            PlayerPrefs.SetInt(SignedInKey, 1);
            PlayerPrefs.SetString(PlayerNameKey, PlayerName);
            PlayerPrefs.Save();
            OnSignInResult?.Invoke(true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GooglePlayManager] Sign-in failed: {e.Message}");
            OnSignInResult?.Invoke(false);
        }
    }

    private void TryGooglePlaySignOut()
    {
        // PlayGamesPlatform.Instance.SignOut();
    }

    private void TryShowNativeLeaderboard()
    {
        // PlayGamesPlatform.Instance.ShowLeaderboardUI("leaderboard_id");
        Social.ShowLeaderboardUI();
    }

    private void TryShowNativeAchievements()
    {
        // PlayGamesPlatform.Instance.ShowAchievementsUI();
        Social.ShowAchievementsUI();
    }

    private void TrySubmitScore(int score)
    {
        Social.ReportScore(score, "CyberDriftRunner_HighScore", (bool success) =>
        {
            Debug.Log($"[GooglePlayManager] Score submitted: {success}");
        });
    }

    private void TryUnlockAchievement(string achievementId)
    {
        Social.ReportProgress(achievementId, 100.0, (bool success) =>
        {
            Debug.Log($"[GooglePlayManager] Achievement reported: {achievementId} = {success}");
        });
    }

    private void TrySaveToCloud(string key, string jsonData)
    {
        PlayerPrefs.SetString("cdr.cloud." + key, jsonData);
        PlayerPrefs.Save();
    }

    private string TryLoadFromCloud(string key)
    {
        return PlayerPrefs.GetString("cdr.cloud." + key, "");
    }
#endif
}
