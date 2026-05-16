using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultRoomAccessScopeResolver : IRoomAccessScopeResolver
{
    private readonly IRoomRepository _rooms;

    public DefaultRoomAccessScopeResolver(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<RoomAccessScope?> ResolveRoomScopeAsync(
        int roomId,
        CancellationToken cancellationToken)
    {
        var room = await _rooms.GetForCalculationAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        return new RoomAccessScope(
            RoomId: room.Id,
            FloorId: room.FloorId,
            BuildingId: room.Floor.BuildingId,
            ProjectId: room.Floor.Building.ProjectId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }
}
