using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGenerationStageResult(
    SystemEnergyUseKind UseKind,
    IReadOnlyList<double> RequestedLoadProfileKWh,
    IReadOnlyList<double> DeliveredLoadProfileKWh,
    IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> FinalEnergyByCarrierKWh,
    IReadOnlyList<double> GenerationLossesProfileKWh,
    IReadOnlyList<double> AuxiliaryEnergyProfileKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings);
