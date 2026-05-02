using AssistantEngineer.Modules.Calculations.Application.Models.Heating;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;

public interface IBuildingHeatingReadModelRepository
{
    Task<BuildingHeatingReadModel?> GetByIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default);
}
