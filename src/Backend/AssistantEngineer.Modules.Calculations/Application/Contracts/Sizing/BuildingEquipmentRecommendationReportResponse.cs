namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingEquipmentRecommendationReportResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public string ScenarioName { get; set; } = string.Empty;

    public AutosizingGranularity Granularity { get; set; }
    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    public int ScopeCount { get; set; }
    public int RowCount { get; set; }

    public double AverageRequiredCapacityKw { get; set; }
    public double AverageTopCompositeScore { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public List<EquipmentRecommendationReportRowResponse> Rows { get; set; } = new();
}