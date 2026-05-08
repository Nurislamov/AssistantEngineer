using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterLossComponentResult(
    DomesticHotWaterLossComponentKind ComponentKind,
    double AnnualLossKWh,
    IReadOnlyList<double> MonthlyLossKWh,
    IReadOnlyList<double> HourlyLossKWh8760,
    double AnnualRecoverableLossKWh,
    double AnnualNonRecoverableLossKWh,
    IReadOnlyList<double> HourlyRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyNonRecoverableLossKWh8760,
    DomesticHotWaterLossRecoveryMode RecoveryMode,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
