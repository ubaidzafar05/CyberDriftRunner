using UnityEngine;

public static class RunDistrictCatalog
{
    public struct DistrictInfo
    {
        public DistrictInfo(int index, float startDistance, string name, string subtitle, Color accentColor)
        {
            Index = index;
            StartDistance = startDistance;
            Name = name;
            Subtitle = subtitle;
            AccentColor = accentColor;
        }

        public int Index { get; }
        public float StartDistance { get; }
        public string Name { get; }
        public string Subtitle { get; }
        public Color AccentColor { get; }
    }

    private static readonly DistrictInfo[] Districts =
    {
        new DistrictInfo(0, 0f, "Neon Gateway", "Arrival Spine", new Color(0.18f, 0.95f, 1f)),
        new DistrictInfo(1, 850f, "Market Strip", "Commerce Veins", new Color(1f, 0.84f, 0.24f)),
        new DistrictInfo(2, 1800f, "Security Grid", "Citadel Threshold", new Color(0.76f, 0.38f, 1f)),
        new DistrictInfo(3, 2700f, "Core Citadel", "Lockdown Spine", new Color(1f, 0.96f, 0.96f))
    };

    public static DistrictInfo Resolve(float distance)
    {
        DistrictInfo resolved = Districts[0];
        for (int i = 1; i < Districts.Length; i++)
        {
            if (distance < Districts[i].StartDistance)
            {
                break;
            }

            resolved = Districts[i];
        }

        return resolved;
    }

    public static DistrictInfo GetByIndex(int index)
    {
        int clampedIndex = Mathf.Clamp(index, 0, Districts.Length - 1);
        return Districts[clampedIndex];
    }

    public static string ResolveName(float distance) => Resolve(distance).Name;
}
