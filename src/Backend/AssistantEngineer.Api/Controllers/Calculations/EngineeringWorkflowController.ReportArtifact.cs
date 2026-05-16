using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AssistantEngineer.Api.Controllers.Calculations;

public sealed partial class EngineeringWorkflowController
{
    [HttpGet("scenarios/{scenarioId}/artifacts")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>> GetScenarioArtifacts(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        var authorizationDecision = await _authorizationGate.RequireArtifactReadPermissionAsync(
            projectId: scenario?.ProjectId,
            buildingId: scenario?.BuildingId,
            workflowId: scenarioId,
            artifactId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var artifacts = await _workflowPersistence.ListScenarioArtifactsAsync(scenarioId, cancellationToken);
        return Ok(artifacts);
    }

    [HttpGet("scenarios/{scenarioId}/artifacts/{artifactKind}")]
    public async Task<ActionResult<EngineeringCalculationArtifactRecordDto>> GetScenarioArtifactByKind(
        string scenarioId,
        string artifactKind,
        CancellationToken cancellationToken)
    {
        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        var authorizationDecision = await _authorizationGate.RequireArtifactReadPermissionAsync(
            projectId: scenario?.ProjectId,
            buildingId: scenario?.BuildingId,
            workflowId: scenarioId,
            artifactId: artifactKind,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (!Enum.TryParse<EngineeringCalculationArtifactKind>(artifactKind, true, out var parsedKind))
        {
            return BadRequest(new
            {
                scenarioId,
                artifactKind,
                code = "WORKFLOW_ARTIFACT_KIND_INVALID",
                message = "Artifact kind is invalid for workflow persistence endpoint."
            });
        }

        var artifact = await _workflowPersistence.GetScenarioArtifactAsync(
            scenarioId,
            parsedKind,
            cancellationToken);

        if (artifact is null)
        {
            return NotFound(new
            {
                scenarioId,
                artifactKind = parsedKind.ToString(),
                code = "WORKFLOW_ARTIFACT_NOT_FOUND",
                message = "Scenario artifact was not found in workflow persistence foundation store."
            });
        }

        return Ok(artifact);
    }

    [HttpPost("trace-preview")]
    public async Task<ActionResult<EngineeringWorkflowTracePreviewResponseDto>> TracePreview(
        [FromBody] EngineeringWorkflowTracePreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireReportReadPermissionAsync(
            projectId: request.State.ProjectId,
            buildingId: request.State.BuildingId,
            workflowId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var detailLevel = _tracePreviewService.ParseDetailLevel(request.DetailLevel);
        var diagnostics = _workflowDiagnostics.ValidateState(request.State);

        var trace = _tracePreviewService.BuildTraceDocument(request.State, detailLevel, diagnostics);
        var summary = _tracePreviewService.BuildTraceSummary(trace, request.DetailLevel);

        return Ok(new EngineeringWorkflowTracePreviewResponseDto(
            TraceDocument: trace,
            TraceSummary: summary,
            Diagnostics: diagnostics));
    }

    [HttpPost("report")]
    public async Task<ActionResult<EngineeringWorkflowReportResponseDto>> GenerateReport(
        [FromBody] EngineeringWorkflowReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireReportWritePermissionAsync(
            projectId: request.State.ProjectId,
            buildingId: request.State.BuildingId,
            workflowId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var diagnostics = _workflowDiagnostics.ValidateState(request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request, diagnostics);
        var preview = _reportPreviewService.BuildReportPreview(reportDocument);

        return Ok(new EngineeringWorkflowReportResponseDto(
            ReportDocument: reportDocument,
            Preview: preview,
            Diagnostics: diagnostics));
    }

    [EnableRateLimiting(ApiHardeningRegistration.EngineeringHeavyPolicyName)]
    [HttpPost("report/export/json")]
    public async Task<ActionResult<EngineeringWorkflowReportExportResponseDto>> ExportReportJson(
        [FromBody] EngineeringWorkflowReportExportRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireReportWritePermissionAsync(
            projectId: request.Request.State.ProjectId,
            buildingId: request.Request.State.BuildingId,
            workflowId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var diagnostics = _workflowDiagnostics.ValidateState(request.Request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request.Request, diagnostics);
        var content = _reportJsonExporter.Export(reportDocument, indented: true);

        return Ok(new EngineeringWorkflowReportExportResponseDto(
            Format: "Json",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics));
    }

    [EnableRateLimiting(ApiHardeningRegistration.EngineeringHeavyPolicyName)]
    [HttpPost("report/export/markdown")]
    public async Task<ActionResult<EngineeringWorkflowReportExportResponseDto>> ExportReportMarkdown(
        [FromBody] EngineeringWorkflowReportExportRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireReportWritePermissionAsync(
            projectId: request.Request.State.ProjectId,
            buildingId: request.Request.State.BuildingId,
            workflowId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var diagnostics = _workflowDiagnostics.ValidateState(request.Request.State);
        var reportDocument = _reportPreviewService.BuildReportDocument(request.Request, diagnostics);
        var content = _reportMarkdownExporter.Export(reportDocument);

        return Ok(new EngineeringWorkflowReportExportResponseDto(
            Format: "Markdown",
            Content: content,
            SchemaVersion: reportDocument.SchemaVersion,
            ReportId: reportDocument.ReportId,
            Diagnostics: diagnostics));
    }
}
