namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2MonthlySummary(
    int Month,
    double HeatingEnergyKWh,
    double CoolingEnergyKWh,
    double TotalNodeHeatGainsKWh,
    double PeakHeatingLoadW,
    double PeakCoolingLoadW,
    double AverageAirTemperatureAfterHvacC);