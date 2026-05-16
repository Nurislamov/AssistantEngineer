using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultBuildingReadAccessScopeResolver : IBuildingReadAccessScopeResolver
{
    private readonly IBuildingRepository _buildings;

    public DefaultBuildingReadAccessScopeResolver(IBuildingRepository buildings)
    {
        _buildings = buildings;
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

        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: building.Id,
            projectId: building.ProjectId,
            organizationId: null,
            ownerUserId: null,
            isTenantScoped: false);
    }
}
