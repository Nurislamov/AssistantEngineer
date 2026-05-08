using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEndUseChainResult(
    SystemEnergyEndUse EndUse,
    IReadOnlyList<SystemEnergyModuleResult> Modules,
    IReadOnlyList<double> HourlyUsefulEnergyKWh8760,
    IReadOnlyList<double> HourlySystemLoadBeforeGenerationKWh8760,
    IReadOnlyList<double> HourlyRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyNonRecoverableLossKWh8760,
    double AnnualUsefulEnergyKWh,
    double AnnualSystemLoadBeforeGenerationKWh,
    double AnnualRecoverableLossKWh,
    double AnnualNonRecoverableLossKWh,
    IReadOnlyList<double> MonthlyUsefulEnergyKWh,
    IReadOnlyList<double> MonthlySystemLoadBeforeGenerationKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
