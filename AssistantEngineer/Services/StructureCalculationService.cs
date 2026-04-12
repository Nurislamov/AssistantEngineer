using AssistantEngineer.Contracts.Results;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
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

        var totalHeatLoadW = await CalculateRoomsTotalHeatLoadAsync(rooms);

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

        var floorIds = floors
            .Select(f => f.Id)
            .ToList();

        var rooms = await _context.Rooms
            .Where(r => floorIds.Contains(r.FloorId))
            .AsNoTracking()
            .ToListAsync();

        var totalHeatLoadW = await CalculateRoomsTotalHeatLoadAsync(rooms);

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            FloorsCount = floors.Count,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2)
        };
    }

    private async Task<double> CalculateRoomsTotalHeatLoadAsync(IReadOnlyCollection<Room> rooms)
    {
        if (rooms.Count == 0)
            return 0;

        var roomIds = rooms
            .Select(room => room.Id)
            .ToList();

        var windowsByRoomId = (await _context.Windows
                .Where(window => roomIds.Contains(window.RoomId))
                .AsNoTracking()
                .ToListAsync())
            .ToLookup(window => window.RoomId);

        var wallsByRoomId = (await _context.Walls
                .Where(wall => roomIds.Contains(wall.RoomId))
                .AsNoTracking()
                .ToListAsync())
            .ToLookup(wall => wall.RoomId);

        double totalHeatLoadW = 0;

        foreach (var room in rooms)
        {
            var roomResult = _roomCalculationService.Calculate(
                room,
                windowsByRoomId[room.Id],
                wallsByRoomId[room.Id]);

            totalHeatLoadW += roomResult.TotalHeatLoadW;
        }

        return totalHeatLoadW;
    }
}
