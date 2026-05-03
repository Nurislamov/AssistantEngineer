namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixMonthlySummary(
    int Month,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh,
    double TotalNodeHeatGainsKWh,
    double PeakHeatingLoadW,
    double PeakCoolingLoadW,
    double AverageAirTemperatureAfterHvacC);