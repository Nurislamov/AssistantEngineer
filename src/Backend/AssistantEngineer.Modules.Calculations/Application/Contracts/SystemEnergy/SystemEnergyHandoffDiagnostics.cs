using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyHandoffDiagnostics(
    CalculationDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null);
