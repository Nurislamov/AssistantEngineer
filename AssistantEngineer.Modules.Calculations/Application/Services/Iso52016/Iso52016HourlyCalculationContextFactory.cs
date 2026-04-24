using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed class Iso52016HourlyCalculationContextFactory
{
    private readonly IBuildingEnvelopeReferenceData _envelopeReferenceData;
    private readonly Iso52016EnergyNeedOptions _options;

    public Iso52016HourlyCalculationContextFactory(
        IBuildingEnvelopeReferenceData envelopeReferenceData,
        Iso52016EnergyNeedOptions options)
    {
        _envelopeReferenceData = envelopeReferenceData;
        _options = options;
    }

    public Iso52016HourlyBuildingCalculationContext CreateBuildingContext(
        Building building,
        CalculationPreferences? preferences)
    {
        var zones = GetThermalZoneGroups(building);
        var zoneStates = zones.ToDictionary(
            zone => zone.Name,
            zone => CreateThermalZoneState(zone, preferences));

        var roomZoneMap = zones
            .SelectMany(zone => zone.Rooms.Select(room => new { room.Id, zone.Name }))
            .ToDictionary(x => x.Id, x => x.Name);

        var previousRoomTemperatures = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToDictionary(room => room.Id, room => room.IndoorTemperature.Celsius);

        return new Iso52016HourlyBuildingCalculationContext(
            zones,
            zoneStates,
            roomZoneMap,
            previousRoomTemperatures);
    }

    public Iso52016HourlyZoneCalculationContext CreateZoneCoolingContext(
        ThermalZone thermalZone,
        double coolingSetpoint)
    {
        var zone = new Iso52016ThermalZoneGroup(thermalZone.Name, thermalZone.AssignedRooms);
        var roomZoneMap = zone.Rooms.ToDictionary(room => room.Id, _ => zone.Name);
        var previousRoomTemperatures = zone.Rooms
            .ToDictionary(room => room.Id, room => room.IndoorTemperature.Celsius);

        return new Iso52016HourlyZoneCalculationContext(
            zone,
            CreateThermalZoneState(zone, preferences: null, coolingSetpoint),
            roomZoneMap,
            previousRoomTemperatures);
    }

    private Iso52016ThermalZoneState CreateThermalZoneState(
        Iso52016ThermalZoneGroup zone,
        CalculationPreferences? preferences,
        double? coolingSetpointOverride = null)
    {
        var rooms = zone.Rooms;
        var envelopeDefaults = _envelopeReferenceData.GetDefaults();
        var floorArea = rooms.Sum(room => room.Area.SquareMeters);
        var volume = rooms.Sum(room => room.CalculateVolume());

        var outdoorEnvelope = rooms.Sum(room =>
            GetOutdoorEnvelopeHeatTransferCoefficient(room, envelopeDefaults));
        var groundEnvelope = rooms.Sum(room =>
            GetGroundEnvelopeHeatTransferCoefficient(room, envelopeDefaults));
        var ventilation = rooms.Sum(room =>
        {
            var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ??
                                    GetDefaultAirChangesPerHour(preferences);
            var heatRecoveryFactor = 1 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0);

            return _options.AirHeatCapacityWhPerM3K *
                   airChangesPerHour *
                   room.CalculateVolume() *
                   heatRecoveryFactor;
        });

        var envelopeCapacity = rooms.Sum(room => room.CalculateInternalHeatCapacityKjPerK(
            envelopeDefaults.FloorHeatCapacityKjPerM2K,
            envelopeDefaults.CeilingHeatCapacityKjPerM2K) * 1000.0);

        var thermalCapacity = Math.Max(
            envelopeCapacity + floorArea * GetInternalHeatCapacityJPerM2K(preferences),
            Math.Max(floorArea, 1.0) * GetInternalHeatCapacityJPerM2K(preferences));

        var heatingSetpoint = Iso52016HourlyCalculatorMath.WeightedAverage(
            rooms,
            room => room.IndoorTemperature.Celsius);
        var coolingSetpoint = coolingSetpointOverride ?? Math.Max(_options.DefaultCoolingSetpointC, heatingSetpoint);

        return new Iso52016ThermalZoneState(
            FloorAreaM2: floorArea,
            VolumeM3: volume,
            OutdoorBoundaryHeatTransferCoefficientWPerK: outdoorEnvelope,
            GroundBoundaryHeatTransferCoefficientWPerK: groundEnvelope,
            VentilationHeatTransferCoefficientWPerK: ventilation,
            ThermalCapacityJPerK: thermalCapacity,
            HeatingSetpointC: heatingSetpoint,
            CoolingSetpointC: coolingSetpoint);
    }

    private double GetInternalHeatCapacityJPerM2K(CalculationPreferences? preferences) =>
        preferences?.Iso52016InternalHeatCapacityJPerM2K > 0
            ? preferences.Iso52016InternalHeatCapacityJPerM2K
            : _options.InternalHeatCapacityJPerM2K;

    private double GetDefaultAirChangesPerHour(CalculationPreferences? preferences) =>
        preferences?.Iso52016DefaultAirChangesPerHour >= 0
            ? preferences.Iso52016DefaultAirChangesPerHour
            : _options.DefaultAirChangesPerHour;

    private static IReadOnlyList<Iso52016ThermalZoneGroup> GetThermalZoneGroups(Building building)
    {
        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        if (building.ThermalZones.Count == 0)
            return [new Iso52016ThermalZoneGroup("Building", allRooms)];

        var countedRooms = new HashSet<Room>();
        var groups = new List<Iso52016ThermalZoneGroup>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var zoneRooms = zone.AssignedRooms
                .Where(countedRooms.Add)
                .ToArray();

            if (zoneRooms.Length > 0)
                groups.Add(new Iso52016ThermalZoneGroup(zone.Name, zoneRooms));
        }

        var unassignedRooms = allRooms
            .Where(room => !countedRooms.Contains(room))
            .ToArray();

        if (unassignedRooms.Length > 0)
            groups.Add(new Iso52016ThermalZoneGroup("Unassigned rooms", unassignedRooms));

        return groups;
    }

    private static double GetOutdoorEnvelopeHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * envelopeDefaults.CeilingUValueWPerM2K;

        return envelope;
    }

    private static double GetGroundEnvelopeHeatTransferCoefficient(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * Iso52016HourlyCalculatorMath.GetWallUValue(wall));

        envelope += room.Area.SquareMeters * envelopeDefaults.FloorUValueWPerM2K;

        return envelope;
    }
}
