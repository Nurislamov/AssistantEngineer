namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;

public sealed class CoolingLoadCalculationOptions
{
    public double DefaultCoolingSafetyFactor { get; init; } = 1.10;
    public double SimplifiedVolumeLoadWPerM3 { get; init; } = 35.0;
    public double SimplifiedInternalWallLoadWPerM2 { get; init; } = 30.0;
    public double SimplifiedNorthExternalWallLoadWPerM2 { get; init; } = 30.0;
    public double SimplifiedExternalWallLoadWPerM2 { get; init; } = 60.0;
    public double DefaultOutdoorCoolingDesignTemperatureC { get; init; } = 35.0;
}
