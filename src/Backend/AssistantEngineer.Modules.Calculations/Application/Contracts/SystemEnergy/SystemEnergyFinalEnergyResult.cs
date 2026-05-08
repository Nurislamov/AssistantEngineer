using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyFinalEnergyResult(
    string CalculationId,
    IReadOnlyList<SystemEnergyGeneratorResult> Generators,
    IReadOnlyList<SystemEnergyEndUseFinalEnergyResult> EndUses,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> HourlyFinalEnergyByCarrierKWh8760,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualFinalEnergyByCarrierKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> MonthlyFinalEnergyByCarrierKWh,
    IReadOnlyList<double> HourlyTotalFinalEnergyKWh8760,
    IReadOnlyList<double> HourlyTotalAuxiliaryElectricityKWh8760,
    double AnnualTotalFinalEnergyKWh,
    double AnnualTotalAuxiliaryElectricityKWh,
    double AnnualTotalUnmetSystemLoadKWh,
    SystemEnergyFinalEnergyStatus Status,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
