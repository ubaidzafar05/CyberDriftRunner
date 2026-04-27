using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/Visual Asset Catalog", fileName = "VisualAssetCatalog")]
public sealed class VisualAssetCatalog : ScriptableObject
{
    [Header("Core Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject barrierPrefab;
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private GameObject dronePrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject bossHazardPrefab;
    [SerializeField] private GameObject bossStagePrefab;
    [SerializeField] private GameObject creditPrefab;

    [Header("Gameplay Prefab Families")]
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private GameObject[] gatewayChunks;
    [SerializeField] private GameObject[] commerceChunks;
    [SerializeField] private GameObject[] securityChunks;

    [Header("Material Overrides")]
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Material accentMaterial;
    [SerializeField] private Material alternateAccentMaterial;
    [SerializeField] private Material tertiaryAccentMaterial;
    [SerializeField] private Material warningMaterial;

    [Header("Policy")]
    [SerializeField] private bool allowGeneratedFallbacks = true;

    public GameObject PlayerPrefab => playerPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject BarrierPrefab => barrierPrefab;
    public GameObject CarPrefab => carPrefab;
    public GameObject DronePrefab => dronePrefab;
    public GameObject BossPrefab => bossPrefab;
    public GameObject BossHazardPrefab => bossHazardPrefab;
    public GameObject BossStagePrefab => bossStagePrefab;
    public GameObject CreditPrefab => creditPrefab;
    public GameObject[] PowerUpPrefabs => powerUpPrefabs;
    public GameObject[] GatewayChunks => gatewayChunks;
    public GameObject[] CommerceChunks => commerceChunks;
    public GameObject[] SecurityChunks => securityChunks;
    public Material RoadMaterial => roadMaterial;
    public Material AccentMaterial => accentMaterial;
    public Material AlternateAccentMaterial => alternateAccentMaterial;
    public Material TertiaryAccentMaterial => tertiaryAccentMaterial;
    public Material WarningMaterial => warningMaterial;
    public bool AllowGeneratedFallbacks => allowGeneratedFallbacks;
    public bool RequireAuthoredAssets => !allowGeneratedFallbacks;
}
