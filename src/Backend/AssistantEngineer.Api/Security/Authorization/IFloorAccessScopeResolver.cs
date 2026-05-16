namespace AssistantEngineer.Api.Security.Authorization;

public interface IFloorAccessScopeResolver
{
    Task<FloorAccessScope?> ResolveFloorScopeAsync(
        int floorId,
        CancellationToken cancellationToken);
}
