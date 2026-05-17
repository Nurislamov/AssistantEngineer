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
            organizationId: project.OrganizationId,
            ownerUserId: project.OwnerUserId,
            isTenantScoped: project.IsTenantScoped,
            tenantScope: project.OrganizationId.HasValue
                ? new TenantScope(project.OrganizationId.Value, $"org-{project.OrganizationId.Value}", IsActive: true)
                : null);
    }
}
