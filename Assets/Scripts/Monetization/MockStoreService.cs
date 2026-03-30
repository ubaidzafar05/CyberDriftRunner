using System;
using UnityEngine;

public sealed class MockStoreService : IStoreService
{
    public void Initialize()
    {
    }

    public bool CanPurchase(string productId)
    {
        return !string.IsNullOrWhiteSpace(productId);
    }

    public void Purchase(string productId, Action<bool> onCompleted)
    {
        onCompleted?.Invoke(true);
    }
}
