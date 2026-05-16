using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultProjectReadAccessScopeResolver : IProjectReadAccessScopeResolver
{
    private readonly IProjectRepository _projects;

    public DefaultProjectReadAccessScopeResolver(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<ProjectAccessScope?> ResolveProjectScopeAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var project = await _projects.GetByIdAsync(
            projectId,
            cancellationToken: cancellationToken);

        if (project is null)
        {
            return null;
        }

        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: project.Id,
            organizationId: null,
            ownerUserId: null,
            isTenantScoped: false);
    }
}
