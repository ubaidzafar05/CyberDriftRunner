using System;

public interface IAdService
{
    bool CanShowRewardedAd { get; }
    bool CanShowInterstitialAd { get; }
    void Initialize();
    void ShowRewardedAd(string placementId, Action<bool> onCompleted);
    void ShowInterstitialAd(string placementId, Action<bool> onClosed);
}
