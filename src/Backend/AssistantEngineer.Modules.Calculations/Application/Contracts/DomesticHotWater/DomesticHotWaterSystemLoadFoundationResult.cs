using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLoadFoundationResult(
    IReadOnlyList<double> UsefulEnergyProfileKWh,
    IReadOnlyList<double> StorageLossesProfileKWh,
    IReadOnlyList<double> DistributionLossesProfileKWh,
    IReadOnlyList<double> CirculationLossesProfileKWh,
    IReadOnlyList<double> RecoveredLossesProfileKWh,
    IReadOnlyList<double> AuxiliaryEnergyProfileKWh,
    IReadOnlyList<double> SystemLoadProfileKWh,
    IReadOnlyList<double> MonthlySystemLoadKWh,
    DomesticHotWaterSystemLoadAnnualSummary AnnualSummary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
