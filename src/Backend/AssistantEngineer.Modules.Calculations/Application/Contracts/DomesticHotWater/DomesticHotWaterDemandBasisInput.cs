using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDemandBasisInput(
    DomesticHotWaterDemandBasis DemandBasis,
    DomesticHotWaterUseCategory UseCategory,
    double? OccupantCount,
    double? DwellingUnitCount,
    double? FloorAreaSquareMeters,
    double? DailyVolumeLitersPerPerson,
    double? DailyVolumeLitersPerDwellingUnit,
    double? DailyVolumeLitersPerSquareMeter,
    double? CustomDailyVolumeLiters,
    IReadOnlyList<DomesticHotWaterFixtureUseInput> FixtureUses,
    IReadOnlyList<double>? CustomHourlyVolumeLiters,
    IReadOnlyList<double>? CustomDailyProfileFractions,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics,
    IReadOnlyList<double>? CustomHourlyUsefulEnergyKWh = null);
