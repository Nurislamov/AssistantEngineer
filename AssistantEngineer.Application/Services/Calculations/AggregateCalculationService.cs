using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Services.Calculations;

namespace AssistantEngineer.Services.Calculations;

public class AggregateCalculationService
{
    private readonly IAppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public AggregateCalculationService(
        IAppDbContext context,
        RoomCalculationService roomCalculationService)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
    }

    public async Task<FloorCalculationResult?> CalculateFloorAsync(int floorId)
    {
        var floor = _context.Floors
            .FirstOrDefault(f => f.Id == floorId);

        if (floor == null)
            return null;

        var rooms = _context.Rooms
            .Where(r => r.FloorId == floorId)
            .ToList();

        var roomResults = CalculateRooms(rooms);
        var result = BuildFloorResult(floor, rooms, roomResults);

        floor.DesignReserveFactor = result.DesignReserveFactor;
        floor.DesignCapacityW = result.DesignCapacityW;
        floor.DesignCapacityKw = result.DesignCapacityKw;

        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<BuildingCalculationResult?> CalculateBuildingAsync(int buildingId)
    {
        var building = _context.Buildings
            .FirstOrDefault(b => b.Id == buildingId);

        if (building == null)
            return null;

        var floors = _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .ToList();

        var floorIds = floors
            .Select(f => f.Id)
            .ToList();

        var rooms = _context.Rooms
            .Where(r => floorIds.Contains(r.FloorId))
            .ToList();

        var roomResults = CalculateRooms(rooms);

        foreach (var floor in floors)
        {
            var floorRooms = rooms
                .Where(room => room.FloorId == floor.Id)
                .ToList();

            var floorResult = BuildFloorResult(floor, floorRooms, roomResults);
            floor.DesignReserveFactor = floorResult.DesignReserveFactor;
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
            DesignReserveFactor = RoomCalculationService.DefaultDesignReserveFactor,
            DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
            DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2)
        };

        building.DesignReserveFactor = result.DesignReserveFactor;
        building.DesignCapacityW = result.DesignCapacityW;
        building.DesignCapacityKw = result.DesignCapacityKw;

        await _context.SaveChangesAsync();

        return result;
    }

    private Dictionary<int, RoomCalculationResult> CalculateRooms(
        IReadOnlyCollection<Room> rooms)
    {
        var results = new Dictionary<int, RoomCalculationResult>();

        if (rooms.Count == 0)
            return results;

        var roomIds = rooms
            .Select(room => room.Id)
            .ToList();

        var windowsByRoomId = (_context.Windows
                .Where(window => roomIds.Contains(window.RoomId))
                .ToList())
            .ToLookup(window => window.RoomId);

        var wallsByRoomId = (_context.Walls
                .Where(wall => roomIds.Contains(wall.RoomId))
                .ToList())
            .ToLookup(wall => wall.RoomId);

        foreach (var room in rooms)
        {
            var result = _roomCalculationService.Calculate(
                room,
                windowsByRoomId[room.Id],
                wallsByRoomId[room.Id]);

            room.DesignReserveFactor = result.DesignReserveFactor;
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
            DesignReserveFactor = RoomCalculationService.DefaultDesignReserveFactor,
            DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
            DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2)
        };
    }
}
