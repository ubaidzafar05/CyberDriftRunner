using UnityEngine;

public readonly struct TrailCosmeticProfile
{
    public TrailCosmeticProfile(Color startColor, Color endColor, Color burstColor)
    {
        StartColor = startColor;
        EndColor = endColor;
        BurstColor = burstColor;
    }

    public Color StartColor { get; }
    public Color EndColor { get; }
    public Color BurstColor { get; }
}

public static class ShopCosmeticPalette
{
    public static TrailCosmeticProfile GetTrailProfile(string trailId)
    {
        switch (trailId)
        {
            case "trail_neon_blue":
                return new TrailCosmeticProfile(
                    new Color(0.12f, 0.92f, 1f, 0.95f),
                    new Color(0.1f, 0.42f, 1f, 0f),
                    new Color(0.2f, 1f, 1f, 1f));
            case "trail_void":
                return new TrailCosmeticProfile(
                    new Color(0.7f, 0.18f, 1f, 0.96f),
                    new Color(0.08f, 0.02f, 0.14f, 0f),
                    new Color(0.9f, 0.3f, 1f, 1f));
            default:
                return new TrailCosmeticProfile(
                    new Color(0.2f, 0.8f, 1f, 0.32f),
                    new Color(0.2f, 1f, 1f, 0f),
                    new Color(0.2f, 1f, 1f, 1f));
        }
    }

    public static Color GetWeaponColor(string weaponSkinId)
    {
        switch (weaponSkinId)
        {
            case "weapon_skin_chrome":
                return new Color(0.9f, 0.96f, 1f);
            case "weapon_skin_overdrive":
                return new Color(1f, 0.55f, 0.18f);
            default:
                return new Color(0.15f, 0.95f, 1f);
        }
    }
}
