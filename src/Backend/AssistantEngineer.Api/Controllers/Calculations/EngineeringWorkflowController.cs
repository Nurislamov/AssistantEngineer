using AssistantEngineer.Api;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/engineering-workflow")]
public sealed partial class EngineeringWorkflowController : ControllerBase
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
    private readonly IWorkflowTenantScopedReadService _workflowTenantScopedReads;
    private readonly ITenantQueryContextFactory _tenantQueryContextFactory;
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _authorizationOptions;

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
        IProtectedEndpointAuthorizationGate authorizationGate,
        IWorkflowTenantScopedReadService workflowTenantScopedReads,
        ITenantQueryContextFactory tenantQueryContextFactory,
        IOptionsMonitor<ApiAuthorizationOptions> authorizationOptions)
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
        _workflowTenantScopedReads = workflowTenantScopedReads;
        _tenantQueryContextFactory = tenantQueryContextFactory;
        _authorizationOptions = authorizationOptions;
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

    private bool ShouldUseTenantScopedWorkflowReads()
    {
        var options = _authorizationOptions.CurrentValue;
        return options.RequiresProtectedWorkflowReadAuthorization();
    }

    private TenantQueryContext CreateWorkflowTenantContext(bool includeUnscopedResourcesInTenantLists)
    {
        var options = _authorizationOptions.CurrentValue;
        return _tenantQueryContextFactory.CreateCurrent(
            includeUnscopedResourcesInTenantLists: includeUnscopedResourcesInTenantLists,
            returnNotFoundForTenantMismatch: options.ShouldReturnNotFoundForWorkflowTenantMismatch());
    }
}
