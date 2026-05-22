using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointAuthorizationLogger
{
    void LogProjectDenied(int projectId, Permission permission, bool returnNotFound);

    void LogBuildingDenied(int buildingId, Permission permission, bool returnNotFound);

    void LogWorkflowDenied(string workflowId, string? artifactId, Permission permission, bool returnNotFound);
}
