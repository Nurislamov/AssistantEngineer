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
            .FirstOrDefaultAsync(f => f.Id == floorId);

        if (floor == null)
            return null;

        var rooms = await _context.Rooms
            .Where(r => r.FloorId == floorId)
            .ToListAsync();

        var roomResults = await CalculateRoomsAsync(rooms);
        var result = BuildFloorResult(floor, rooms, roomResults);

        floor.ReserveFactor = result.ReserveFactor;
        floor.DesignCapacityW = result.DesignCapacityW;
        floor.DesignCapacityKw = result.DesignCapacityKw;

        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<BuildingCalculationResult?> CalculateBuildingAsync(int buildingId)
    {
        var building = await _context.Buildings
            .FirstOrDefaultAsync(b => b.Id == buildingId);

        if (building == null)
            return null;

        var floors = await _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .ToListAsync();

        var floorIds = floors
            .Select(f => f.Id)
            .ToList();

        var rooms = await _context.Rooms
            .Where(r => floorIds.Contains(r.FloorId))
            .ToListAsync();

        var roomResults = await CalculateRoomsAsync(rooms);

        foreach (var floor in floors)
        {
            var floorRooms = rooms
                .Where(room => room.FloorId == floor.Id)
                .ToList();

            var floorResult = BuildFloorResult(floor, floorRooms, roomResults);
            floor.ReserveFactor = floorResult.ReserveFactor;
            floor.DesignCapacityW = floorResult.DesignCapacityW;
            floor.DesignCapacityKw = floorResult.DesignCapacityKw;
        }

        var totalHeatLoadW = roomResults.Values.Sum(result => result.TotalHeatLoadW);
        var totalDesignCapacityW = roomResults.Values.Sum(result => result.DesignCapacityW);

        var result = new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            FloorsCount = floors.Count,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2),
            ReserveFactor = RoomCalculationService.DefaultReserveFactor,
            DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
            DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2)
        };

        building.ReserveFactor = result.ReserveFactor;
        building.DesignCapacityW = result.DesignCapacityW;
        building.DesignCapacityKw = result.DesignCapacityKw;

        await _context.SaveChangesAsync();

        return result;
    }

    private async Task<Dictionary<int, RoomCalculationResult>> CalculateRoomsAsync(IReadOnlyCollection<Room> rooms)
    {
        var results = new Dictionary<int, RoomCalculationResult>();

        if (rooms.Count == 0)
            return results;

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

        foreach (var room in rooms)
        {
            var result = _roomCalculationService.Calculate(
                room,
                windowsByRoomId[room.Id],
                wallsByRoomId[room.Id]);

            room.ReserveFactor = result.ReserveFactor;
            room.DesignCapacityW = result.DesignCapacityW;
            room.DesignCapacityKw = result.DesignCapacityKw;

            results[room.Id] = result;
        }

        return results;
    }

    private static FloorCalculationResult BuildFloorResult(
        Floor floor,
        IReadOnlyCollection<Room> rooms,
        IReadOnlyDictionary<int, RoomCalculationResult> roomResults)
    {
        var totalHeatLoadW = rooms.Sum(room => roomResults[room.Id].TotalHeatLoadW);
        var totalDesignCapacityW = rooms.Sum(room => roomResults[room.Id].DesignCapacityW);

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2),
            ReserveFactor = RoomCalculationService.DefaultReserveFactor,
            DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
            DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2)
        };
    }
}
