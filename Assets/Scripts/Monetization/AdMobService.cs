using System;

public sealed class AdMobService : IAdService
{
    // Replace with your real AdMob unit IDs before release
    private const string BannerAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";
    private const string InterstitialAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";
    private const string RewardedAdUnitId = "ca-app-pub-XXXXXXXXXXXXXXXX/XXXXXXXXXX";

    private bool _initialized;
    private bool _rewardedReady;
    private bool _interstitialReady;
    public bool CanShowRewardedAd => _rewardedReady;
    public bool CanShowInterstitialAd => _interstitialReady;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

#if UNITY_ANDROID || UNITY_IOS
        // Uncomment when Google Mobile Ads SDK is imported:
        // MobileAds.Initialize(initStatus =>
        // {
        //     LoadRewardedAd();
        //     LoadInterstitialAd();
        // });
        //
        // Respect GDPR consent:
        // var request = new AdRequest.Builder();
        // if (!ConsentManager.IsPersonalizedAdsEnabled)
        //     request.AddExtra("npa", "1");
#endif

        // Mock ready state for development
        _rewardedReady = true;
        _interstitialReady = true;
    }

    public void ShowRewardedAd(string placementId, Action<bool> onCompleted)
    {
#if UNITY_ANDROID || UNITY_IOS
        // Uncomment when Google Mobile Ads SDK is imported:
        // if (_rewardedAd != null && _rewardedAd.CanShowAd())
        // {
        //     _rewardedAd.Show(reward => onCompleted?.Invoke(true));
        //     return;
        // }
#endif

        // Mock: simulate successful reward for development
        UnityEngine.Debug.Log("[AdMob] Mock rewarded ad shown — granting reward");
        onCompleted?.Invoke(true);
    }

    public void ShowInterstitialAd(string placementId, Action<bool> onClosed)
    {
#if UNITY_ANDROID || UNITY_IOS
        // Uncomment when Google Mobile Ads SDK is imported:
        // if (_interstitialAd != null && _interstitialAd.CanShowAd())
        // {
        //     _interstitialAd.Show();
        //     LoadInterstitialAd(); // preload next
        //     onClosed?.Invoke(true);
        //     return;
        // }
#endif

        UnityEngine.Debug.Log("[AdMob] Mock interstitial shown");
        onClosed?.Invoke(true);
    }

    // Call these after importing the Google Mobile Ads Unity Plugin:
    // 1. Import package from: https://github.com/googleads/googleads-mobile-unity/releases
    // 2. Replace ad unit IDs above with your real ones
    // 3. Uncomment the SDK calls in Initialize(), ShowRewardedAd(), ShowInterstitial()
    // 4. In MonetizationManager, replace MockAdService with: new AdMobService()

    // private void LoadRewardedAd()
    // {
    //     var request = new AdRequest();
    //     RewardedAd.Load(RewardedAdUnitId, request, (ad, error) =>
    //     {
    //         _rewardedReady = error == null;
    //         _rewardedAd = ad;
    //         if (ad != null)
    //         {
    //             ad.OnAdFullScreenContentClosed += () => { _rewardedReady = false; LoadRewardedAd(); };
    //         }
    //     });
    // }

    // private void LoadInterstitialAd()
    // {
    //     var request = new AdRequest();
    //     InterstitialAd.Load(InterstitialAdUnitId, request, (ad, error) =>
    //     {
    //         _interstitialReady = error == null;
    //         _interstitialAd = ad;
    //         if (ad != null)
    //         {
    //             ad.OnAdFullScreenContentClosed += () => { _interstitialReady = false; LoadInterstitialAd(); };
    //         }
    //     });
    // }
}
