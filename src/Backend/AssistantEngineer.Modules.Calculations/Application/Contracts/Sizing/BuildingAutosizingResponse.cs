namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingAutosizingResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;

    public int Year { get; set; }
    public ReferenceDesignDayMode Mode { get; set; }
    public AutosizingGranularity Granularity { get; set; }

    public List<double> CandidateNominalCapacitiesKw { get; set; } = new();

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

    public int HeatingSeasonStartMonth { get; set; }
    public int HeatingSeasonEndMonth { get; set; }

    public double CoolingSafetyFactor { get; set; }
    public double HeatingSafetyFactor { get; set; }

    public int MaxUnitsPerScope { get; set; }
    public int TopRecommendationsPerScope { get; set; }
    public double MaxOversizeRatio { get; set; }

    public List<AutosizingScopeResponse> Scopes { get; set; } = new();
}