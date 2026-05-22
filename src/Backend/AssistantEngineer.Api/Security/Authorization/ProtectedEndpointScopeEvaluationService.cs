using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using System.Globalization;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointScopeEvaluationService : IProtectedEndpointScopeEvaluationService
{
    private readonly IProjectReadAccessScopeResolver _projectScopeResolver;
    private readonly IBuildingReadAccessScopeResolver _buildingScopeResolver;
    private readonly IFloorAccessScopeResolver _floorScopeResolver;
    private readonly IRoomAccessScopeResolver _roomScopeResolver;
    private readonly IWorkflowAccessScopeResolver _workflowScopeResolver;
    private readonly ProjectTenantAccessPolicy _accessPolicy;

    public ProtectedEndpointScopeEvaluationService(
        IProjectReadAccessScopeResolver projectScopeResolver,
        IBuildingReadAccessScopeResolver buildingScopeResolver,
        IFloorAccessScopeResolver floorScopeResolver,
        IRoomAccessScopeResolver roomScopeResolver,
        IWorkflowAccessScopeResolver workflowScopeResolver,
        ProjectTenantAccessPolicy accessPolicy)
    {
        _projectScopeResolver = projectScopeResolver;
        _buildingScopeResolver = buildingScopeResolver;
        _floorScopeResolver = floorScopeResolver;
        _roomScopeResolver = roomScopeResolver;
        _workflowScopeResolver = workflowScopeResolver;
        _accessPolicy = accessPolicy;
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateProjectScopeAsync(
        AuthenticatedPrincipal principal,
        int projectId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var scope = await _projectScopeResolver.ResolveProjectScopeAsync(projectId, cancellationToken);
        if (scope is null)
        {
            return ProtectedEndpointScopeEvaluationResult.Missing(
                ProtectedEndpointScopeKind.Project,
                projectId: projectId);
        }

        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_accessPolicy.CanAccessProject(principalContext, scope, permission))
        {
            return ProtectedEndpointScopeEvaluationResult.Allowed(
                ProtectedEndpointScopeKind.Project,
                projectId: projectId);
        }

        return ProtectedEndpointScopeEvaluationResult.Mismatch(
            ProtectedEndpointScopeKind.Project,
            projectId: projectId);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateBuildingScopeAsync(
        AuthenticatedPrincipal principal,
        int buildingId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var scope = await _buildingScopeResolver.ResolveBuildingScopeAsync(buildingId, cancellationToken);
        if (scope is null)
        {
            return ProtectedEndpointScopeEvaluationResult.Missing(
                ProtectedEndpointScopeKind.Building,
                buildingId: buildingId);
        }

        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_accessPolicy.CanAccessBuilding(principalContext, scope, permission))
        {
            return ProtectedEndpointScopeEvaluationResult.Allowed(
                ProtectedEndpointScopeKind.Building,
                projectId: scope.ProjectId,
                buildingId: buildingId);
        }

        return ProtectedEndpointScopeEvaluationResult.Mismatch(
            ProtectedEndpointScopeKind.Building,
            projectId: scope.ProjectId,
            buildingId: buildingId);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateWorkflowScopeAsync(
        AuthenticatedPrincipal principal,
        string workflowId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var scope = await _workflowScopeResolver.ResolveWorkflowScopeAsync(workflowId, cancellationToken);
        return EvaluateResolvedWorkflowScope(principal, permission, workflowId, scope, ProtectedEndpointScopeKind.Workflow);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateScenarioScopeAsync(
        AuthenticatedPrincipal principal,
        string scenarioId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var scope = await _workflowScopeResolver.ResolveScenarioScopeAsync(scenarioId, cancellationToken);
        return EvaluateResolvedWorkflowScope(principal, permission, scenarioId, scope, ProtectedEndpointScopeKind.WorkflowScenario);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateJobScopeAsync(
        AuthenticatedPrincipal principal,
        string jobId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var scope = await _workflowScopeResolver.ResolveJobScopeAsync(jobId, cancellationToken);
        return EvaluateResolvedWorkflowScope(principal, permission, jobId, scope, ProtectedEndpointScopeKind.WorkflowJob);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateFloorScopeAsync(
        AuthenticatedPrincipal principal,
        int floorId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var floorScope = await _floorScopeResolver.ResolveFloorScopeAsync(floorId, cancellationToken);
        if (floorScope is null)
        {
            return ProtectedEndpointScopeEvaluationResult.Missing(
                ProtectedEndpointScopeKind.Floor,
                scopeIdentifier: floorId.ToString(CultureInfo.InvariantCulture));
        }

        return await EvaluateBuildingScopeAsync(principal, floorScope.BuildingId, permission, cancellationToken);
    }

    public async Task<ProtectedEndpointScopeEvaluationResult> EvaluateRoomScopeAsync(
        AuthenticatedPrincipal principal,
        int roomId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var roomScope = await _roomScopeResolver.ResolveRoomScopeAsync(roomId, cancellationToken);
        if (roomScope is null)
        {
            return ProtectedEndpointScopeEvaluationResult.Missing(
                ProtectedEndpointScopeKind.Room,
                scopeIdentifier: roomId.ToString(CultureInfo.InvariantCulture));
        }

        return await EvaluateBuildingScopeAsync(principal, roomScope.BuildingId, permission, cancellationToken);
    }

    private ProtectedEndpointScopeEvaluationResult EvaluateResolvedWorkflowScope(
        AuthenticatedPrincipal principal,
        Permission permission,
        string scopeIdentifier,
        WorkflowAccessScope? scope,
        ProtectedEndpointScopeKind scopeKind)
    {
        if (scope is null)
        {
            // Workflow/scenario/job misses currently fall back to broader scope in gate flow.
            return ProtectedEndpointScopeEvaluationResult.NotEvaluated(scopeKind);
        }

        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_accessPolicy.CanAccessWorkflow(principalContext, scope, permission))
        {
            return ProtectedEndpointScopeEvaluationResult.Allowed(
                scopeKind,
                projectId: scope.ProjectId,
                buildingId: scope.BuildingId,
                scopeIdentifier: scopeIdentifier);
        }

        if (scope.OrganizationId is null && !scope.IsTenantScoped)
        {
            // Preserve existing fallback behavior for unscoped workflow resources.
            return ProtectedEndpointScopeEvaluationResult.NotEvaluated(scopeKind);
        }

        return ProtectedEndpointScopeEvaluationResult.Mismatch(
            scopeKind,
            projectId: scope.ProjectId,
            buildingId: scope.BuildingId,
            scopeIdentifier: scopeIdentifier);
    }
}
