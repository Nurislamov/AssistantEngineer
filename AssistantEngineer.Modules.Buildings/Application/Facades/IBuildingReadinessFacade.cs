using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public interface IBuildingReadinessFacade
{
    Task<Result<BuildingCalculationReadinessReport>> CheckAsync(
        int buildingId,
        int? weatherYear,
        CancellationToken cancellationToken);
}
