using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default);

    Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default);

    void Add(Room room);
    void Remove(Room room);
    void RemoveWindow(Window window);
    void RemoveWall(Wall wall);
}
