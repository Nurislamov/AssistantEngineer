namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyMonthlyResult(
    int Month,
    double HeatingKWh,
    double CoolingKWh,
    double TotalKWh);
