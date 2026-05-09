using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

public sealed record EngineeringReportDiagnostic(
    EngineeringReportDiagnosticSeverity Severity,
    string Code,
    string Message,
    CalculationTraceModuleKind Module,
    string? Context = null,
    string? Source = null,
    string? SuggestedCorrection = null);

