namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationRequest
{
    public AutosizingGranularity Granularity { get; set; } = AutosizingGranularity.Zone;

    public string SystemType { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;

    public bool OccupiedHoursOnly { get; set; } = false;
    public double OccupancyThreshold { get; set; } = 0.05;

    public int CoolingSeasonStartMonth { get; set; } = 5;
    public int CoolingSeasonEndMonth { get; set; } = 9;

    public double CoolingSafetyFactor { get; set; } = 1.10;

    public int MaxUnitsPerScope { get; set; } = 6;
    public int TopRecommendationsPerScope { get; set; } = 3;
    public double MaxOversizeRatio { get; set; } = 0.35;

    public List<string> PreferredManufacturers { get; set; } = new();
    public List<string> PreferredModelKeywords { get; set; } = new();

    public List<string> ExcludedManufacturers { get; set; } = new();
    public List<string> ExcludedModelKeywords { get; set; } = new();

    public double OversizePenaltyWeight { get; set; } = 1.2;
    public double UnitCountPenaltyWeight { get; set; } = 0.8;

    public double PreferredManufacturerBonus { get; set; } = 10.0;
    public double PreferredModelKeywordBonus { get; set; } = 4.0;

    public double MinimumCatalogScore { get; set; } = 0.0;

    public double SizingWeight { get; set; } = 0.40;
    public double CapexWeight { get; set; } = 0.25;
    public double EfficiencyWeight { get; set; } = 0.20;
    public double ComplexityWeight { get; set; } = 0.15;

    public double CapexPerKwFactor { get; set; } = 1.0;
    public double CapexPerUnitFactor { get; set; } = 0.75;

    public List<string> PreferredEfficiencyKeywords { get; set; } = new();
    public double PreferredEfficiencyKeywordBonus { get; set; } = 8.0;
}