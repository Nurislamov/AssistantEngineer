using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IBuildingRepository
{
    Task<Building?> GetByIdAsync(
        int id,
        bool includeClimateZone = false,
        CancellationToken cancellationToken = default);

    Task<Building?> GetWithFloorsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Building?> GetWithThermalZonesAndRoomsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Building?> GetByThermalZoneIdAsync(
        int thermalZoneId,
        CancellationToken cancellationToken = default);

    Task<Building?> GetForCalculationAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Building?> GetForReportAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Building>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    void Add(Building building);
    void Remove(Building building);
    
    Task<Building?> GetForValidationAsync(
        int id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);
}
