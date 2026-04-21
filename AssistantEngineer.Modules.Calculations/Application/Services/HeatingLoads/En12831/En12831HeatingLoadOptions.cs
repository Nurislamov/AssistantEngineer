namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;

public sealed class En12831HeatingLoadOptions
{
    public double DefaultAirChangesPerHour { get; init; } = 0.5;
    public double AirHeatCapacityWhPerM3K { get; init; } = 0.34;
}