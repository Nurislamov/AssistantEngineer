using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

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
