namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationScenarioSummaryResponse
{
    public string ScenarioName { get; set; } = string.Empty;

    public AutosizingGranularity Granularity { get; set; }

    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    public int ScopeCount { get; set; }

    public double AverageRequiredCapacityKw { get; set; }
    public double AverageTopCompositeScore { get; set; }
}