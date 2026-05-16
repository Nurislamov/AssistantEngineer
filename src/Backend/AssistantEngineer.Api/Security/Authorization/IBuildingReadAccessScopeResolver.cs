using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IBuildingReadAccessScopeResolver
{
    Task<BuildingAccessScope?> ResolveBuildingScopeAsync(
        int buildingId,
        CancellationToken cancellationToken);
}
