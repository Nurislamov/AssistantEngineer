using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorResult(
    string GeneratorId,
    string? Name,
    SystemEnergyGeneratorKind GeneratorKind,
    SystemEnergyGeneratorCalculationMode CalculationMode,
    SystemEnergyCarrier FinalEnergyCarrier,
    IReadOnlyList<SystemEnergyEndUse> ServedEndUses,
    IReadOnlyList<SystemEnergyHourlyGeneratorDispatchResult> HourlyDispatch,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlySuppliedSystemLoadByEndUseKWh8760,
    IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyList<double>> HourlyFinalEnergyByEndUseKWh8760,
    IReadOnlyList<double> HourlyTotalFinalEnergyKWh8760,
    IReadOnlyList<double> HourlyTotalAuxiliaryElectricityKWh8760,
    double AnnualSuppliedSystemLoadKWh,
    double AnnualFinalEnergyKWh,
    double AnnualAuxiliaryElectricityKWh,
    IReadOnlyList<double> MonthlyFinalEnergyKWh,
    IReadOnlyList<double> MonthlyAuxiliaryElectricityKWh,
    SystemEnergyFinalEnergyStatus Status,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
