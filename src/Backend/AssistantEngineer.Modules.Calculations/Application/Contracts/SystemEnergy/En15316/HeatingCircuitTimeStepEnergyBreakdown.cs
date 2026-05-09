namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record HeatingCircuitTimeStepEnergyBreakdown(
    string CircuitId,
    double UsefulHeatingEnergyKWh,
    double UsefulDhwEnergyKWh,
    double UsefulTotalEnergyKWh,
    double EmissionInputEnergyKWh,
    double EmissionOutputEnergyKWh,
    double EmissionLossEnergyKWh,
    double DistributionInputEnergyKWh,
    double DistributionOutputEnergyKWh,
    double DistributionLossEnergyKWh,
    double StorageInputEnergyKWh,
    double StorageOutputEnergyKWh,
    double StorageLossEnergyKWh,
    double GeneratorInputEnergyKWh,
    double GeneratorOutputEnergyKWh,
    double GeneratorLossEnergyKWh,
    double FinalEnergyKWh,
    double PrimaryEnergyKWh,
    double OverallUsefulToFinalEfficiency);
