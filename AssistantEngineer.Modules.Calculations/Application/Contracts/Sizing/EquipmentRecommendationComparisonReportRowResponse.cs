namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationComparisonReportRowResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public string ScenarioName { get; set; } = string.Empty;
    public bool IsWinner { get; set; }
    public string WinningScenarioName { get; set; } = string.Empty;

    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    public int CatalogItemId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public double RequiredCapacityKw { get; set; }
    public double UnitNominalCapacityKw { get; set; }
    public int UnitCount { get; set; }
    public double TotalNominalCapacityKw { get; set; }

    public double CatalogScore { get; set; }
    public double CapexProxyScore { get; set; }
    public double EfficiencyProxyScore { get; set; }
    public double InstallComplexityScore { get; set; }
    public double CompositeScore { get; set; }

    public string Notes { get; set; } = string.Empty;
}