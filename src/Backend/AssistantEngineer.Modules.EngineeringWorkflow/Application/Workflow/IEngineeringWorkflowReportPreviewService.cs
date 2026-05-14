using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public interface IEngineeringWorkflowReportPreviewService
{
    EngineeringReportDocument BuildReportDocument(
        EngineeringWorkflowReportRequestDto request,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics);

    EngineeringWorkflowReportPreviewDto BuildReportPreview(EngineeringReportDocument report);
}
