using System;
using UnityEngine;

public sealed class MockAdService : IAdService
{
    public bool CanShowRewardedAd => true;
    public bool CanShowInterstitialAd => true;

    public void Initialize()
    {
    }

    public void ShowRewardedAd(string placementId, Action<bool> onCompleted)
    {
        onCompleted?.Invoke(true);
    }

    public void ShowInterstitialAd(string placementId, Action<bool> onClosed)
    {
        onClosed?.Invoke(true);
    }
}
