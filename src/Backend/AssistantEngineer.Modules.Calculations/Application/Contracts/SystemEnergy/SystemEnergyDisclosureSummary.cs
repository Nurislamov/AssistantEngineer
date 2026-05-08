using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyDisclosureSummary(
    SystemEnergyDisclosureStatus Status,
    IReadOnlyList<string> AllowedClaims,
    IReadOnlyList<string> ForbiddenClaims,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
