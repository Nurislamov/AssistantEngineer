namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyEnergyNeed(
    int HourOfYear,
    int Month,
    double HeatingLoadW,
    double CoolingLoadW,
    double OperativeTemperatureC,
    double OutdoorTemperatureC,
    double InternalGainsW,
    double SolarGainsW);