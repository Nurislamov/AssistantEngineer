using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Reports;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Domain.Equipment;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Domain.Services.Equipment;
using AssistantEngineer.Services.Calculations;

namespace AssistantEngineer.Services.Reports;

public class BuildingReportDataService
{
    private readonly IAppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;
    private readonly CoolingEquipmentSelector _coolingEquipmentSelector;

    public BuildingReportDataService(
        IAppDbContext context,
        RoomCalculationService roomCalculationService,
        CoolingEquipmentSelector coolingEquipmentSelector)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
        _coolingEquipmentSelector = coolingEquipmentSelector;
    }

    public async Task<BuildingReport?> BuildReportAsync(
        int buildingId,
        string? systemType = null,
        string? unitType = null)
    {
        var requestedSystemType = systemType?.Trim();
        var requestedUnitType = unitType?.Trim();
        var equipmentSelectionRequested =
            !string.IsNullOrWhiteSpace(requestedSystemType) &&
            !string.IsNullOrWhiteSpace(requestedUnitType);

        var building = _context.Buildings
            .FirstOrDefault(b => b.Id == buildingId);

        if (building == null)
            return null;

        var project = _context.Projects
            .First(project => project.Id == building.ProjectId);

        var floors = _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .OrderBy(f => f.Id)
            .ToList();

        var floorIds = floors
            .Select(f => f.Id)
            .ToList();

        var rooms = _context.Rooms
            .Where(r => floorIds.Contains(r.FloorId))
            .OrderBy(r => r.FloorId)
            .ThenBy(r => r.Id)
            .ToList();

        var roomIds = rooms
            .Select(r => r.Id)
            .ToList();

        var windows = _context.Windows
            .Where(w => roomIds.Contains(w.RoomId))
            .OrderBy(w => w.RoomId)
            .ThenBy(w => w.Id)
            .ToList();

        var walls = _context.Walls
            .Where(w => roomIds.Contains(w.RoomId))
            .OrderBy(w => w.RoomId)
            .ThenBy(w => w.Id)
            .ToList();

        var floorsById = floors.ToDictionary(f => f.Id);
        var roomsById = rooms.ToDictionary(r => r.Id);
        var windowsByRoomId = windows.ToLookup(w => w.RoomId);
        var wallsByRoomId = walls.ToLookup(w => w.RoomId);

        List<CoolingEquipmentCatalogItem> equipmentCatalogItems = equipmentSelectionRequested
            ? _context.EquipmentCatalogItems
                .Where(item =>
                    item.IsActive &&
                    item.SystemType == requestedSystemType &&
                    item.UnitType == requestedUnitType)
                .OrderBy(item => item.NominalCoolingCapacityKw)
                .ToList()
            : [];

        var roomRows = new List<RoomReportRow>();
        var roomCalculations = new Dictionary<int, RoomCalculationResult>();

        foreach (var room in rooms)
        {
            var calculation = _roomCalculationService.Calculate(
                room,
                windowsByRoomId[room.Id],
                wallsByRoomId[room.Id]);

            roomCalculations[room.Id] = calculation;
            var floorName = floorsById[room.FloorId].Name;

            var selectedItem = equipmentSelectionRequested
                ? _coolingEquipmentSelector.SelectSmallestSuitable(
                    equipmentCatalogItems,
                    calculation.DesignCapacityKw)
                : null;

            roomRows.Add(new RoomReportRow
            {
                RoomId = room.Id,
                ProjectName = project.Name,
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
                TotalHeatLoadKw = calculation.TotalHeatLoadKw,
                DesignReserveFactor = calculation.DesignReserveFactor,
                DesignCapacityW = calculation.DesignCapacityW,
                DesignCapacityKw = calculation.DesignCapacityKw,
                RequestedSystemType = equipmentSelectionRequested ? requestedSystemType! : string.Empty,
                RequestedUnitType = equipmentSelectionRequested ? requestedUnitType! : string.Empty,
                SelectedCatalogItemId = selectedItem?.Id,
                SelectedManufacturer = selectedItem?.Manufacturer ?? string.Empty,
                SelectedModelName = selectedItem?.ModelName ?? string.Empty,
                SelectedNominalCoolingCapacityKw = selectedItem?.NominalCoolingCapacityKw,
                SelectionReserveKw = selectedItem == null
                    ? null
                    : Math.Round(selectedItem.NominalCoolingCapacityKw - calculation.DesignCapacityKw, 2),
                EquipmentSelected = selectedItem != null
            });
        }

        var floorSummaries = floors
            .Select(floor =>
            {
                var floorRooms = rooms.Where(room => room.FloorId == floor.Id).ToList();
                var totalHeatLoadW = floorRooms.Sum(room => roomCalculations[room.Id].TotalHeatLoadW);
                var totalDesignCapacityW = floorRooms.Sum(room => roomCalculations[room.Id].DesignCapacityW);

                return new FloorReportSummary
                {
                    FloorId = floor.Id,
                    FloorName = floor.Name,
                    RoomsCount = floorRooms.Count,
                    DesignReserveFactor = RoomCalculationService.DefaultDesignReserveFactor,
                    DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
                    DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2),
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

                return new WindowReportRow
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

                return new WallReportRow
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
        var totalDesignCapacityW = roomCalculations.Values.Sum(calculation => calculation.DesignCapacityW);

        var roomsWithSelectionCount = roomRows.Count(r => r.EquipmentSelected);
        var roomsWithoutSelectionCount = equipmentSelectionRequested
            ? roomRows.Count - roomsWithSelectionCount
            : 0;

        double? totalSelectedCapacityKw = equipmentSelectionRequested
            ? Math.Round(
                roomRows
                    .Where(r => r.SelectedNominalCoolingCapacityKw.HasValue)
                    .Sum(r => r.SelectedNominalCoolingCapacityKw!.Value),
                2)
            : null;

        return new BuildingReport
        {
            ProjectName = project.Name,
            BuildingName = building.Name,
            GeneratedAtUtc = DateTime.UtcNow,
            FloorsCount = floors.Count,
            RoomsCount = rooms.Count,
            TotalHeatLoadW = Math.Round(totalHeatLoadW, 2),
            TotalHeatLoadKw = Math.Round(totalHeatLoadW / 1000.0, 2),
            DesignReserveFactor = RoomCalculationService.DefaultDesignReserveFactor,
            DesignCapacityW = Math.Round(totalDesignCapacityW, 2),
            DesignCapacityKw = Math.Round(totalDesignCapacityW / 1000.0, 2),
            FloorSummaries = floorSummaries,
            Rooms = roomRows,
            Windows = windowRows,
            Walls = wallRows,
            EquipmentSelectionRequested = equipmentSelectionRequested,
            RequestedSystemType = equipmentSelectionRequested ? requestedSystemType! : string.Empty,
            RequestedUnitType = equipmentSelectionRequested ? requestedUnitType! : string.Empty,
            RoomsWithSelectionCount = roomsWithSelectionCount,
            RoomsWithoutSelectionCount = roomsWithoutSelectionCount,
            TotalSelectedCapacityKw = totalSelectedCapacityKw
        };
    }

}
