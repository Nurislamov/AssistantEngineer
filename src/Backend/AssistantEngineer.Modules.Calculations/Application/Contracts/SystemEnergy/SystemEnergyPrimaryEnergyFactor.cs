using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyPrimaryEnergyFactor(
    SystemEnergyCarrier Carrier,
    double RenewableFactor,
    double NonRenewableFactor,
    double TotalFactor,
    SystemEnergyFactorSourceKind SourceKind,
    string? Source,
    string? Region,
    int? Year,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
