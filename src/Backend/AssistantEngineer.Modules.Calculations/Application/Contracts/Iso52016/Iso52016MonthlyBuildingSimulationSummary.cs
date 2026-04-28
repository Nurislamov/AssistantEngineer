namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016MonthlyBuildingSimulationSummary(
    int Month,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh,
    double SolarGainsKWh,
    double InternalGainsKWh,
    double TotalGainsKWh,
    double PeakHeatingLoadW,
    double PeakCoolingLoadW,
    double AverageIndoorTemperatureC,
    double AverageOutdoorTemperatureC);