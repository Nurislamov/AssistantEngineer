namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record HeatingSystemTimeStepResult(
    int TimeStepIndex,
    int Month,
    double UsefulHeatingLoadKWh,
    double UsefulDhwLoadKWh,
    double UsefulTotalLoadKWh,
    IReadOnlyDictionary<string, HeatingCircuitTimeStepEnergyBreakdown> CircuitBreakdowns,
    double EmissionLossEnergyKWh,
    double DistributionLossEnergyKWh,
    double StorageLossEnergyKWh,
    double GeneratorLossEnergyKWh,
    IReadOnlyDictionary<string, double> FinalEnergyByCircuitKWh,
    IReadOnlyDictionary<string, double> PrimaryEnergyByCircuitKWh,
    double TotalFinalEnergyKWh,
    double TotalPrimaryEnergyKWh,
    double OverallUsefulToFinalEfficiency,
    IReadOnlyList<En15316SystemEnergyDiagnostics> Diagnostics);
