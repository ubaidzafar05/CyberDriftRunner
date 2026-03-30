using UnityEngine;

public static class SkinCatalog
{
    public static SkinDefinition[] CreateDefaultCatalog()
    {
        return new[]
        {
            new SkinDefinition
            {
                Id = "street_default",
                DisplayName = "Street Default",
                SoftCurrencyCost = 0,
                BaseColor = new Color(0.08f, 0.75f, 1f),
                EmissionColor = new Color(0.2f, 1f, 1f)
            },
            new SkinDefinition
            {
                Id = "magenta_flux",
                DisplayName = "Magenta Flux",
                SoftCurrencyCost = 120,
                BaseColor = new Color(1f, 0.12f, 0.7f),
                EmissionColor = new Color(1f, 0.2f, 1f)
            },
            new SkinDefinition
            {
                Id = "gold_circuit",
                DisplayName = "Gold Circuit",
                SoftCurrencyCost = 240,
                BaseColor = new Color(1f, 0.8f, 0.16f),
                EmissionColor = new Color(1f, 0.9f, 0.25f)
            },
            new SkinDefinition
            {
                Id = "void_ghost",
                DisplayName = "Void Ghost",
                SoftCurrencyCost = 0,
                PremiumProductId = "skin_void_ghost",
                BaseColor = new Color(0.32f, 0.34f, 0.44f),
                EmissionColor = new Color(0.55f, 0.6f, 1f)
            }
        };
    }
}
