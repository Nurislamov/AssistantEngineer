using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProjectReadAccessScopeResolver
{
    Task<ProjectAccessScope?> ResolveProjectScopeAsync(
        int projectId,
        CancellationToken cancellationToken);
}
