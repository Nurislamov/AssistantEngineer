using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyModuleInput(
    string ModuleId,
    SystemEnergyModuleKind ModuleKind,
    SystemEnergyEndUse EndUse,
    SystemEnergyModuleCalculationMode CalculationMode,
    double? LossFraction,
    double? Efficiency,
    double? FixedAnnualLossKWh,
    IReadOnlyList<double>? HourlyLossProfileKWh8760,
    IReadOnlyList<double>? MonthlyLossProfileKWh,
    double? RecoverableFraction,
    SystemEnergyRecoveryMode RecoveryMode,
    SystemEnergyCarrier? Carrier,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
