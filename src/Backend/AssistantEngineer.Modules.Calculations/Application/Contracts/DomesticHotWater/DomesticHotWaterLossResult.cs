using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterLossResult(
    IReadOnlyList<double> StorageLossesProfileKWh,
    IReadOnlyList<double> DistributionLossesProfileKWh,
    IReadOnlyList<double> CirculationLossesProfileKWh,
    IReadOnlyList<double> RecoveredLossesProfileKWh,
    IReadOnlyList<double> AuxiliaryEnergyProfileKWh,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
