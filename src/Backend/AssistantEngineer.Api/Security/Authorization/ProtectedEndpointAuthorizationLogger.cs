using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointAuthorizationLogger : IProtectedEndpointAuthorizationLogger
{
    private readonly ILogger<ProtectedEndpointAuthorizationGate> _logger;

    public ProtectedEndpointAuthorizationLogger(ILogger<ProtectedEndpointAuthorizationGate> logger)
    {
        _logger = logger;
    }

    public void LogProjectDenied(int projectId, Permission permission, bool returnNotFound)
    {
        _logger.LogInformation(
            "Project authorization denied for principal. ProjectId={ProjectId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
            projectId,
            permission,
            returnNotFound);
    }

    public void LogBuildingDenied(int buildingId, Permission permission, bool returnNotFound)
    {
        _logger.LogInformation(
            "Building authorization denied for principal. BuildingId={BuildingId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
            buildingId,
            permission,
            returnNotFound);
    }

    public void LogWorkflowDenied(string workflowId, string? artifactId, Permission permission, bool returnNotFound)
    {
        _logger.LogInformation(
            "Workflow authorization denied for principal. WorkflowId={WorkflowId}, ArtifactId={ArtifactId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
            workflowId,
            artifactId,
            permission,
            returnNotFound);
    }
}
