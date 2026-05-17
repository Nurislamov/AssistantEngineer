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

    Task<ProtectedEndpointAuthorizationDecision> RequireReportReadPermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireReportWritePermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireArtifactReadPermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        string? artifactId,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireArtifactWritePermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        string? artifactId,
        CancellationToken cancellationToken);

    Task<ProtectedEndpointAuthorizationDecision> RequireWorkflowReadPermissionAsync(
        string? workflowId,
        string? scenarioId,
        string? jobId,
        int? projectId,
        int? buildingId,
        CancellationToken cancellationToken);
}
