using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyLoadIntakeResult(
    SystemEnergyUsefulLoadSet UsefulLoadSet,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
