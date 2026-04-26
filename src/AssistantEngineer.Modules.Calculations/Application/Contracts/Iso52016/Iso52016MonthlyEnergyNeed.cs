namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016MonthlyEnergyNeed(
    int Month,
    double HeatingDemandKWh,
    double CoolingDemandKWh);