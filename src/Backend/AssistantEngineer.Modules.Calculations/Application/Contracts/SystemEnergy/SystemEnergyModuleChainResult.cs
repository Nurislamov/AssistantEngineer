using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyModuleChainResult(
    string CalculationId,
    IReadOnlyList<SystemEnergyEndUseChainResult> EndUses,
    IReadOnlyList<SystemEnergyAuxiliaryLoadInput> AuxiliaryLoads,
    IReadOnlyList<double> HourlyTotalUsefulEnergyKWh8760,
    IReadOnlyList<double> HourlyTotalSystemLoadBeforeGenerationKWh8760,
    IReadOnlyList<double> HourlyTotalRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyTotalNonRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyTotalAuxiliaryEnergyKWh8760,
    double AnnualTotalUsefulEnergyKWh,
    double AnnualTotalSystemLoadBeforeGenerationKWh,
    double AnnualTotalRecoverableLossKWh,
    double AnnualTotalNonRecoverableLossKWh,
    double AnnualTotalAuxiliaryEnergyKWh,
    IReadOnlyList<double> MonthlyTotalUsefulEnergyKWh,
    IReadOnlyList<double> MonthlyTotalSystemLoadBeforeGenerationKWh,
    SystemEnergyGenerationHandoff GenerationHandoff,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
