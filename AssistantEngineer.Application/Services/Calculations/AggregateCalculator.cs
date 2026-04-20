using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Models.ThermalZones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Calculations;

public interface IAggregateLoadCalculator
{
    Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);

    Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

public sealed class AggregateCalculator : IAggregateLoadCalculator
{
    private readonly IRoomCoolingLoadCalculator _roomCoolingLoadCalculator;
    private readonly CoolingLoadCalculationOptions _options;
    private readonly IHourlyProfileAggregator _profileAggregator;
    private readonly ILogger<AggregateCalculator> _logger;

    public AggregateCalculator(
        IRoomCoolingLoadCalculator roomCoolingLoadCalculator,
        CoolingLoadCalculationOptions options,
        IHourlyProfileAggregator profileAggregator,
        ILogger<AggregateCalculator>? logger = null)
    {
        _roomCoolingLoadCalculator = roomCoolingLoadCalculator;
        _options = options;
        _profileAggregator = profileAggregator;
        _logger = logger ?? NullLogger<AggregateCalculator>.Instance;
    }

    public Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default) =>
        CalculateFloorAsync(floor, CoolingLoadCalculationMethod.Simplified, preferences, cancellationToken);

    public async Task<FloorCalculationResult> CalculateFloorAsync(
        Floor floor,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Aggregate cooling calculation started for floor {FloorId} using {CalculationMethod}.",
            floor.Id,
            method);

        var roomCount = 0;
        var roomProfiles = new List<IReadOnlyList<double>>();
        foreach (var room in floor.Rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roomResult = await _roomCoolingLoadCalculator.CalculateAsync(room, method, preferences, cancellationToken);
            roomCount++;
            roomProfiles.Add(roomResult.HourlyHeatLoadW);
        }

        var hourlyHeatLoad = _profileAggregator.SumProfiles(
            roomProfiles,
            cancellationToken);
        var totalHeatLoad = hourlyHeatLoad.Count > 0 ? hourlyHeatLoad.Max() : 0;
        var peakHour = hourlyHeatLoad.Count > 0 ? _profileAggregator.FindPeakHour(hourlyHeatLoad) : 0;
        var reserveFactor = GetReserveFactor(preferences);
        var designLoad = totalHeatLoad * reserveFactor;

        _logger.LogDebug(
            "Aggregate cooling calculation finished for floor {FloorId}: rooms {RoomCount}, design capacity {DesignCapacityKw} kW.",
            floor.Id,
            roomCount,
            Round(designLoad / 1000.0));

        return new FloorCalculationResult
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            CalculationMethod = method.ToString(),
            PeakHour = peakHour,
            RoomsCount = roomCount,
            TotalHeatLoadW = Round(totalHeatLoad),
            TotalHeatLoadKw = Round(totalHeatLoad / 1000.0),
            DesignReserveFactor = reserveFactor,
            DesignCapacityW = Round(designLoad),
            DesignCapacityKw = Round(designLoad / 1000.0),
            HourlyHeatLoadW = hourlyHeatLoad
        };
    }

    public Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default) =>
        CalculateBuildingAsync(building, CoolingLoadCalculationMethod.Simplified, preferences, cancellationToken);

    public async Task<BuildingCalculationResult> CalculateBuildingAsync(
        Building building,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        if (method == CoolingLoadCalculationMethod.Iso52016 && building.ThermalZones.Count > 0)
        {
            _logger.LogInformation(
                "Aggregate cooling calculation for building {BuildingId} will use {ThermalZoneCount} thermal zones.",
                building.Id,
                building.ThermalZones.Count);
            return await CalculateBuildingByThermalZonesAsync(building, method, preferences, cancellationToken);
        }

        _logger.LogDebug(
            "Aggregate cooling calculation started for building {BuildingId} using {CalculationMethod}.",
            building.Id,
            method);

        var floorCount = 0;
        var roomCount = 0;
        var floorProfiles = new List<IReadOnlyList<double>>();
        foreach (var floor in building.Floors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var floorResult = await CalculateFloorAsync(floor, method, preferences, cancellationToken);
            floorCount++;
            roomCount += floorResult.RoomsCount;
            floorProfiles.Add(floorResult.HourlyHeatLoadW);
        }

        var hourlyHeatLoad = _profileAggregator.SumProfiles(
            floorProfiles,
            cancellationToken);
        var totalHeatLoad = hourlyHeatLoad.Count > 0 ? hourlyHeatLoad.Max() : 0;
        var peakHour = hourlyHeatLoad.Count > 0 ? _profileAggregator.FindPeakHour(hourlyHeatLoad) : 0;
        var reserveFactor = GetReserveFactor(preferences);
        var designLoad = totalHeatLoad * reserveFactor;

        _logger.LogDebug(
            "Aggregate cooling calculation finished for building {BuildingId}: floors {FloorCount}, rooms {RoomCount}, design capacity {DesignCapacityKw} kW.",
            building.Id,
            floorCount,
            roomCount,
            Round(designLoad / 1000.0));

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = method.ToString(),
            PeakHour = peakHour,
            FloorsCount = floorCount,
            RoomsCount = roomCount,
            TotalHeatLoadW = Round(totalHeatLoad),
            TotalHeatLoadKw = Round(totalHeatLoad / 1000.0),
            DesignReserveFactor = reserveFactor,
            DesignCapacityW = Round(designLoad),
            DesignCapacityKw = Round(designLoad / 1000.0),
            HourlyHeatLoadW = hourlyHeatLoad
        };
    }

    private async Task<BuildingCalculationResult> CalculateBuildingByThermalZonesAsync(
        Building building,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Thermal zone aggregate calculation started for building {BuildingId}.",
            building.Id);

        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();
        var roomsById = allRooms
            .Where(room => room.Id > 0)
            .ToDictionary(room => room.Id);
        var countedRoomIds = new HashSet<int>();
        var zoneResults = new List<ThermalZoneCalculationResult>(building.ThermalZones.Count + 1);

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var zoneRooms = zone.RoomIds
                .Where(countedRoomIds.Add)
                .Select(roomId => roomsById.TryGetValue(roomId, out var room) ? room : null)
                .Where(room => room is not null)
                .Select(room => room!)
                .ToArray();

            if (zoneRooms.Length == 0)
                continue;

            zoneResults.Add(await CalculateThermalZoneAsync(zone, zoneRooms, method, preferences, cancellationToken));
        }

        var unassignedRooms = allRooms
            .Where(room => room.Id <= 0 || !countedRoomIds.Contains(room.Id))
            .ToArray();
        if (unassignedRooms.Length > 0)
        {
            zoneResults.Add(await CalculateUnassignedRoomsZoneAsync(
                unassignedRooms,
                method,
                preferences,
                cancellationToken));
        }

        var hourlyHeatLoad = _profileAggregator.SumProfiles(
            zoneResults.Select(zone => zone.HourlyHeatLoadW),
            cancellationToken);
        var totalHeatLoad = hourlyHeatLoad.Count > 0 ? hourlyHeatLoad.Max() : 0;
        var peakHour = hourlyHeatLoad.Count > 0 ? _profileAggregator.FindPeakHour(hourlyHeatLoad) : 0;
        var reserveFactor = GetReserveFactor(preferences);
        var designLoad = totalHeatLoad * reserveFactor;

        _logger.LogDebug(
            "Thermal zone aggregate calculation finished for building {BuildingId}: zones {ZoneCount}, rooms {RoomCount}.",
            building.Id,
            zoneResults.Count,
            allRooms.Length);

        return new BuildingCalculationResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CalculationMethod = method.ToString(),
            PeakHour = peakHour,
            FloorsCount = building.Floors.Count,
            RoomsCount = allRooms.Length,
            TotalHeatLoadW = Round(totalHeatLoad),
            TotalHeatLoadKw = Round(totalHeatLoad / 1000.0),
            DesignReserveFactor = reserveFactor,
            DesignCapacityW = Round(designLoad),
            DesignCapacityKw = Round(designLoad / 1000.0),
            HourlyHeatLoadW = hourlyHeatLoad,
            ThermalZones = zoneResults
        };
    }

    private async Task<ThermalZoneCalculationResult> CalculateThermalZoneAsync(
        ThermalZone zone,
        IReadOnlyCollection<Room> rooms,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        var result = await CalculateThermalZoneRoomsAsync(rooms, method, preferences, cancellationToken);
        result.ThermalZoneId = zone.Id;
        result.ThermalZoneName = zone.Name;
        result.RoomIds = rooms.Select(room => room.Id).ToList();
        return result;
    }

    private async Task<ThermalZoneCalculationResult> CalculateUnassignedRoomsZoneAsync(
        IReadOnlyCollection<Room> rooms,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        var result = await CalculateThermalZoneRoomsAsync(rooms, method, preferences, cancellationToken);
        result.ThermalZoneName = "Unassigned rooms";
        result.IsUnassignedRoomsZone = true;
        result.RoomIds = rooms.Select(room => room.Id).ToList();
        return result;
    }

    private async Task<ThermalZoneCalculationResult> CalculateThermalZoneRoomsAsync(
        IReadOnlyCollection<Room> rooms,
        CoolingLoadCalculationMethod method,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        var roomCount = 0;
        var roomProfiles = new List<IReadOnlyList<double>>();
        foreach (var room in rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roomResult = await _roomCoolingLoadCalculator.CalculateAsync(room, method, preferences, cancellationToken);
            roomCount++;
            roomProfiles.Add(roomResult.HourlyHeatLoadW);
        }

        var hourlyHeatLoad = _profileAggregator.SumProfiles(
            roomProfiles,
            cancellationToken);
        var totalHeatLoad = hourlyHeatLoad.Count > 0 ? hourlyHeatLoad.Max() : 0;
        var peakHour = hourlyHeatLoad.Count > 0 ? _profileAggregator.FindPeakHour(hourlyHeatLoad) : 0;

        return new ThermalZoneCalculationResult
        {
            RoomsCount = roomCount,
            PeakHour = peakHour,
            TotalHeatLoadW = Round(totalHeatLoad),
            TotalHeatLoadKw = Round(totalHeatLoad / 1000.0),
            HourlyHeatLoadW = hourlyHeatLoad
        };
    }

    private double GetReserveFactor(CalculationPreferences? preferences) =>
        preferences?.CoolingSafetyFactor ?? _options.DefaultCoolingSafetyFactor;

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}


