using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyStageCalculationResult(
    SystemEnergySubsystemKind SubsystemKind,
    SystemEnergyUseKind UseKind,
    IReadOnlyList<double> InputProfileKWh,
    IReadOnlyList<double> OutputProfileKWh,
    IReadOnlyList<double> LossesProfileKWh,
    IReadOnlyList<double> RecoveredLossesProfileKWh,
    IReadOnlyList<double> AuxiliaryEnergyProfileKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings);
