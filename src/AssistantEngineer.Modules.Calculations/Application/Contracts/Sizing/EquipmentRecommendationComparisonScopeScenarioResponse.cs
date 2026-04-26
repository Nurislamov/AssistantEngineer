namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationComparisonScopeScenarioResponse
{
    public string ScenarioName { get; set; } = string.Empty;

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

    public List<string> Notes { get; set; } = new();
}