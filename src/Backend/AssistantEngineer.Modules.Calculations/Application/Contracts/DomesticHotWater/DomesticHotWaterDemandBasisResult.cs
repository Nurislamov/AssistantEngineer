using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDemandBasisResult(
    double DailyVolumeLiters,
    IReadOnlyList<double> CustomHourlyVolumeLiters8760,
    bool UsesCustomHourlyVolume,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<double>? ScheduledUsefulEnergyKWh = null,
    bool UsesScheduledUsefulEnergy = false);
