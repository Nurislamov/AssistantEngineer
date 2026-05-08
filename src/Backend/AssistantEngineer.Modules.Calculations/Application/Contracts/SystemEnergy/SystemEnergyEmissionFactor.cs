using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyEmissionFactor(
    SystemEnergyCarrier Carrier,
    SystemEnergyEmissionFactorKind FactorKind,
    double KgPerKWh,
    SystemEnergyFactorSourceKind SourceKind,
    string? Source,
    string? Region,
    int? Year,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
