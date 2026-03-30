using System;

public interface IStoreService
{
    void Initialize();
    bool CanPurchase(string productId);
    void Purchase(string productId, Action<bool> onCompleted);
}
