using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default);
    Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default);
    Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default);
    Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    void Add(Building building);
}
