using System;
using UnityEngine;

public sealed class MonetizationManager : MonoBehaviour
{
    public static MonetizationManager Instance { get; private set; }

    [SerializeField] private bool useMockServices = true;

    private IAdService adService;
    private IStoreService storeService;

    public bool CanShowRewardedAd => adService != null && adService.CanShowRewardedAd;
    public bool CanShowInterstitialAd => adService != null && adService.CanShowInterstitialAd;

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
        adService?.ShowRewardedAd("revive", onCompleted);
    }

    public void ShowInterstitialGameOver()
    {
        if (!CanShowInterstitialAd)
        {
            return;
        }

        adService.ShowInterstitialAd("gameover", null);
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
        if (useMockServices)
        {
            adService = new MockAdService();
            storeService = new MockStoreService();
        }

        adService?.Initialize();
        storeService?.Initialize();
    }
}
