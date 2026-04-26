using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IClimateZoneRepository
{
    Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
