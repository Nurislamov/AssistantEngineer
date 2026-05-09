namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyAnnualSummary(
    double UsefulEnergyKWh,
    double SystemLoadKWh,
    double EmissionLossesKWh,
    double DistributionLossesKWh,
    double StorageLossesKWh,
    double GenerationLossesKWh,
    double RecoveredLossesKWh,
    double AuxiliaryEnergyKWh,
    double FinalEnergyKWh,
    double PrimaryEnergyKWh,
    double Co2Kg);
