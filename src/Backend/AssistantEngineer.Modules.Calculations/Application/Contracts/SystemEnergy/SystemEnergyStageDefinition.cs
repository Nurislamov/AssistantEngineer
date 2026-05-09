using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyStageDefinition(
    string StageId,
    SystemEnergySubsystemKind SubsystemKind,
    SystemEnergyUseKind AppliesToUse,
    double? Efficiency,
    double? LossFraction,
    IReadOnlyList<double>? FixedLossProfile,
    IReadOnlyList<double>? AuxiliaryEnergyProfile,
    double? RecoveredLossFraction,
    SystemEnergyCarrierKind TargetCarrier,
    SystemEnergyModuleCalculationMode CalculationMode,
    bool VerboseDiagnostics,
    int Priority = 0,
    string? Source = null,
    IReadOnlyList<StandardCalculationDiagnostic>? Diagnostics = null);
