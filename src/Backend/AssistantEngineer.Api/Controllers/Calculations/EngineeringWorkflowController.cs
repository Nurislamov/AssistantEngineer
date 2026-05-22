using AssistantEngineer.Api;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Querying.Projects;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using AssistantEngineer.Api.Services.Calculations.Composition;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
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

    private readonly IEngineeringWorkflowControllerActionService _actionService;
    private readonly IEngineeringWorkflowStateBuilder _stateBuilder;
    private readonly IEngineeringWorkflowDiagnosticsService _workflowDiagnostics;
    private readonly IEngineeringWorkflowTracePreviewService _tracePreviewService;
    private readonly IEngineeringWorkflowReportPreviewService _reportPreviewService;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;
    private readonly IEngineeringCalculationJobService _jobService;
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;
    private readonly IEngineeringWorkflowSubmissionService _workflowSubmissionService;
    private readonly IProtectedEndpointAuthorizationGate _authorizationGate;
    private readonly IWorkflowTenantScopedReadService _workflowTenantScopedReads;
    private readonly ITenantQueryContextFactory _tenantQueryContextFactory;
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _authorizationOptions;

    public EngineeringWorkflowController(
        IEngineeringWorkflowControllerActionService actionService,
        IEngineeringWorkflowStateBuilder stateBuilder,
        IEngineeringWorkflowDiagnosticsService workflowDiagnostics,
        IEngineeringWorkflowTracePreviewService tracePreviewService,
        IEngineeringWorkflowReportPreviewService reportPreviewService,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter,
        IEngineeringCalculationJobService jobService,
        IEngineeringWorkflowPersistenceService workflowPersistence,
        IEngineeringWorkflowSubmissionService workflowSubmissionService,
        IProtectedEndpointAuthorizationGate authorizationGate,
        IWorkflowTenantScopedReadService workflowTenantScopedReads,
        ITenantQueryContextFactory tenantQueryContextFactory,
        IOptionsMonitor<ApiAuthorizationOptions> authorizationOptions)
    {
        _actionService = actionService;
        _stateBuilder = stateBuilder;
        _workflowDiagnostics = workflowDiagnostics;
        _tracePreviewService = tracePreviewService;
        _reportPreviewService = reportPreviewService;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
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
        var response = await _actionService.ValidateAsync(request, cancellationToken);
        return Ok(response);
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

        var response = await _actionService.PrepareCalculationAsync(request, cancellationToken);
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

    internal static CollectionQueryParameters NormalizeWorkflowListQuery(CollectionQueryParameters query)
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

    private static AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo MapWorkflowPersistenceProviderInfo(
        EngineeringWorkflowPersistenceProviderInfo providerInfo)
    {
        var provider = providerInfo.Provider switch
        {
            EngineeringWorkflowPersistenceProvider.SQLite => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.SQLite,
            EngineeringWorkflowPersistenceProvider.None => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.None,
            _ => AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProvider.InMemory
        };

        return new AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence.EngineeringWorkflowPersistenceProviderInfo(
            Provider: provider,
            DurableEnabled: providerInfo.DurableEnabled,
            ProviderLabel: providerInfo.ProviderLabel);
    }

}
