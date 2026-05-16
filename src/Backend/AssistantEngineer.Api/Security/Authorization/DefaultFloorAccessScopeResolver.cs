using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultFloorAccessScopeResolver : IFloorAccessScopeResolver
{
    private readonly IFloorRepository _floors;

    public DefaultFloorAccessScopeResolver(IFloorRepository floors)
    {
        _floors = floors;
    }

    public async Task<FloorAccessScope?> ResolveFloorScopeAsync(
        int floorId,
        CancellationToken cancellationToken)
    {
        var floor = await _floors.GetForCalculationAsync(floorId, cancellationToken);
        if (floor is null)
        {
            return null;
        }

        return new FloorAccessScope(
            FloorId: floor.Id,
            BuildingId: floor.BuildingId,
            ProjectId: floor.Building.ProjectId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }
}
