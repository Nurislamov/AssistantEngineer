using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDemandDefinition(
    string DemandId,
    string? BuildingId,
    string? ZoneId,
    DomesticHotWaterBuildingUseKind UseKind,
    DomesticHotWaterDemandBasis DemandBasis,
    double? OccupantCount,
    double? FloorAreaSquareMeters,
    double? DwellingCount,
    double? FixtureCount,
    double? DailyVolumeLitersPerPerson,
    double? DailyVolumeLitersPerSquareMeter,
    double? DailyVolumeLitersPerDwelling,
    double ColdWaterTemperatureCelsius,
    double HotWaterSetpointTemperatureCelsius,
    double? ReferenceDrawOffTemperatureCelsius,
    double TimeStepHours,
    IReadOnlyList<double>? HourlySchedule,
    IReadOnlyList<double>? MonthlySchedule,
    IReadOnlyList<double>? ScheduledVolumeProfile,
    IReadOnlyList<double>? ScheduledUsefulEnergyProfileKWh,
    int? AnnualOperatingDays,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
