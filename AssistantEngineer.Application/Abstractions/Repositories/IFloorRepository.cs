using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface IFloorRepository
{
    Task<Floor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default);
    Task<Floor?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Floor>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default);
    void Add(Floor floor);
}
