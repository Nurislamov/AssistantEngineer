namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingEquipmentRecommendationComparisonReportResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public AutosizingGranularity Granularity { get; set; }

    public int ScenarioCount { get; set; }
    public int ScopeCount { get; set; }
    public int RowCount { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public List<EquipmentRecommendationScenarioSummaryResponse> ScenarioSummaries { get; set; } = new();
    public List<EquipmentRecommendationComparisonReportRowResponse> Rows { get; set; } = new();
}