namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class ReferenceDesignDayRequest
{
    public ReferenceDesignDayMode Mode { get; set; } = ReferenceDesignDayMode.Cooling;

    public bool OccupiedHoursOnly { get; set; } = false;
    public double OccupancyThreshold { get; set; } = 0.05;

    public int CoolingSeasonStartMonth { get; set; } = 5;
    public int CoolingSeasonEndMonth { get; set; } = 9;

    public int HeatingSeasonStartMonth { get; set; } = 11;
    public int HeatingSeasonEndMonth { get; set; } = 3;

    public double CoolingSafetyFactor { get; set; } = 1.10;
    public double HeatingSafetyFactor { get; set; } = 1.10;
}