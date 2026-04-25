namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingEquipmentRecommendationComparisonResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public AutosizingGranularity Granularity { get; set; }

    public List<EquipmentRecommendationScenarioSummaryResponse> ScenarioSummaries { get; set; } = new();
    public List<EquipmentRecommendationComparisonScopeResponse> Scopes { get; set; } = new();
}