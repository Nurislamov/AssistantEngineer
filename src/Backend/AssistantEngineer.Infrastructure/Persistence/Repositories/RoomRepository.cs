using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

internal sealed class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context) => _context = context;

    public async Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms.FindAsync([id], cancellationToken);

    public async Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(room => room.Floor)
                .ThenInclude(floor => floor.Building)
                    .ThenInclude(building => building.Project)
            .Include(room => room.Floor)
                .ThenInclude(floor => floor.Building)
                    .ThenInclude(building => building.ClimateZone)
            .Include(room => room.OccupancySchedule)
            .Include(room => room.EquipmentSchedule)
            .Include(room => room.LightingSchedule)
            .Include(room => room.VentilationParameters)
            .Include(room => room.Windows)
            .Include(room => room.Walls)
                .ThenInclude(wall => wall.AdjacentRoom)
            .Include(room => room.Walls)
                .ThenInclude(wall => wall.ConstructionAssembly)
                    .ThenInclude(assembly => assembly!.Layers)
                        .ThenInclude(layer => layer.Material)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .Include(room => room.Windows)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .Include(room => room.Walls)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .Include(room => room.Windows)
            .Include(room => room.Walls)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .Include(room => room.VentilationParameters)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Room>> ListByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .AsNoTracking()
            .Where(room => room.Floor.BuildingId == buildingId)
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default) =>
        await _context.Rooms
            .AsNoTracking()
            .AsSplitQuery()
            .Where(room => room.Floor.BuildingId == buildingId)
            .Include(room => room.Windows)
            .Include(room => room.Walls)
            .Include(room => room.VentilationParameters)
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Rooms.AnyAsync(room => room.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _context.Windows
            .AsNoTracking()
            .Where(window => window.RoomId == roomId)
            .OrderBy(window => window.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _context.Walls
            .AsNoTracking()
            .Where(wall => wall.RoomId == roomId)
            .OrderBy(wall => wall.Id)
            .ToListAsync(cancellationToken);

    public void Add(Room room) => _context.Rooms.Add(room);

    public void Remove(Room room) => _context.Rooms.Remove(room);

    public void RemoveWindow(Window window) => _context.Windows.Remove(window);

    public void RemoveWall(Wall wall) => _context.Walls.Remove(wall);
}
