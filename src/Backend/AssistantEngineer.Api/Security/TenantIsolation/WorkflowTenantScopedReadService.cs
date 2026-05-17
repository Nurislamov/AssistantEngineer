using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public sealed class WorkflowTenantScopedReadService : IWorkflowTenantScopedReadService
{
    private const string WorkflowResourceNotFoundMessage = "Workflow resource was not found in workflow persistence store.";
    private const string WorkflowProjectNotFoundMessage = "Workflow project scope was not found.";

    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;
    private readonly IEngineeringCalculationJobService _jobService;
    private readonly IProjectReadAccessScopeResolver _projectScopeResolver;
    private readonly IWorkflowAccessScopeResolver _workflowScopeResolver;
    private readonly ITenantQueryIsolationPolicy _policy;

    public WorkflowTenantScopedReadService(
        IEngineeringWorkflowPersistenceService workflowPersistence,
        IEngineeringCalculationJobService jobService,
        IProjectReadAccessScopeResolver projectScopeResolver,
        IWorkflowAccessScopeResolver workflowScopeResolver,
        ITenantQueryIsolationPolicy policy)
    {
        _workflowPersistence = workflowPersistence;
        _jobService = jobService;
        _projectScopeResolver = projectScopeResolver;
        _workflowScopeResolver = workflowScopeResolver;
        _policy = policy;
    }

    public async Task<Result<WorkflowTenantScopedStateReadResult>> GetWorkflowStateForTenantAsync(
        int projectId,
        int? buildingId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeProjectAsync(
            projectId,
            context,
            notFoundResourceMessage: $"Project with id {projectId} not found.",
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<WorkflowTenantScopedStateReadResult>.Failure(accessResult);
        }

        var persistedState = await _workflowPersistence.GetLatestWorkflowStateAsync(
            projectId,
            buildingId,
            cancellationToken);

        return Result<WorkflowTenantScopedStateReadResult>.Success(new WorkflowTenantScopedStateReadResult(persistedState));
    }

    public async Task<Result<EngineeringCalculationScenarioRecordDto>> GetScenarioForTenantAsync(
        string scenarioId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            return Result<EngineeringCalculationScenarioRecordDto>.NotFound(WorkflowResourceNotFoundMessage);
        }

        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        if (scenario is null)
        {
            return Result<EngineeringCalculationScenarioRecordDto>.NotFound(WorkflowResourceNotFoundMessage);
        }

        var accessResult = await AuthorizeWorkflowScopeAsync(
            identifier: scenarioId,
            resolveScope: token => _workflowScopeResolver.ResolveScenarioScopeAsync(scenarioId, token),
            fallbackProjectId: scenario.ProjectId,
            context,
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<EngineeringCalculationScenarioRecordDto>.Failure(accessResult);
        }

        return Result<EngineeringCalculationScenarioRecordDto>.Success(scenario);
    }

    public async Task<Result<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>> ListScenariosForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeProjectAsync(
            projectId,
            context,
            notFoundResourceMessage: $"Project with id {projectId} not found.",
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>.Failure(accessResult);
        }

        var scenarios = await _workflowPersistence.ListProjectScenariosAsync(projectId, cancellationToken);
        return Result<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>.Success(scenarios);
    }

    public async Task<Result<EngineeringCalculationJobResultDto>> GetJobForTenantAsync(
        string jobId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Result<EngineeringCalculationJobResultDto>.NotFound(WorkflowResourceNotFoundMessage);
        }

        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return Result<EngineeringCalculationJobResultDto>.NotFound(WorkflowResourceNotFoundMessage);
        }

        var accessResult = await AuthorizeWorkflowScopeAsync(
            identifier: jobId,
            resolveScope: token => _workflowScopeResolver.ResolveJobScopeAsync(jobId, token),
            fallbackProjectId: job.ProjectId,
            context,
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<EngineeringCalculationJobResultDto>.Failure(accessResult);
        }

        return Result<EngineeringCalculationJobResultDto>.Success(job);
    }

    public async Task<Result<IReadOnlyList<EngineeringCalculationJobEventDto>>> GetJobEventsForTenantAsync(
        string jobId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return Result<IReadOnlyList<EngineeringCalculationJobEventDto>>.Success(Array.Empty<EngineeringCalculationJobEventDto>());
        }

        var job = await _jobService.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            var fallbackEvents = await _jobService.ListJobEventsAsync(jobId, cancellationToken);
            return Result<IReadOnlyList<EngineeringCalculationJobEventDto>>.Success(fallbackEvents);
        }

        var accessResult = await AuthorizeWorkflowScopeAsync(
            identifier: jobId,
            resolveScope: token => _workflowScopeResolver.ResolveJobScopeAsync(jobId, token),
            fallbackProjectId: job.ProjectId,
            context,
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<IReadOnlyList<EngineeringCalculationJobEventDto>>.Failure(accessResult);
        }

        var events = await _jobService.ListJobEventsAsync(jobId, cancellationToken);
        return Result<IReadOnlyList<EngineeringCalculationJobEventDto>>.Success(events);
    }

    public async Task<Result<IReadOnlyList<EngineeringCalculationJobResultDto>>> ListJobsForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default)
    {
        var accessResult = await AuthorizeProjectAsync(
            projectId,
            context,
            notFoundResourceMessage: $"Project with id {projectId} not found.",
            cancellationToken);
        if (accessResult.IsFailure)
        {
            return Result<IReadOnlyList<EngineeringCalculationJobResultDto>>.Failure(accessResult);
        }

        var jobs = await _jobService.ListProjectJobsAsync(projectId, cancellationToken);
        return Result<IReadOnlyList<EngineeringCalculationJobResultDto>>.Success(jobs);
    }

    private async Task<Result> AuthorizeWorkflowScopeAsync(
        string identifier,
        Func<CancellationToken, Task<WorkflowAccessScope?>> resolveScope,
        int fallbackProjectId,
        TenantQueryContext context,
        CancellationToken cancellationToken)
    {
        var workflowScope = await resolveScope(cancellationToken);
        if (workflowScope is not null)
        {
            var strictScopeResult = EnforceStrictOrganizationRequirement(
                context,
                workflowScope.OrganizationId,
                WorkflowResourceNotFoundMessage);
            if (strictScopeResult is not null)
            {
                return strictScopeResult;
            }

            var decision = _policy.CanReadResource(
                context,
                workflowScope.OrganizationId,
                Permission.WorkflowsRead.ToString());
            if (decision.Allowed)
            {
                return Result.Success();
            }

            return ToDeniedResult(
                decision,
                notFoundResourceMessage: WorkflowResourceNotFoundMessage,
                fallbackDeniedMessage: $"Workflow read denied for {identifier}.");
        }

        var projectScope = await _projectScopeResolver.ResolveProjectScopeAsync(fallbackProjectId, cancellationToken);
        if (projectScope is not null)
        {
            var strictScopeResult = EnforceStrictOrganizationRequirement(
                context,
                projectScope.OrganizationId,
                WorkflowResourceNotFoundMessage);
            if (strictScopeResult is not null)
            {
                return strictScopeResult;
            }

            var fallbackProjectDecision = _policy.CanReadResource(
                context,
                projectScope.OrganizationId,
                Permission.WorkflowsRead.ToString());
            if (fallbackProjectDecision.Allowed)
            {
                return Result.Success();
            }

            return ToDeniedResult(
                fallbackProjectDecision,
                notFoundResourceMessage: WorkflowResourceNotFoundMessage,
                fallbackDeniedMessage: $"Workflow project scope denied for {identifier}.");
        }

        if (context.StrictTenantMatch)
        {
            return context.ReturnNotFoundForTenantMismatch
                ? Result.NotFound(WorkflowResourceNotFoundMessage)
                : Result.Failure(TenantQueryFailureReasons.MissingOrganization);
        }

        var compatibilityDecision = _policy.CanReadResource(
            context,
            resourceOrganizationId: null,
            Permission.WorkflowsRead.ToString());
        if (compatibilityDecision.Allowed)
        {
            return Result.Success();
        }

        return ToDeniedResult(
            compatibilityDecision,
            notFoundResourceMessage: WorkflowResourceNotFoundMessage,
            fallbackDeniedMessage: $"Workflow fallback scope denied for {identifier}.");
    }

    private static Result? EnforceStrictOrganizationRequirement(
        TenantQueryContext context,
        int? organizationId,
        string notFoundResourceMessage)
    {
        if (!context.StrictTenantMatch || organizationId.HasValue)
        {
            return null;
        }

        return context.ReturnNotFoundForTenantMismatch
            ? Result.NotFound(notFoundResourceMessage)
            : Result.Failure(TenantQueryFailureReasons.MissingOrganization);
    }

    private async Task<Result> AuthorizeProjectAsync(
        int projectId,
        TenantQueryContext context,
        string notFoundResourceMessage,
        CancellationToken cancellationToken)
    {
        var projectScope = await _projectScopeResolver.ResolveProjectScopeAsync(projectId, cancellationToken);
        if (projectScope is null)
        {
            return Result.NotFound(notFoundResourceMessage);
        }

        var decision = _policy.CanReadResource(
            context,
            projectScope.OrganizationId,
            Permission.WorkflowsRead.ToString());

        if (decision.Allowed)
        {
            return Result.Success();
        }

        return ToDeniedResult(
            decision,
            notFoundResourceMessage,
            fallbackDeniedMessage: WorkflowProjectNotFoundMessage);
    }

    private static Result ToDeniedResult(
        TenantScopedQueryDecision decision,
        string notFoundResourceMessage,
        string fallbackDeniedMessage)
    {
        if (decision.ShouldReturnNotFound)
        {
            return Result.NotFound(notFoundResourceMessage);
        }

        return Result.Failure(CreateDeniedMessage(decision, fallbackDeniedMessage));
    }

    private static string CreateDeniedMessage(
        TenantScopedQueryDecision decision,
        string fallback) =>
        string.IsNullOrWhiteSpace(decision.FailureReason)
            ? fallback
            : decision.FailureReason;
}
