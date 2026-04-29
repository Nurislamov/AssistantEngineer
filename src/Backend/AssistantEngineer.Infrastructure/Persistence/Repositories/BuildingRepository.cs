using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

internal sealed class BuildingRepository : IBuildingRepository
{
    private readonly AppDbContext _context;

    public BuildingRepository(AppDbContext context) => _context = context;

    public async Task<Building?> GetByIdAsync(
        int id,
        bool includeClimateZone = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Building> query = _context.Buildings;

        if (includeClimateZone)
            query = query.Include(building => building.ClimateZone);

        return await query.FirstOrDefaultAsync(building => building.Id == id, cancellationToken);
    }

    public async Task<Building?> GetWithFloorsAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await _context.Buildings
            .Include(building => building.Floors)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<Building?> GetWithThermalZonesAndRoomsAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await _context.Buildings
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
            .Include(building => building.ThermalZones)
                .ThenInclude(zone => zone.Rooms)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<Building?> GetByThermalZoneIdAsync(
        int thermalZoneId,
        CancellationToken cancellationToken = default) =>
        await _context.Buildings
            .Include(building => building.ThermalZones)
                .ThenInclude(zone => zone.Rooms)
            .FirstOrDefaultAsync(
                building => building.ThermalZones.Any(zone => zone.Id == thermalZoneId),
                cancellationToken);

    public async Task<Building?> GetForCalculationAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await WithCalculationGraph(_context.Buildings)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<Building?> GetForReportAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await WithCalculationGraph(_context.Buildings)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Building>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default) =>
        await _context.Buildings
            .AsNoTracking()
            .Include(building => building.ClimateZone)
            .Where(building => building.ProjectId == projectId)
            .OrderBy(building => building.Id)
            .ToListAsync(cancellationToken);

    public void Add(Building building) => _context.Buildings.Add(building);

    public void Remove(Building building) => _context.Buildings.Remove(building);

    private static IQueryable<Building> WithCalculationGraph(IQueryable<Building> query) =>
        query
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(building => building.Project)
            .Include(building => building.ClimateZone)
            .Include(building => building.ThermalZones)
                .ThenInclude(zone => zone.Rooms)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.OccupancySchedule)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.EquipmentSchedule)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.LightingSchedule)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.VentilationParameters)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.Windows)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.Walls)
                        .ThenInclude(wall => wall.AdjacentRoom)
            .Include(building => building.Floors)
                .ThenInclude(floor => floor.Rooms)
                    .ThenInclude(room => room.Walls)
                        .ThenInclude(wall => wall.ConstructionAssembly)
                            .ThenInclude(assembly => assembly!.Layers)
                                .ThenInclude(layer => layer.Material);
    
    public async Task<Building?> GetForValidationAsync(
        int id,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        await WithValidationGraph(_context.Buildings, asTracking)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);
    
    private static IQueryable<Building> WithValidationGraph(
        IQueryable<Building> query,
        bool asTracking)
    {
        if (!asTracking)
            query = query.AsNoTrackingWithIdentityResolution();

        return query
            .AsSplitQuery()
            .Include(building => building.ClimateZone)
            .Include(building => building.ThermalZones)
            .ThenInclude(zone => zone.Rooms)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.OccupancySchedule)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.EquipmentSchedule)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.LightingSchedule)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.VentilationParameters)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.Windows)
            .Include(building => building.Floors)
            .ThenInclude(floor => floor.Rooms)
            .ThenInclude(room => room.Walls)
            .ThenInclude(wall => wall.AdjacentRoom);
    }
}
