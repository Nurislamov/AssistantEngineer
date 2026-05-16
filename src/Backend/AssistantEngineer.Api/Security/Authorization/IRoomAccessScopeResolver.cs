namespace AssistantEngineer.Api.Security.Authorization;

public interface IRoomAccessScopeResolver
{
    Task<RoomAccessScope?> ResolveRoomScopeAsync(
        int roomId,
        CancellationToken cancellationToken);
}
