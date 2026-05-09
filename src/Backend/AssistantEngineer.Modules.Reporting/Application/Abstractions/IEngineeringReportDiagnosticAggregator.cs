using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Abstractions;

public interface IEngineeringReportDiagnosticAggregator
{
    EngineeringReportDiagnostic FromCalculationDiagnostic(
        CalculationDiagnostic diagnostic,
        CalculationTraceModuleKind module = CalculationTraceModuleKind.Validation,
        string? source = null);

    EngineeringReportDiagnostic FromStandardDiagnostic(
        StandardCalculationDiagnostic diagnostic,
        CalculationTraceModuleKind defaultModule = CalculationTraceModuleKind.Validation,
        string? source = null);

    EngineeringReportDiagnostic FromTraceDiagnostic(
        CalculationTraceDiagnostic diagnostic);

    IReadOnlyList<EngineeringReportDiagnostic> Aggregate(
        IEnumerable<EngineeringReportDiagnostic> diagnostics);
}

