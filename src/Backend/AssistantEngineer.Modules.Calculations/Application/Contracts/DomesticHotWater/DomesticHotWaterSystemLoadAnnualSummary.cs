namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLoadAnnualSummary(
    double UsefulEnergyKWh,
    double StorageLossesKWh,
    double DistributionLossesKWh,
    double CirculationLossesKWh,
    double RecoveredLossesKWh,
    double AuxiliaryEnergyKWh,
    double SystemLoadKWh);
