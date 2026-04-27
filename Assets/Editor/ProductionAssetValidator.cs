using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ProductionAssetValidator
{
    public readonly struct ValidationResult
    {
        public ValidationResult(bool isValid, string report)
        {
            IsValid = isValid;
            Report = report;
        }

        public bool IsValid { get; }
        public string Report { get; }
    }

    [MenuItem("Cyber Drift Runner/Validate Production Asset Catalog")]
    public static void ValidateConfiguredCatalog()
    {
        GameplayConfigBootstrapper.ConfigBundle configs = GameplayConfigBootstrapper.EnsureConfigs();
        ValidationResult result = Validate(configs.VisualAssets);
        if (result.IsValid)
        {
            Debug.Log(result.Report);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Production Asset Catalog", result.Report, "OK");
            }
            return;
        }

        Debug.LogError(result.Report);
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Production Asset Catalog", result.Report, "OK");
        }
    }

    public static ValidationResult Validate(VisualAssetCatalog catalog)
    {
        if (catalog == null)
        {
            return new ValidationResult(false, "Production asset validation failed.\n- VisualAssetCatalog is missing.");
        }

        List<string> issues = new List<string>();
        bool strict = catalog.RequireAuthoredAssets;
        ValidatePrefab(catalog.PlayerPrefab, "PlayerPrefab", issues, strict);
        ValidatePrefab(catalog.ProjectilePrefab, "ProjectilePrefab", issues, strict);
        ValidatePrefab(catalog.BarrierPrefab, "BarrierPrefab", issues, strict);
        ValidatePrefab(catalog.CarPrefab, "CarPrefab", issues, strict);
        ValidatePrefab(catalog.DronePrefab, "DronePrefab", issues, strict);
        ValidatePrefab(catalog.BossPrefab, "BossPrefab", issues, strict);
        ValidatePrefab(catalog.BossHazardPrefab, "BossHazardPrefab", issues, strict);
        ValidatePrefab(catalog.BossStagePrefab, "BossStagePrefab", issues, strict);
        ValidatePrefab(catalog.CreditPrefab, "CreditPrefab", issues, strict);
        ValidatePrefabArray(catalog.PowerUpPrefabs, "PowerUpPrefabs", issues, strict);
        ValidatePrefabArray(catalog.GatewayChunks, "GatewayChunks", issues, strict);
        ValidatePrefabArray(catalog.CommerceChunks, "CommerceChunks", issues, strict);
        ValidatePrefabArray(catalog.SecurityChunks, "SecurityChunks", issues, strict);
        ValidateMaterial(catalog.RoadMaterial, "RoadMaterial", issues, strict);
        ValidateMaterial(catalog.AccentMaterial, "AccentMaterial", issues, strict);
        ValidateMaterial(catalog.AlternateAccentMaterial, "AlternateAccentMaterial", issues, strict);
        ValidateMaterial(catalog.TertiaryAccentMaterial, "TertiaryAccentMaterial", issues, strict);
        ValidateMaterial(catalog.WarningMaterial, "WarningMaterial", issues, strict);

        StringBuilder report = new StringBuilder();
        report.AppendLine("Production asset catalog validation");
        report.AppendLine($"Catalog: {AssetDatabase.GetAssetPath(catalog)}");
        report.AppendLine($"Authored-only mode: {catalog.RequireAuthoredAssets}");
        if (issues.Count == 0)
        {
            report.Append("Result: valid.");
            return new ValidationResult(true, report.ToString());
        }

        report.AppendLine(strict ? "Result: invalid." : "Result: incomplete but fallback mode is enabled.");
        for (int i = 0; i < issues.Count; i++)
        {
            report.Append("- ").AppendLine(issues[i]);
        }

        return new ValidationResult(!strict, report.ToString());
    }

    private static void ValidatePrefab(GameObject prefab, string fieldName, List<string> issues, bool strict)
    {
        if (strict && prefab == null)
        {
            issues.Add($"{fieldName} is missing.");
        }
    }

    private static void ValidateMaterial(Material material, string fieldName, List<string> issues, bool strict)
    {
        if (strict && material == null)
        {
            issues.Add($"{fieldName} is missing.");
        }
    }

    private static void ValidatePrefabArray(GameObject[] prefabs, string fieldName, List<string> issues, bool strict)
    {
        if (!strict)
        {
            return;
        }

        if (prefabs == null || prefabs.Length == 0)
        {
            issues.Add($"{fieldName} has no entries.");
            return;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null)
            {
                issues.Add($"{fieldName}[{i}] is null.");
            }
        }
    }
}
