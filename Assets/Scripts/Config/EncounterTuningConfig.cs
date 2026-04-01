using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/Encounter Tuning", fileName = "EncounterTuningConfig")]
public sealed class EncounterTuningConfig : ScriptableObject
{
    [System.Serializable]
    public struct DistrictDefinition
    {
        public string name;
        public float startDistance;
        public float difficultyBias;
        public float droneChance;
        public float powerUpChance;
        public float spacingScale;
    }

    [Header("Spawner")]
    [SerializeField] private float spawnDistanceAhead = 42f;
    [SerializeField] private float minRowSpacing = 8.5f;
    [SerializeField] private float maxRowSpacing = 14f;
    [SerializeField] private int obstaclePreload = 16;
    [SerializeField] private int dronePreload = 8;
    [SerializeField] private int collectiblePreload = 10;
    [SerializeField] private float laneOffset = 2.5f;

    [Header("Districts")]
    [SerializeField] private DistrictDefinition[] districts =
    {
        new DistrictDefinition { name = "Gateway", startDistance = 0f, difficultyBias = 0.12f, droneChance = 0.1f, powerUpChance = 0.22f, spacingScale = 1f },
        new DistrictDefinition { name = "Commerce Strip", startDistance = 550f, difficultyBias = 0.3f, droneChance = 0.18f, powerUpChance = 0.18f, spacingScale = 0.95f },
        new DistrictDefinition { name = "Transit Spine", startDistance = 1250f, difficultyBias = 0.52f, droneChance = 0.26f, powerUpChance = 0.16f, spacingScale = 0.89f },
        new DistrictDefinition { name = "Security Zone", startDistance = 2150f, difficultyBias = 0.74f, droneChance = 0.34f, powerUpChance = 0.12f, spacingScale = 0.84f }
    };

    public float SpawnDistanceAhead => Mathf.Max(10f, spawnDistanceAhead);
    public float MinRowSpacing => Mathf.Max(1f, minRowSpacing);
    public float MaxRowSpacing => Mathf.Max(MinRowSpacing, maxRowSpacing);
    public int ObstaclePreload => Mathf.Max(1, obstaclePreload);
    public int DronePreload => Mathf.Max(1, dronePreload);
    public int CollectiblePreload => Mathf.Max(1, collectiblePreload);
    public float LaneOffset => Mathf.Max(1f, laneOffset);
    public DistrictDefinition[] Districts => districts;
}
