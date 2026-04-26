using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.Modules.Reporting.Application.Models;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingCoolingReportGenerator
{
    private readonly TimeProvider _timeProvider;

    public BuildingCoolingReportGenerator(
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public BuildingCoolingReport Generate(
        BuildingCoolingReportData data)
    {
        var building = data.Building;
        var buildingCalculation = data.BuildingCalculation;
        var roomRows = CreateRoomRows(data);
        var roomsWithSelection = roomRows.Count(room => room.EquipmentSelected);

        return new BuildingCoolingReport
        {
            ProjectName = building.Project.Name,
            BuildingName = building.Name,
            CalculationMethod = buildingCalculation.CalculationMethod,
            PeakHour = buildingCalculation.PeakHour,
            GeneratedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,

            FloorsCount = buildingCalculation.FloorsCount,
            RoomsCount = buildingCalculation.RoomsCount,

            DesignReserveFactor = buildingCalculation.DesignReserveFactor,
            DesignCapacityW = buildingCalculation.DesignCapacityW,
            DesignCapacityKw = buildingCalculation.DesignCapacityKw,

            TotalHeatLoadW = buildingCalculation.TotalHeatLoadW,
            TotalHeatLoadKw = buildingCalculation.TotalHeatLoadKw,

            FloorSummaries = data.FloorCalculations
                .Select(ToFloorSummary)
                .ToList(),

            Rooms = roomRows,
            Windows = CreateWindowRows(data),
            Walls = CreateWallRows(data),

            EquipmentSelectionRequested = data.EquipmentSelectionRequested,
            RequestedSystemType = data.RequestedSystemType,
            RequestedUnitType = data.RequestedUnitType,

            RoomsWithSelectionCount = roomsWithSelection,
            RoomsWithoutSelectionCount = data.EquipmentSelectionRequested
                ? roomRows.Count - roomsWithSelection
                : 0,

            TotalSelectedCapacityKw = data.EquipmentSelectionRequested
                ? roomRows.Sum(room => room.SelectedNominalCoolingCapacityKw ?? 0)
                : null
        };
    }

    private static FloorCoolingReportSummary ToFloorSummary(
        FloorCalculationResult calculation) =>
        new()
        {
            FloorId = calculation.FloorId,
            FloorName = calculation.FloorName,
            RoomsCount = calculation.RoomsCount,
            TotalHeatLoadW = calculation.TotalHeatLoadW,
            TotalHeatLoadKw = calculation.TotalHeatLoadKw,
            DesignReserveFactor = calculation.DesignReserveFactor,
            DesignCapacityW = calculation.DesignCapacityW,
            DesignCapacityKw = calculation.DesignCapacityKw
        };

    private static List<RoomCoolingReportRow> CreateRoomRows(
        BuildingCoolingReportData data) =>
        data.RoomCalculations
            .Select(item =>
            {
                var calculation = item.Calculation;
                var selection = item.EquipmentSelection;

                return new RoomCoolingReportRow
                {
                    RoomId = item.Room.Id,
                    CalculationMethod = calculation.CalculationMethod,
                    PeakHour = calculation.PeakHour,

                    ProjectName = data.Building.Project.Name,
                    BuildingName = data.Building.Name,
                    FloorName = item.Floor.Name,
                    RoomName = item.Room.Name,

                    AreaM2 = calculation.AreaM2,
                    HeightM = calculation.HeightM,
                    VolumeM3 = calculation.VolumeM3,

                    IndoorTemperatureC = calculation.IndoorTemperatureC,
                    OutdoorTemperatureC = calculation.OutdoorTemperatureC,

                    PeopleCount = calculation.PeopleCount,
                    EquipmentLoadW = calculation.EquipmentLoadW,
                    LightingLoadW = calculation.LightingLoadW,

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

                    RequestedSystemType = data.RequestedSystemType,
                    RequestedUnitType = data.RequestedUnitType,

                    SelectedCatalogItemId = selection?.SelectedCatalogItemId,
                    SelectedManufacturer = selection?.SelectedManufacturer ?? string.Empty,
                    SelectedModelName = selection?.SelectedModelName ?? string.Empty,
                    SelectedNominalCoolingCapacityKw = selection?.SelectedNominalCoolingCapacityKw,
                    SelectionReserveKw = selection?.CapacityReserveKw,
                    EquipmentSelected = selection is not null
                };
            })
            .ToList();

    private static List<WindowCoolingReportRow> CreateWindowRows(
        BuildingCoolingReportData data) =>
        data.RoomCalculations
            .SelectMany(item => item.Room.Windows
                .OrderBy(window => window.Id)
                .Select(window => new WindowCoolingReportRow
                {
                    WindowId = window.Id,
                    RoomId = item.Room.Id,
                    FloorName = item.Floor.Name,
                    RoomName = item.Room.Name,
                    AreaM2 = window.Area.SquareMeters
                }))
            .ToList();

    private static List<WallCoolingReportRow> CreateWallRows(
        BuildingCoolingReportData data) =>
        data.RoomCalculations
            .SelectMany(item => item.Room.Walls
                .OrderBy(wall => wall.Id)
                .Select(wall => new WallCoolingReportRow
                {
                    WallId = wall.Id,
                    RoomId = item.Room.Id,
                    FloorName = item.Floor.Name,
                    RoomName = item.Room.Name,
                    AreaM2 = wall.Area.SquareMeters,
                    IsExternal = wall.IsExternal
                }))
            .ToList();
}