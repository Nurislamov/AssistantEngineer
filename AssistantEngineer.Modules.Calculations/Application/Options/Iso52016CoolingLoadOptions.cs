namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso52016CoolingLoadOptions
{
    public int DefaultDesignMonth { get; init; } = 7;
    public double DefaultThermalMassWhPerM2K { get; init; } = 45.0;
    public double DefaultVentilationAirChangesPerHour { get; init; } = 0.5;
    public double AirHeatCapacityWhPerM3K { get; init; } = 0.34;
    public double DefaultSolarUtilizationFactor { get; init; } = 0.75;
    public double DefaultCoolingSafetyFactor { get; init; } = 1.10;
}
