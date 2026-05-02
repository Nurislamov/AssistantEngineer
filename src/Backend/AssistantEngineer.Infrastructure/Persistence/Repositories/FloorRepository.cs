using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

internal sealed class FloorRepository : IFloorRepository
{
    private readonly AppDbContext _context;

    public FloorRepository(AppDbContext context) => _context = context;

    public async Task<Floor?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Floors.FindAsync([id], cancellationToken);

    public async Task<Floor?> GetWithRoomsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Floors
            .Include(floor => floor.Rooms)
            .FirstOrDefaultAsync(floor => floor.Id == id, cancellationToken);

    public async Task<Floor?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Floors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(floor => floor.Building)
                .ThenInclude(building => building.Project)
            .Include(floor => floor.Building)
                .ThenInclude(building => building.ClimateZone)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.OccupancySchedule)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.EquipmentSchedule)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.LightingSchedule)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.VentilationParameters)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.Windows)
            .Include(floor => floor.Rooms)
                .ThenInclude(room => room.Walls)
                    .ThenInclude(wall => wall.ConstructionAssembly)
                        .ThenInclude(assembly => assembly!.Layers)
                            .ThenInclude(layer => layer.Material)
            .FirstOrDefaultAsync(floor => floor.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Floor>> ListByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default) =>
        await _context.Floors
            .AsNoTracking()
            .Where(floor => floor.BuildingId == buildingId)
            .OrderBy(floor => floor.Id)
            .ToListAsync(cancellationToken);

    public void Add(Floor floor) => _context.Floors.Add(floor);

    public void Remove(Floor floor) => _context.Floors.Remove(floor);
}
