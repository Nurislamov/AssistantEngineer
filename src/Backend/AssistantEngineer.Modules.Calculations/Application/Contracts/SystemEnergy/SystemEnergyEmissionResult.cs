using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEmissionResult(
    SystemEnergyCarrier Carrier,
    SystemEnergyEmissionFactorKind FactorKind,
    IReadOnlyList<double> HourlyEmissionsKg8760,
    IReadOnlyList<double> MonthlyEmissionsKg,
    double AnnualEmissionsKg,
    SystemEnergyEmissionFactor Factor,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
