namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyBuildingSimulationRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double OutdoorTemperatureC,
    double AverageIndoorTemperatureC,
    double SolarGainsW,
    double InternalGainsW,
    double TotalGainsW,
    double HeatingLoadW,
    double CoolingLoadW,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh);