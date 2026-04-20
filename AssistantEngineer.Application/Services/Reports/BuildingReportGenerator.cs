using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Reports;

namespace AssistantEngineer.Application.Services.Reports;

public sealed class BuildingReportGenerator
{
    private readonly TimeProvider _timeProvider;

    public BuildingReportGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public BuildingReport GenerateCoolingReport(BuildingCoolingReportData data)
    {
        var building = data.Building;
        var buildingCalculation = data.BuildingCalculation;
        var roomRows = CreateRoomRows(data);
        var roomsWithSelection = roomRows.Count(room => room.EquipmentSelected);

        return new BuildingReport
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
            FloorSummaries = data.FloorCalculations.Select(ToFloorSummary).ToList(),
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

    public HeatingReport GenerateHeatingReport(BuildingHeatingReportData data)
    {
        var rooms = data.RoomCalculations;
        var transmissionLoss = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilationLoss = rooms.Sum(room => room.VentilationHeatLossW);
        var totalLoad = transmissionLoss + ventilationLoss;
        var outdoorTemp = WeightedAverage(
            rooms,
            room => room.OutdoorDesignTemperatureC,
            room => room.TotalDesignHeatingLoadW);
        var indoorTemp = WeightedAverage(
            rooms,
            room => room.IndoorDesignTemperatureC,
            room => room.TotalDesignHeatingLoadW);

        return new HeatingReport
        {
            ProjectName = data.Building.Project.Name,
            BuildingName = data.Building.Name,
            CalculationMethod = data.Method.ToString(),
            GeneratedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            OutdoorDesignTemperatureC = outdoorTemp,
            IndoorDesignTemperatureC = indoorTemp,
            RoomsCount = rooms.Count,
            TotalTransmissionLossW = Math.Round(transmissionLoss, 2, MidpointRounding.AwayFromZero),
            TotalVentilationLossW = Math.Round(ventilationLoss, 2, MidpointRounding.AwayFromZero),
            TotalDesignHeatingLoadW = Math.Round(totalLoad, 2, MidpointRounding.AwayFromZero),
            TotalDesignHeatingLoadKw = Math.Round(totalLoad / 1000.0, 2, MidpointRounding.AwayFromZero),
            Rooms = rooms.ToList()
        };
    }

    private static double WeightedAverage(
        IReadOnlyCollection<RoomHeatingLoadResult> rooms,
        Func<RoomHeatingLoadResult, double> valueSelector,
        Func<RoomHeatingLoadResult, double> weightSelector)
    {
        if (rooms.Count == 0)
            return 0;

        var totalWeight = rooms.Sum(weightSelector);
        if (totalWeight <= 0)
            return Math.Round(rooms.Average(valueSelector), 2, MidpointRounding.AwayFromZero);

        return Math.Round(
            rooms.Sum(room => valueSelector(room) * weightSelector(room)) / totalWeight,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static FloorReportSummary ToFloorSummary(FloorCalculationResult calculation) =>
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

    private static List<RoomReportRow> CreateRoomRows(BuildingCoolingReportData data) =>
        data.RoomCalculations
            .Select(item =>
            {
                var calculation = item.Calculation;
                var selection = item.EquipmentSelection;
                return new RoomReportRow
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

    private static List<WindowReportRow> CreateWindowRows(BuildingCoolingReportData data) =>
        data.RoomCalculations
            .SelectMany(item => item.Room.Windows
                .OrderBy(window => window.Id)
                .Select(window => new WindowReportRow
                {
                    WindowId = window.Id,
                    RoomId = item.Room.Id,
                    FloorName = item.Floor.Name,
                    RoomName = item.Room.Name,
                    AreaM2 = window.Area.SquareMeters
                }))
            .ToList();

    private static List<WallReportRow> CreateWallRows(BuildingCoolingReportData data) =>
        data.RoomCalculations
            .SelectMany(item => item.Room.Walls
                .OrderBy(wall => wall.Id)
                .Select(wall => new WallReportRow
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
