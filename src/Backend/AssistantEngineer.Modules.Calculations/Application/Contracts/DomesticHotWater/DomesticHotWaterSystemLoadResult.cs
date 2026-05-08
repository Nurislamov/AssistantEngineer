using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLoadResult(
    string CalculationId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    DomesticHotWaterUsefulDemandResult UsefulDemand,
    IReadOnlyList<DomesticHotWaterLossComponentResult> LossComponents,
    double AnnualUsefulEnergyKWh,
    double AnnualStorageLossKWh,
    double AnnualDistributionLossKWh,
    double AnnualCirculationLossKWh,
    double AnnualAuxiliaryElectricityKWh,
    double AnnualRecoverableLossKWh,
    double AnnualNonRecoverableLossKWh,
    double AnnualSystemHeatRequirementKWh,
    IReadOnlyList<double> MonthlySystemHeatRequirementKWh,
    IReadOnlyList<double> HourlySystemHeatRequirementKWh8760,
    IReadOnlyList<double> HourlyRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyNonRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyAuxiliaryElectricityKWh8760,
    DomesticHotWaterEn15316Handoff En15316Handoff,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
