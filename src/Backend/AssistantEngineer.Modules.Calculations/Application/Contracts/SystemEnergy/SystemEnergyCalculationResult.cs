using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyCalculationResult(
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> UsefulEnergyByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> SystemLoadByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> EmissionLossesByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> DistributionLossesByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> StorageLossesByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> GenerationLossesByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> RecoveredLossesByUseKWh,
    IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> AuxiliaryEnergyByUseKWh,
    IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> FinalEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> PrimaryEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> Co2ByCarrierKg,
    IReadOnlyList<double> MonthlyFinalEnergyKWh,
    SystemEnergyAnnualSummary AnnualSummary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
