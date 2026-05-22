using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointScopeEvaluationService
{
    Task<ProtectedEndpointScopeEvaluationResult> EvaluateProjectScopeAsync(
        AuthenticatedPrincipal principal,
        int projectId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateBuildingScopeAsync(
        AuthenticatedPrincipal principal,
        int buildingId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateWorkflowScopeAsync(
        AuthenticatedPrincipal principal,
        string workflowId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateScenarioScopeAsync(
        AuthenticatedPrincipal principal,
        string scenarioId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateJobScopeAsync(
        AuthenticatedPrincipal principal,
        string jobId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateFloorScopeAsync(
        AuthenticatedPrincipal principal,
        int floorId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointScopeEvaluationResult> EvaluateRoomScopeAsync(
        AuthenticatedPrincipal principal,
        int roomId,
        Permission permission,
        CancellationToken cancellationToken);
}
