using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public interface IEngineeringWorkflowTracePreviewService
{
    CalculationTraceDetailLevel ParseDetailLevel(string? detailLevel);

    CalculationTraceDocument BuildTraceDocument(
        EngineeringWorkflowStateDto state,
        CalculationTraceDetailLevel detailLevel,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    EngineeringWorkflowTraceSummaryDto BuildTraceSummary(
        CalculationTraceDocument trace,
        string detailLevel);
}
