using System;
using UnityEngine;

public sealed class MonetizationManager : MonoBehaviour
{
    private enum ServiceMode
    {
        Mock,
        Disabled,
        External
    }

    public static MonetizationManager Instance { get; private set; }

    [SerializeField] private ServiceMode serviceMode = ServiceMode.Mock;

    private IAdService adService;
    private IStoreService storeService;

    public bool CanShowRewardedAd => adService != null && adService.CanShowRewardedAd;
    public bool CanShowInterstitialAd => adService != null && adService.CanShowInterstitialAd;

    public bool CanOfferRevive(float distance)
    {
        return distance > 500f && CanShowRewardedAd;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeServices();
    }

    public void ShowRewardedRevive(Action<bool> onCompleted)
    {
        if (adService == null)
        {
            onCompleted?.Invoke(false);
            return;
        }

        adService.ShowRewardedAd("revive", succeeded =>
        {
            if (succeeded)
            {
                AnalyticsManager.Instance?.TrackAdView("revive");
            }

            onCompleted?.Invoke(succeeded);
        });
    }

    public void ShowRewardedDoubleRewards(Action<bool> onCompleted)
    {
        if (adService == null)
        {
            onCompleted?.Invoke(false);
            return;
        }

        adService.ShowRewardedAd("double_rewards", succeeded =>
        {
            if (succeeded)
            {
                AnalyticsManager.Instance?.TrackAdView("double_rewards");
            }

            onCompleted?.Invoke(succeeded);
        });
    }

    public void ShowInterstitialGameOver()
    {
        if (!CanShowInterstitialAd)
        {
            return;
        }

        if (MonetizationV2.Instance != null && !MonetizationV2.Instance.ShouldShowInterstitial())
        {
            return;
        }

        adService.ShowInterstitialAd("gameover", _ =>
        {
            MonetizationV2.Instance?.RecordInterstitialShown();
            AnalyticsManager.Instance?.TrackAdView("interstitial_gameover");
        });
    }

    public bool CanPurchase(string productId)
    {
        return storeService != null && storeService.CanPurchase(productId);
    }

    public void Purchase(string productId, Action<bool> onCompleted)
    {
        if (!CanPurchase(productId))
        {
            onCompleted?.Invoke(false);
            return;
        }

        storeService.Purchase(productId, onCompleted);
    }

    private void InitializeServices()
    {
        switch (serviceMode)
        {
            case ServiceMode.Mock:
                adService = new MockAdService();
                storeService = new MockStoreService();
                break;
            case ServiceMode.Disabled:
                adService = null;
                storeService = null;
                break;
            case ServiceMode.External:
#if UNITY_EDITOR
                Debug.LogWarning("[Monetization] External mode selected, but no production adapters are registered. Falling back to disabled mode.");
#endif
                adService = null;
                storeService = null;
                break;
        }

        adService?.Initialize();
        storeService?.Initialize();
    }
}
