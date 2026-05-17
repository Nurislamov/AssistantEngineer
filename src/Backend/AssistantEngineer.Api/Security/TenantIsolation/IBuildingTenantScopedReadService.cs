using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public interface IBuildingTenantScopedReadService
{
    Task<Result<Building?>> GetBuildingForTenantAsync(
        int buildingId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Building>>> ListBuildingsForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);
}
