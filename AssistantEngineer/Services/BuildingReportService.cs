using AssistantEngineer.Contracts;
using AssistantEngineer.Contracts.Results;
using AssistantEngineer.Data;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Services;

public class BuildingReportService
{
    private readonly AppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public BuildingReportService(
        AppDbContext context,
        RoomCalculationService roomCalculationService)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
    }

    public async Task<BuildingReportDto?> BuildReportAsync(int buildingId)
    {
        var building = await _context.Buildings
            .Include(b => b.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == buildingId);

        if (building == null)
            return null;

        var floors = await _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .OrderBy(f => f.Id)
            .AsNoTracking()
            .ToListAsync();

        var floorIds = floors
            .Select(f => f.Id)
            .ToList();

        var rooms = await _context.Rooms
            .Where(r => floorIds.Contains(r.FloorId))
            .OrderBy(r => r.FloorId)
            .ThenBy(r => r.Id)
            .AsNoTracking()
            .ToListAsync();

        var roomIds = rooms
            .Select(r => r.Id)
            .ToList();

        var windows = await _context.Windows
            .Where(w => roomIds.Contains(w.RoomId))
            .OrderBy(w => w.RoomId)
            .ThenBy(w => w.Id)
            .AsNoTracking()
            .ToListAsync();

        var walls = await _context.Walls
            .Where(w => roomIds.Contains(w.RoomId))
            .OrderBy(w => w.RoomId)
            .ThenBy(w => w.Id)
            .AsNoTracking()
            .ToListAsync();

        var floorsById = floors.ToDictionary(f => f.Id);
        var roomsById = rooms.ToDictionary(r => r.Id);
        var windowsByRoomId = windows.ToLookup(w => w.RoomId);
        var wallsByRoomId = walls.ToLookup(w => w.RoomId);

        var roomRows = new List<BuildingRoomReportRowDto>();
        var roomCalculations = new Dictionary<int, RoomCalculationResult>();

        foreach (var room in rooms)
        {
            var calculation = _roomCalculationService.Calculate(
                room,
                windowsByRoomId[room.Id],
                wallsByRoomId[room.Id]);

            roomCalculations[room.Id] = calculation;
            var floorName = floorsById[room.FloorId].Name;

            roomRows.Add(new BuildingRoomReportRowDto
            {
                RoomId = room.Id,
                ProjectName = building.Project.Name,
                BuildingName = building.Name,
                FloorName = floorName,
                RoomName = room.Name,
                AreaM2 = room.AreaM2,
                HeightM = room.HeightM,
                VolumeM3 = room.VolumeM3,
                IndoorTemperatureC = room.IndoorTemperatureC,
                OutdoorTemperatureC = room.OutdoorTemperatureC,
                PeopleCount = room.PeopleCount,
                EquipmentLoadW = room.EquipmentLoadW,
                LightingLoadW = room.LightingLoadW,
                TotalWindowAreaM2 = calculation.TotalWindowAreaM2,
                TotalWallAreaM2 = calculation.TotalWallAreaM2,
                ExternalWallAreaM2 = calculation.ExternalWallAreaM2,
                BaseRoomLoadW = calculation.BaseRoomLoadW,
                WindowHeatGainW = calculation.WindowHeatGainW,
                WallHeatGainW = calculation.WallHeatGainW,
                InternalHeatGainW = calculation.InternalHeatGainW,
                TotalHeatLoadW = calculation.TotalHeatLoadW,
                TotalHeatLoadKw = calculation.TotalHeatLoadKw
            });
        }

        var floorSummaries = floors
            .Select(floor =>
            {
                var floorRooms = rooms.Where(room => room.FloorId == floor.Id).ToList();
                var totalHeatLoadW = floorRooms.Sum(room => roomCalculations[room.Id].TotalHeatLoadW);

                return new BuildingFloorSummaryDto
                {
                    FloorId = floor.Id,
                    FloorName = floor.Name,
                    RoomsCount = floorRooms.Count,
                    TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
                    TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2)
                };
            })
            .ToList();

        var windowRows = windows
            .Select(window =>
            {
                var room = roomsById[window.RoomId];
                var floor = floorsById[room.FloorId];

                return new WindowReportRowDto
                {
                    WindowId = window.Id,
                    RoomId = room.Id,
                    FloorName = floor.Name,
                    RoomName = room.Name,
                    AreaM2 = window.AreaM2
                };
            })
            .ToList();

        var wallRows = walls
            .Select(wall =>
            {
                var room = roomsById[wall.RoomId];
                var floor = floorsById[room.FloorId];

                return new WallReportRowDto
                {
                    WallId = wall.Id,
                    RoomId = room.Id,
                    FloorName = floor.Name,
                    RoomName = room.Name,
                    AreaM2 = wall.AreaM2,
                    IsExternal = wall.IsExternal
                };
            })
            .ToList();

        var totalHeatLoadW = roomCalculations.Values.Sum(calculation => calculation.TotalHeatLoadW);

        return new BuildingReportDto
        {
            ProjectName = building.Project.Name,
            BuildingName = building.Name,
            GeneratedAtUtc = DateTime.UtcNow,
            FloorsCount = floors.Count,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2),
            FloorSummaries = floorSummaries,
            Rooms = roomRows,
            Windows = windowRows,
            Walls = wallRows
        };
    }
}
