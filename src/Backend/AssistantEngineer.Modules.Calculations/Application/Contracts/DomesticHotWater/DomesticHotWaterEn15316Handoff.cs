using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterEn15316Handoff(
    string CalculationId,
    string EndUse,
    string UsefulEnergySource,
    double AnnualUsefulDhwEnergyKWh,
    double AnnualDhwSystemHeatRequirementKWh,
    double AnnualDhwAuxiliaryElectricityKWh,
    IReadOnlyList<double> HourlyUsefulDhwEnergyKWh8760,
    IReadOnlyList<double> HourlyDhwSystemHeatRequirementKWh8760,
    IReadOnlyList<double> HourlyDhwAuxiliaryElectricityKWh8760,
    IReadOnlyList<double> HourlyRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyNonRecoverableLossKWh8760,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
