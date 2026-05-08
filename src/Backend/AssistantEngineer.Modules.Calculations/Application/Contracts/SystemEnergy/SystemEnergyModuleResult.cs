using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyModuleResult(
    string ModuleId,
    SystemEnergyModuleKind ModuleKind,
    SystemEnergyEndUse EndUse,
    SystemEnergyModuleCalculationMode CalculationMode,
    IReadOnlyList<double> HourlyInputEnergyKWh8760,
    IReadOnlyList<double> HourlyOutputEnergyKWh8760,
    IReadOnlyList<double> HourlyLossEnergyKWh8760,
    IReadOnlyList<double> HourlyRecoverableLossKWh8760,
    IReadOnlyList<double> HourlyNonRecoverableLossKWh8760,
    double AnnualInputEnergyKWh,
    double AnnualOutputEnergyKWh,
    double AnnualLossEnergyKWh,
    double AnnualRecoverableLossKWh,
    double AnnualNonRecoverableLossKWh,
    IReadOnlyList<double> MonthlyInputEnergyKWh,
    IReadOnlyList<double> MonthlyOutputEnergyKWh,
    IReadOnlyList<double> MonthlyLossEnergyKWh,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
