using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEndUseFinalEnergyResult(
    SystemEnergyEndUse EndUse,
    IReadOnlyList<double> HourlySystemLoadBeforeGenerationKWh8760,
    IReadOnlyList<double> HourlySuppliedSystemLoadKWh8760,
    IReadOnlyList<double> HourlyUnmetSystemLoadKWh8760,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> HourlyFinalEnergyByCarrierKWh8760,
    IReadOnlyList<double> HourlyAuxiliaryElectricityKWh8760,
    double AnnualSystemLoadBeforeGenerationKWh,
    double AnnualSuppliedSystemLoadKWh,
    double AnnualUnmetSystemLoadKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, double> AnnualFinalEnergyByCarrierKWh,
    double AnnualAuxiliaryElectricityKWh,
    IReadOnlyList<double> MonthlySuppliedSystemLoadKWh,
    IReadOnlyList<double> MonthlyUnmetSystemLoadKWh,
    IReadOnlyDictionary<SystemEnergyCarrier, IReadOnlyList<double>> MonthlyFinalEnergyByCarrierKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
