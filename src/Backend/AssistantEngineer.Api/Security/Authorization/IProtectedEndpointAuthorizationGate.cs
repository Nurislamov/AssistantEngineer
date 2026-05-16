using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointAuthorizationGate
{
    Task<ProtectedEndpointAuthorizationDecision> RequirePermissionAsync(
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireProjectPermissionAsync(
        int projectId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireBuildingPermissionAsync(
        int buildingId,
        Permission permission,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireWorkflowPermissionAsync(
        Permission permission,
        string? workflowId,
        int? projectId,
        int? buildingId,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireCalculationPermissionAsync(
        Permission permission,
        int? projectId,
        int? buildingId,
        int? floorId,
        int? roomId,
        CancellationToken cancellationToken);
}
