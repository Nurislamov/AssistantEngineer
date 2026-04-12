using AssistantEngineer.Contracts.Results;
using AssistantEngineer.Data;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Services;

public class StructureCalculationService
{
    private readonly AppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public StructureCalculationService(
        AppDbContext context,
        RoomCalculationService roomCalculationService)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
    }
    
    public async Task<FloorCalculationResult?> CalculateFloorAsync(int floorId)
    {
        var floor = await _context.Floors
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == floorId);

        if (floor == null)
            return null;

        var rooms = await _context.Rooms
            .Where(r => r.FloorId == floorId)
            .AsNoTracking()
            .ToListAsync();

        double totalHeatLoadW = 0;

        foreach (var room in rooms)
        {
            var windows = await _context.Windows
                .Where(w => w.RoomId == room.Id)
                .AsNoTracking()
                .ToListAsync();

            var walls = await _context.Walls
                .Where(w => w.RoomId == room.Id)
                .AsNoTracking()
                .ToListAsync();

            var roomResult = _roomCalculationService.Calculate(room, windows, walls);
            totalHeatLoadW += roomResult.TotalHeatLoadW;
        }

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2)
        };
    }
    
    public async Task<BuildingCalculationResult?> CalculateBuildingAsync(int buildingId)
    {
        var building = await _context.Buildings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == buildingId);

        if (building == null)
            return null;

        var floors = await _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .AsNoTracking()
            .ToListAsync();

        double totalHeatLoadW = 0;
        var roomsCount = 0;

        foreach (var floor in floors)
        {
            var floorResult = await CalculateFloorAsync(floor.Id);
            if (floorResult == null)
                continue;

            totalHeatLoadW += floorResult.TotalHeatLoadW;
            roomsCount += floorResult.RoomsCount;
        }

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            FloorsCount = floors.Count,
            RoomsCount = roomsCount,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2)
        };
    }
}