namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingEquipmentRecommendationResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;

    public int Year { get; set; }
    public AutosizingGranularity Granularity { get; set; }

    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

    public double CoolingSafetyFactor { get; set; }

    public int MaxUnitsPerScope { get; set; }
    public int TopRecommendationsPerScope { get; set; }
    public double MaxOversizeRatio { get; set; }

    public double SizingWeight { get; set; }
    public double CapexWeight { get; set; }
    public double EfficiencyWeight { get; set; }
    public double ComplexityWeight { get; set; }

    public List<EquipmentRecommendationScopeResponse> Scopes { get; set; } = new();
}