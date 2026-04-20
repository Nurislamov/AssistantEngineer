using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Data.Repositories;

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

    public async Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Buildings
            .Include(building => building.Floors)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
        await WithCalculationGraph(_context.Buildings)
            .FirstOrDefaultAsync(building => building.Id == id, cancellationToken);

    public async Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
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

    private static IQueryable<Building> WithCalculationGraph(IQueryable<Building> query) =>
        query
            .AsNoTracking()
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
                        .ThenInclude(wall => wall.ConstructionAssembly)
                            .ThenInclude(assembly => assembly!.Layers)
                                .ThenInclude(layer => layer.Material);
}
