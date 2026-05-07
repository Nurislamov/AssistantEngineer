using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public sealed record StandardCalculationDiagnostic(
    CalculationDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Context = null,
    string? Source = null,
    StandardCalculationFamily? Family = null,
    StandardCalculationStage? Stage = null);
