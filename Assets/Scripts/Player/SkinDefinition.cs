using UnityEngine;

[System.Serializable]
public sealed class SkinDefinition
{
    public string Id;
    public string DisplayName;
    public int SoftCurrencyCost;
    public string PremiumProductId;
    public Color BaseColor;
    public Color EmissionColor;

    public bool IsPremium => !string.IsNullOrWhiteSpace(PremiumProductId);
}
