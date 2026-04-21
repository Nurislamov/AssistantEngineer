using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default);
    Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default);
    Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default);
    void Add(Room room);
}
