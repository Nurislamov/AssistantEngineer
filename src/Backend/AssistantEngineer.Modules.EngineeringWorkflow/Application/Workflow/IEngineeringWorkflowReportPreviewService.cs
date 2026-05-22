using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public interface IEngineeringWorkflowReportPreviewService
{
    EngineeringReportDocument BuildReportDocument(
        EngineeringWorkflowReportRequestDto request,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    EngineeringWorkflowReportPreviewDto BuildReportPreview(EngineeringReportDocument report);
}
