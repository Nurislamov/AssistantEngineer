using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IFloorRepository
{
    Task<Floor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default);
    Task<Floor?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default);
    void Add(Floor floor);
    void Remove(Floor floor);
}
