using AssistantEngineer.Api;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/engineering-workflow")]
public sealed class EngineeringWorkflowController : ControllerBase
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private const int WorkflowListMaxPageSize = 200;

    private readonly IEngineeringWorkflowStateBuilder _stateBuilder;
    private readonly IEngineeringWorkflowDiagnosticsService _workflowDiagnostics;
    private readonly IEngineeringWorkflowTracePreviewService _tracePreviewService;
    private readonly IEngineeringWorkflowReportPreviewService _reportPreviewService;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;
    private readonly IEngineeringCalculationScenarioRunner _scenarioRunner;
    private readonly IEngineeringCalculationJobService _jobService;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;
    private readonly IEngineeringWorkflowSubmissionService _workflowSubmissionService;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;

    public EngineeringWorkflowController(
        IEngineeringWorkflowStateBuilder stateBuilder,
        IEngineeringWorkflowDiagnosticsService workflowDiagnostics,
        IEngineeringWorkflowTracePreviewService tracePreviewService,
        IEngineeringWorkflowReportPreviewService reportPreviewService,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter,
        IEngineeringCalculationScenarioRunner scenarioRunner,
        IEngineeringCalculationJobService jobService,
        IEngineeringWorkflowPersistenceService workflowPersistence,
        IEngineeringWorkflowSubmissionService workflowSubmissionService,
        IProtectedEndpointAuthorizationGate authorizationGate)
    {
        _stateBuilder = stateBuilder;
        _workflowDiagnostics = workflowDiagnostics;
        _tracePreviewService = tracePreviewService;
        _reportPreviewService = reportPreviewService;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
        _scenarioRunner = scenarioRunner;
        _jobService = jobService;
        _workflowPersistence = workflowPersistence;
        _workflowSubmissionService = workflowSubmissionService;
        _authorizationGate = authorizationGate;
    }

    [HttpGet("{projectId:int}/state")]
    public async Task<ActionResult<EngineeringWorkflowStateDto>> GetWorkflowState(
        int projectId,
        [FromQuery] int? buildingId,
        CancellationToken cancellationToken)
    {
        var persistedState = await _workflowPersistence.GetLatestWorkflowStateAsync(
            projectId,
            buildingId,
            cancellationToken);

        if (persistedState is not null)
        {
            return Ok(persistedState);
        }

        EngineeringWorkflowStateDto state;

        try
        {
            state = await _stateBuilder.BuildWorkflowStateAsync(projectId, buildingId, cancellationToken);
            state = _workflowDiagnostics.AddMissingPersistedStateDiagnostic(state, _workflowPersistence.GetProviderInfo());
            await _workflowPersistence.SaveWorkflowStateAsync(state, state.Diagnostics, cancellationToken);
        }
        catch (Exception exception)
        {
            state = _stateBuilder.BuildInfrastructureFallbackState(
                projectId,
                buildingId,
                $"Workflow persistence source is unavailable: {exception.Message}");
        }

        return Ok(state);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<EngineeringWorkflowValidationResponseDto>> Validate(
        [FromBody] EngineeringWorkflowValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var diagnostics = _workflowDiagnostics.ValidateState(request.State);
        var steps = _workflowDiagnostics.BuildStepStatuses(request.State, diagnostics);
        var stateToPersist = request.State with
        {
            Diagnostics = diagnostics,
            Steps = steps
        };

        await _workflowPersistence.SaveWorkflowStateAsync(stateToPersist, diagnostics, cancellationToken);

        return Ok(new EngineeringWorkflowValidationResponseDto(
            IsValid: diagnostics.All(diagnostic => !_workflowDiagnostics.IsErrorSeverity(diagnostic.Severity)),
            Diagnostics: diagnostics,
            Steps: steps));
    }

    [HttpPost("prepare-calculation")]
    public async Task<ActionResult<EngineeringWorkflowCalculationPreparationResponseDto>> PrepareCalculation(
        [FromBody] EngineeringWorkflowCalculationPreparationRequestDto request,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: null,
            projectId: request.State.ProjectId,
            buildingId: request.State.BuildingId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var scenarioRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: $"wf-prep-{request.State.ProjectId}-{request.State.BuildingId?.ToString() ?? "none"}",
            ProjectId: request.State.ProjectId,
            BuildingId: request.State.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.PrepareOnly,
            State: request.State,
            RequestedModules: request.State.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: false,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var scenarioResult = await _scenarioRunner.RunAsync(scenarioRequest, cancellationToken);
        var persistedScenario = await _workflowPersistence.SavePreparedScenarioAsync(
            scenarioRequest,
            scenarioResult,
            cancellationToken);

        var preview = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["projectId"] = request.State.ProjectId.ToString(),
            ["projectName"] = request.State.ProjectName,
            ["buildingId"] = request.State.BuildingId?.ToString() ?? "n/a",
            ["currentStep"] = request.State.CurrentStep,
            ["zonesCount"] = request.State.Zones.Count.ToString(),
            ["boundariesCount"] = request.State.Boundaries.Count.ToString(),
            ["diagnosticsCount"] = scenarioResult.ValidationDiagnostics.Count.ToString(),
            ["availableModulesCount"] = request.State.AvailableModules.Count.ToString(),
            ["scenarioStatus"] = scenarioResult.Status.ToString(),
            ["scenarioId"] = persistedScenario.ScenarioId
        };

        var status = scenarioResult.Status is EngineeringCalculationExecutionStatus.FailedValidation or EngineeringCalculationExecutionStatus.FailedExecution
            ? "blocked"
            : "prepared";

        var providerInfo = _workflowPersistence.GetProviderInfo();
        var metadata = scenarioResult.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        metadata["persistence"] = providerInfo.ProviderLabel;
        metadata["persistenceProvider"] = providerInfo.Provider.ToString();
        metadata["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false";

        var response = new EngineeringWorkflowCalculationPreparationResponseDto(
            RequestId: persistedScenario.ScenarioId,
            Status: status,
            Executed: false,
            RequestPreview: preview,
            Assumptions: scenarioResult.Assumptions,
            Diagnostics: scenarioResult.ValidationDiagnostics,
            Metadata: metadata);

        return Ok(response);
    }

    [RequestTimeout(RequestPolicies.LongRunning)]
    [EnableRateLimiting(ApiHardeningRegistration.EngineeringHeavyPolicyName)]
    [HttpPost("run-calculation")]
    public async Task<ActionResult<EngineeringCalculationScenarioResultDto>> RunCalculation(
        [FromBody] EngineeringCalculationScenarioRequestDto request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: request.ScenarioId,
            projectId: request.ProjectId,
            buildingId: request.BuildingId,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var outcome = await _workflowSubmissionService.RunCalculationAsync(request, idempotencyKey, cancellationToken);
        if (outcome.IsConflict)
        {
            return Conflict(new
            {
                code = outcome.ConflictCode ?? "ENGINEERING_IDEMPOTENCY_CONFLICT",
                message = outcome.ConflictMessage ?? "Idempotency conflict for engineering workflow submission."
            });
        }

        return Ok(outcome.Payload);
    }

    [RequestTimeout(RequestPolicies.LongRunning)]
    [EnableRateLimiting(ApiHardeningRegistration.EngineeringHeavyPolicyName)]
    [HttpPost("jobs")]
    public async Task<ActionResult<EngineeringCalculationJobResultDto>> CreateOrRunJob(
        [FromBody] EngineeringCalculationJobRequestDto request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: request.JobId,
            projectId: request.ProjectId,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        if (request.ProjectId < 0)
        {
            return BadRequest(new
            {
                code = "CALCULATION_JOB_PROJECT_INVALID",
                message = "Project id must be zero or greater for engineering workflow jobs."
            });
        }

        var outcome = await _workflowSubmissionService.CreateOrRunJobAsync(request, idempotencyKey, cancellationToken);
        if (outcome.IsConflict)
        {
            return Conflict(new
            {
                code = outcome.ConflictCode ?? "ENGINEERING_IDEMPOTENCY_CONFLICT",
                message = outcome.ConflictMessage ?? "Idempotency conflict for engineering workflow job submission."
            });
        }
        return Ok(outcome.Payload);
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<EngineeringCalculationJobResultDto>> GetJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (result is null)
        {
            return NotFound(new
            {
                jobId,
                code = "CALCULATION_JOB_NOT_FOUND",
                message = "Calculation job was not found in workflow persistence store."
            });
        }

        return Ok(result);
    }

    [HttpGet("jobs/{jobId}/events")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationJobEventDto>>> GetJobEvents(
        string jobId,
        CancellationToken cancellationToken)
    {
        var events = await _jobService.ListJobEventsAsync(jobId, cancellationToken);
        return Ok(events);
    }

    [HttpPost("jobs/{jobId}/cancel")]
    public async Task<ActionResult<EngineeringCalculationJobResultDto>> CancelJob(
        string jobId,
        CancellationToken cancellationToken)
    {
        var authorizationDecision = await _authorizationGate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: jobId,
            projectId: null,
            buildingId: null,
            cancellationToken);
        if (!authorizationDecision.IsAllowed)
        {
            return ToAuthorizationActionResult(authorizationDecision);
        }

        var result = await _jobService.CancelJobAsync(jobId, cancellationToken);
        if (result is null)
        {
            return NotFound(new
            {
                jobId,
                code = "CALCULATION_JOB_NOT_FOUND",
                message = "Calculation job was not found in workflow persistence store."
            });
        }

        return Ok(result);
    }

    [HttpGet("{projectId:int}/jobs")]
    public async Task<ActionResult<PagedResponse<EngineeringCalculationJobResultDto>>> ListProjectJobs(
        int projectId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var jobs = await _jobService.ListProjectJobsAsync(projectId, cancellationToken);
        return Ok(jobs
            .ApplyProjectListQuery(query)
            .ToPagedResponse(NormalizeWorkflowListQuery(query)));
    }

    [HttpGet("scenarios/{scenarioId}")]
    public async Task<ActionResult<EngineeringCalculationScenarioRecordDto>> GetScenarioResult(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        if (scenario is null)
        {
            return NotFound(new
            {
                scenarioId,
                code = "WORKFLOW_SCENARIO_NOT_FOUND",
                message = "Scenario record was not found in workflow persistence foundation store."
            });
        }

        return Ok(scenario);
    }

    [HttpGet("{projectId:int}/scenarios")]
    public async Task<ActionResult<PagedResponse<EngineeringCalculationScenarioRecordDto>>> GetProjectScenarios(
        int projectId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var scenarios = await _workflowPersistence.ListProjectScenariosAsync(projectId, cancellationToken);
        return Ok(scenarios
            .ApplyProjectListQuery(query)
            .ToPagedResponse(NormalizeWorkflowListQuery(query)));
    }

    [HttpGet("scenarios/{scenarioId}/artifacts")]
    public async Task<ActionResult<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>> GetScenarioArtifacts(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var artifacts = await _workflowPersistence.ListScenarioArtifactsAsync(scenarioId, cancellationToken);
        return Ok(artifacts);
    }

    [HttpGet("scenarios/{scenarioId}/artifacts/{artifactKind}")]
    public async Task<ActionResult<EngineeringCalculationArtifactRecordDto>> GetScenarioArtifactByKind(
        string scenarioId,
        string artifactKind,
        CancellationToken cancellationToken)
    {
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
    public ActionResult<EngineeringWorkflowTracePreviewResponseDto> TracePreview(
        [FromBody] EngineeringWorkflowTracePreviewRequestDto request)
    {
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
    public ActionResult<EngineeringWorkflowReportResponseDto> GenerateReport(
        [FromBody] EngineeringWorkflowReportRequestDto request)
    {
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
    public ActionResult<EngineeringWorkflowReportExportResponseDto> ExportReportJson(
        [FromBody] EngineeringWorkflowReportExportRequestDto request)
    {
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
    public ActionResult<EngineeringWorkflowReportExportResponseDto> ExportReportMarkdown(
        [FromBody] EngineeringWorkflowReportExportRequestDto request)
    {
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

    private static CollectionQueryParameters NormalizeWorkflowListQuery(CollectionQueryParameters query)
    {
        var page = query.GetPage();
        var pageSize = Math.Min(query.GetPageSize(), WorkflowListMaxPageSize);

        return new CollectionQueryParameters
        {
            Search = query.Search,
            SortBy = query.SortBy,
            SortDescending = query.SortDescending,
            Page = page,
            PageSize = pageSize
        };
    }

    private ActionResult ToAuthorizationActionResult(ProtectedEndpointAuthorizationDecision decision) =>
        decision.Outcome switch
        {
            ProtectedEndpointAuthorizationOutcome.Unauthorized => Unauthorized(),
            ProtectedEndpointAuthorizationOutcome.Forbidden => Forbid(),
            ProtectedEndpointAuthorizationOutcome.NotFound => NotFound(),
            _ => Ok()
        };
}
