namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;

public sealed class BuildingZoneComfortMetricsResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public double OverheatingThresholdC { get; set; }
    public double SevereOverheatingThresholdC { get; set; }
    public double UnderheatingThresholdC { get; set; }

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

    public List<ZoneComfortMetricsResponse> Zones { get; set; } = new();
}