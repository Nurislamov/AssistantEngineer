using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class SyntheticDesignDayRequest
{
    public ReferenceDesignDayMode Mode { get; set; } = ReferenceDesignDayMode.Cooling;

    public int DayOfYear { get; set; } = 196;

    public double DesignOutdoorDryBulbC { get; set; } = 35.0;
    public double OutdoorDailyRangeC { get; set; } = 10.0;
    public double WindSpeedMPerS { get; set; } = 2.5;
    public double SolarPeakWPerM2 { get; set; } = 700.0;

    public double CoolingSetpointC { get; set; } = 26.0;
    public double HeatingSetpointC { get; set; } = 20.0;

    public bool UseRoomSchedules { get; set; } = true;
    public bool IncludeInternalGains { get; set; } = true;
    public bool IncludeSolarGains { get; set; } = true;
    public bool UseNaturalVentilation { get; set; } = false;

    public double GroundBoundaryTemperatureC { get; set; } = 12.0;

    public double CoolingSafetyFactor { get; set; } = 1.10;
    public double HeatingSafetyFactor { get; set; } = 1.10;
}