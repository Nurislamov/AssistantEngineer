using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultBuildingReadAccessScopeResolver : IBuildingReadAccessScopeResolver
{
    private readonly IBuildingRepository _buildings;
    private readonly IProjectReadAccessScopeResolver _projectScopeResolver;

    public DefaultBuildingReadAccessScopeResolver(
        IBuildingRepository buildings,
        IProjectReadAccessScopeResolver projectScopeResolver)
    {
        _buildings = buildings;
        _projectScopeResolver = projectScopeResolver;
    }

    public async Task<BuildingAccessScope?> ResolveBuildingScopeAsync(
        int buildingId,
        CancellationToken cancellationToken)
    {
        var building = await _buildings.GetByIdAsync(
            buildingId,
            includeClimateZone: false,
            cancellationToken: cancellationToken);

        if (building is null)
        {
            return null;
        }

        var projectScope = await _projectScopeResolver.ResolveProjectScopeAsync(
            building.ProjectId,
            cancellationToken);

        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: building.Id,
            projectId: building.ProjectId,
            organizationId: projectScope?.OrganizationId,
            ownerUserId: projectScope?.OwnerUserId,
            isTenantScoped: projectScope?.IsTenantScoped ?? false,
            tenantScope: projectScope?.TenantScope);
    }
}
