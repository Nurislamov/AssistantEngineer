using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultWorkflowAccessScopeResolver : IWorkflowAccessScopeResolver
{
    private readonly IEngineeringWorkflowPersistenceService _workflowPersistence;
    private readonly IEngineeringCalculationJobRepository _jobRepository;
    private readonly IProjectReadAccessScopeResolver _projectScopeResolver;
    private readonly IBuildingReadAccessScopeResolver _buildingScopeResolver;

    public DefaultWorkflowAccessScopeResolver(
        IEngineeringWorkflowPersistenceService workflowPersistence,
        IEngineeringCalculationJobRepository jobRepository,
        IProjectReadAccessScopeResolver projectScopeResolver,
        IBuildingReadAccessScopeResolver buildingScopeResolver)
    {
        _workflowPersistence = workflowPersistence;
        _jobRepository = jobRepository;
        _projectScopeResolver = projectScopeResolver;
        _buildingScopeResolver = buildingScopeResolver;
    }

    public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(
        string workflowId,
        CancellationToken cancellationToken)
    {
        return ResolveByWorkflowIdAsync(workflowId, cancellationToken);
    }

    public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        return ResolveByScenarioIdAsync(
            scenarioId,
            workflowScopeId: scenarioId,
            cancellationToken);
    }

    public Task<WorkflowAccessScope?> ResolveJobScopeAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        return ResolveByJobIdAsync(jobId, cancellationToken);
    }

    private async Task<WorkflowAccessScope?> ResolveByWorkflowIdAsync(
        string workflowId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
        {
            return null;
        }

        var scenarioScope = await ResolveByScenarioIdAsync(
            scenarioId: workflowId,
            workflowScopeId: workflowId,
            cancellationToken);
        if (scenarioScope is not null)
        {
            return scenarioScope;
        }

        return await ResolveByJobIdAsync(workflowId, cancellationToken);
    }

    private async Task<WorkflowAccessScope?> ResolveByScenarioIdAsync(
        string scenarioId,
        string workflowScopeId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            return null;
        }

        var scenario = await _workflowPersistence.GetScenarioAsync(scenarioId, cancellationToken);
        if (scenario is null)
        {
            return null;
        }

        return await BuildScopeAsync(
            workflowScopeId,
            projectId: scenario.ProjectId,
            buildingId: scenario.BuildingId,
            cancellationToken);
    }

    private async Task<WorkflowAccessScope?> ResolveByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return null;
        }

        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        int? resolvedProjectId = job.ProjectId;
        int? buildingId = null;
        if (!string.IsNullOrWhiteSpace(job.ScenarioId))
        {
            var scenario = await _workflowPersistence.GetScenarioAsync(job.ScenarioId, cancellationToken);
            if (scenario is not null)
            {
                resolvedProjectId = scenario.ProjectId;
            }

            buildingId = scenario?.BuildingId;
        }

        return await BuildScopeAsync(
            workflowId: jobId,
            projectId: resolvedProjectId,
            buildingId: buildingId,
            cancellationToken);
    }

    private async Task<WorkflowAccessScope?> BuildScopeAsync(
        string workflowId,
        int? projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        projectId = NormalizeIdentifier(projectId);
        buildingId = NormalizeIdentifier(buildingId);

        if (projectId is null && buildingId is null)
        {
            return null;
        }

        int? organizationId = null;
        int? ownerUserId = null;
        TenantScope? tenantScope = null;
        var isTenantScoped = false;

        if (buildingId.HasValue)
        {
            var buildingScope = await _buildingScopeResolver.ResolveBuildingScopeAsync(buildingId.Value, cancellationToken);
            if (buildingScope is not null)
            {
                organizationId = buildingScope.OrganizationId;
                ownerUserId = buildingScope.OwnerUserId;
                tenantScope = buildingScope.TenantScope;
                isTenantScoped = buildingScope.IsTenantScoped;
            }
        }

        if (organizationId is null && projectId.HasValue)
        {
            var projectScope = await _projectScopeResolver.ResolveProjectScopeAsync(projectId.Value, cancellationToken);
            if (projectScope is not null)
            {
                organizationId = projectScope.OrganizationId;
                ownerUserId ??= projectScope.OwnerUserId;
                tenantScope ??= projectScope.TenantScope;
                isTenantScoped = isTenantScoped || projectScope.IsTenantScoped;
            }
        }

        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: workflowId,
            projectId: projectId,
            buildingId: buildingId,
            organizationId: organizationId,
            ownerUserId: ownerUserId,
            isTenantScoped: isTenantScoped,
            tenantScope: tenantScope);
    }

    private static int? NormalizeIdentifier(int? value)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return null;
        }

        return value.Value;
    }
}
