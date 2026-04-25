namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016EnergyBalanceBreakdown(
    double SolarGainsKWh,
    double InternalGainsKWh,
    double HeatingInputKWh,
    double CoolingExtractedKWh);