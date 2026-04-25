namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;

public sealed class BuildingComfortMetricsRequest
{
    public double OverheatingThresholdC { get; set; } = 26.0;
    public double SevereOverheatingThresholdC { get; set; } = 28.0;
    public double UnderheatingThresholdC { get; set; } = 20.0;

    public bool OccupiedHoursOnly { get; set; } = false;
    public double OccupancyThreshold { get; set; } = 0.05;

    public int CoolingSeasonStartMonth { get; set; } = 5;
    public int CoolingSeasonEndMonth { get; set; } = 9;
}