using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDrawOffProfileResult(
    IReadOnlyList<double> VolumeProfileLiters,
    IReadOnlyList<double> UsefulEnergyProfileKWh,
    double TotalVolumeLiters,
    double TotalUsefulEnergyKWh,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
