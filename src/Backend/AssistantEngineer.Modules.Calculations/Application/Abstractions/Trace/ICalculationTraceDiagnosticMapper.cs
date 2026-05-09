using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface ICalculationTraceDiagnosticMapper
{
    CalculationTraceDiagnostic Map(
        CalculationDiagnostic diagnostic,
        CalculationTraceModuleKind moduleKind);

    CalculationTraceDiagnostic Map(
        StandardCalculationDiagnostic diagnostic,
        CalculationTraceModuleKind fallbackModuleKind);

    CalculationTraceDiagnostic Map(
        string code,
        string message,
        CalculationTraceSeverity severity,
        CalculationTraceModuleKind moduleKind,
        string? context = null,
        string? source = null);

    CalculationTraceDiagnostic MapWarning(
        string warning,
        CalculationTraceModuleKind moduleKind,
        string? code = null);
}
