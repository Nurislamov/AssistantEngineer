namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class NaturalVentilationOptions
{
    public bool Enabled { get; init; } = true;
    public bool UseIso16798InspiredCalculator { get; init; } = false;

    public double MinimumOutdoorTemperatureC { get; init; } = 16.0;
    public double MaximumOutdoorTemperatureC { get; init; } = 28.0;
    public double MinimumDemandFactor { get; init; } = 0.05;

    public double OperableWindowAreaFraction { get; init; } = 0.25;
    public double OpeningDischargeCoefficient { get; init; } = 0.60;
    public double MaximumAirChangesPerHour { get; init; } = 10.0;

    public double IndoorTemperatureThresholdC { get; init; } = 23.0;
    public double MinimumIndoorOutdoorDeltaC { get; init; } = 1.0;
    public double MaximumWindSpeedForOpeningMPerS { get; init; } = 12.0;

    public bool EnableNightCooling { get; init; } = true;
    public int NightCoolingStartHour { get; init; } = 22;
    public int NightCoolingEndHour { get; init; } = 6;
    public double NightCoolingIndoorTemperatureThresholdC { get; init; } = 24.0;
    public double MinimumNightOpeningFactor { get; init; } = 0.7;
}
