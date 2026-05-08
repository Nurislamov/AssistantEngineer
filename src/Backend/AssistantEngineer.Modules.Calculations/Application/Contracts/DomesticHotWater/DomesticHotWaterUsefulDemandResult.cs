using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterUsefulDemandResult(
    string CalculationId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    DomesticHotWaterDemandBasis DemandBasis,
    DomesticHotWaterUseCategory UseCategory,
    double DailyVolumeLiters,
    double AnnualVolumeLiters,
    IReadOnlyList<double> MonthlyVolumeLiters,
    IReadOnlyList<double> HourlyVolumeLiters8760,
    double TemperatureRiseKelvin,
    double DailyUsefulEnergyKWh,
    double AnnualUsefulEnergyKWh,
    IReadOnlyList<double> MonthlyUsefulEnergyKWh,
    IReadOnlyList<double> HourlyUsefulEnergyKWh8760,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
