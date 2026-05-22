using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Composition;

public interface IEngineeringWorkflowControllerActionService
{
    Task<EngineeringWorkflowValidationResponseDto> ValidateAsync(
        EngineeringWorkflowValidationRequestDto request,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowCalculationPreparationResponseDto> PrepareCalculationAsync(
        EngineeringWorkflowCalculationPreparationRequestDto request,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowStateDto> BuildOrLoadWorkflowStateAsync(
        int projectId,
        int? buildingId,
        CancellationToken cancellationToken);

    EngineeringWorkflowTracePreviewResponseDto BuildTracePreview(
        EngineeringWorkflowTracePreviewRequestDto request);

    EngineeringWorkflowReportResponseDto BuildReport(
        EngineeringWorkflowReportRequestDto request);

    EngineeringWorkflowReportExportResponseDto BuildJsonExport(
        EngineeringWorkflowReportExportRequestDto request);

    EngineeringWorkflowReportExportResponseDto BuildMarkdownExport(
        EngineeringWorkflowReportExportRequestDto request);
}
