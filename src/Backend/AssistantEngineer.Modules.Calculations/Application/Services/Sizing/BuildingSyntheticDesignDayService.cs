using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class BuildingSyntheticDesignDayService
{
    private readonly IBuildingRepository _buildings;
    private readonly IBuildingEnvelopeReferenceData _envelopeReferenceData;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly INaturalVentilationAirflowService? _naturalVentilationAirflowService;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;
    private readonly WindowSolarGainEngine _windowSolarGains;

    public BuildingSyntheticDesignDayService(
        IBuildingRepository buildings,
        IBuildingEnvelopeReferenceData envelopeReferenceData,
        IEn16798ProfileCatalog profileCatalog,
        INaturalVentilationAirflowService? naturalVentilationAirflowService,
        IOptions<Iso52016EnergyNeedOptions> energyOptions,
        IOptions<En16798ProfileOptions> profileOptions,
        WindowSolarGainEngine? windowSolarGains = null)
    {
        _buildings = buildings;
        _envelopeReferenceData = envelopeReferenceData;
        _profileCatalog = profileCatalog;
        _naturalVentilationAirflowService = naturalVentilationAirflowService;
        _energyOptions = energyOptions.Value;
        _profileOptions = profileOptions.Value;
        _windowSolarGains = windowSolarGains ?? new WindowSolarGainEngine();
    }

    public async Task<Result<BuildingSyntheticDesignDayResponse>> CalculateAsync(
        int buildingId,
        SyntheticDesignDayRequest request,
        CancellationToken cancellationToken)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingSyntheticDesignDayResponse>.NotFound($"Building with id {buildingId} not found.");

        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        if (allRooms.Length == 0)
            return Result<BuildingSyntheticDesignDayResponse>.Validation("Building must contain at least one room.");

        var zoneGroups = GetZoneGroups(building);

        var roomProfiles = new List<RoomSyntheticHour>(capacity: allRooms.Length * 24);

        for (var hourOfDay = 0; hourOfDay < 24; hourOfDay++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outdoorTemperature = GetOutdoorTemperature(
                request.Mode,
                request.DesignOutdoorDryBulbC,
                request.OutdoorDailyRangeC,
                hourOfDay);

            var solarHorizontal = GetSolarHorizontalProfile(
                request.Mode,
                request.SolarPeakWPerM2,
                hourOfDay);

            foreach (var zone in zoneGroups)
            {
                foreach (var room in zone.Rooms)
                {
                    roomProfiles.Add(CalculateRoomHour(
                        room,
                        zone.Name,
                        request,
                        hourOfDay,
                        outdoorTemperature,
                        solarHorizontal));
                }
            }
        }

        var response = new BuildingSyntheticDesignDayResponse
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            Mode = request.Mode,
            DayOfYear = request.DayOfYear,
            Month = GetMonthFromDayOfYear(request.DayOfYear),
            DesignOutdoorDryBulbC = request.DesignOutdoorDryBulbC,
            OutdoorDailyRangeC = request.OutdoorDailyRangeC,
            WindSpeedMPerS = request.WindSpeedMPerS,
            SolarPeakWPerM2 = request.SolarPeakWPerM2,
            CoolingSetpointC = request.CoolingSetpointC,
            HeatingSetpointC = request.HeatingSetpointC,
            UseRoomSchedules = request.UseRoomSchedules,
            IncludeInternalGains = request.IncludeInternalGains,
            IncludeSolarGains = request.IncludeSolarGains,
            UseNaturalVentilation = request.UseNaturalVentilation
        };

        response.Building = BuildScopeResponse(
            roomProfiles,
            scopeId: null,
            scopeName: building.Name,
            parentScopeName: null,
            safetyFactor: GetSafetyFactor(request));

        response.Zones = roomProfiles
            .GroupBy(x => x.ZoneName)
            .OrderBy(x => x.Key)
            .Select(group => BuildScopeResponse(
                group,
                scopeId: zoneGroups.FirstOrDefault(z => z.Name == group.Key)?.ZoneId,
                scopeName: group.Key,
                parentScopeName: null,
                safetyFactor: GetSafetyFactor(request)))
            .Where(x => x is not null)
            .Cast<SyntheticDesignDayScopeResponse>()
            .ToList();

        response.Rooms = roomProfiles
            .GroupBy(x => x.RoomId)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var first = group.First();
                return BuildScopeResponse(
                    group,
                    scopeId: first.RoomId,
                    scopeName: first.RoomName,
                    parentScopeName: first.ZoneName,
                    safetyFactor: GetSafetyFactor(request));
            })
            .Where(x => x is not null)
            .Cast<SyntheticDesignDayScopeResponse>()
            .ToList();

        return Result<BuildingSyntheticDesignDayResponse>.Success(response);
    }

    private RoomSyntheticHour CalculateRoomHour(
        Room room,
        string zoneName,
        SyntheticDesignDayRequest request,
        int hourOfDay,
        double outdoorTemperatureC,
        double solarHorizontalWPerM2)
    {
        var envelopeDefaults = _envelopeReferenceData.GetDefaults();
        var operativeTemperatureC = request.Mode == ReferenceDesignDayMode.Cooling
            ? request.CoolingSetpointC
            : request.HeatingSetpointC;

        var occupancyFactor = GetOccupancyFactor(room, hourOfDay, request.UseRoomSchedules);
        var equipmentFactor = GetEquipmentFactor(room, hourOfDay, request.UseRoomSchedules);
        var lightingFactor = GetLightingFactor(room, hourOfDay, request.UseRoomSchedules);
        var ventilationFactor = GetVentilationFactor(room, hourOfDay, request.UseRoomSchedules);

        var internalGainsW = request.IncludeInternalGains
            ? room.PeopleCount * GetPeopleHeatGain(room.Type) * occupancyFactor +
              room.EquipmentLoad.Watts * equipmentFactor +
              room.LightingLoad.Watts * lightingFactor
            : 0.0;

        var solarGainsW = request.IncludeSolarGains
            ? room.Windows.Sum(window =>
            {
                var incidentIrradiance =
                    solarHorizontalWPerM2 *
                    GetFacadeSolarMultiplier(window.Orientation, hourOfDay);
                var solar = _windowSolarGains.Calculate(
                    WindowSolarGainInputFactory.CreateForWindow(
                        window,
                        incidentIrradiance,
                        externalShadingFactor: GetSimpleShadingReduction(window),
                        hourIndex: hourOfDay,
                        isNight: solarHorizontalWPerM2 <= 0));

                return solar.Value.SolarGainW;
            })
            : 0.0;

        var outdoorUa = GetOutdoorEnvelopeHeatTransferCoefficient(room, envelopeDefaults);
        var groundUa = GetGroundEnvelopeHeatTransferCoefficient(room, envelopeDefaults);
        var adjacentContribution = GetAdjacentBoundaryContribution(room, operativeTemperatureC, outdoorTemperatureC);

        var ventilationUa = GetVentilationHeatTransferCoefficient(
            room,
            operativeTemperatureC,
            outdoorTemperatureC,
            request.WindSpeedMPerS,
            ventilationFactor,
            hourOfDay,
            request.UseNaturalVentilation);

        var sensibleBalanceW =
            outdoorUa * (outdoorTemperatureC - operativeTemperatureC) +
            groundUa * (request.GroundBoundaryTemperatureC - operativeTemperatureC) +
            adjacentContribution +
            ventilationUa * (outdoorTemperatureC - operativeTemperatureC) +
            internalGainsW +
            solarGainsW;

        var loadW = request.Mode == ReferenceDesignDayMode.Cooling
            ? Math.Max(0, sensibleBalanceW)
            : Math.Max(0, -sensibleBalanceW);

        return new RoomSyntheticHour(
            RoomId: room.Id,
            RoomName: room.Name,
            ZoneName: zoneName,
            HourOfDay: hourOfDay,
            DayOfYear: request.DayOfYear,
            Month: GetMonthFromDayOfYear(request.DayOfYear),
            OutdoorTemperatureC: outdoorTemperatureC,
            SolarHorizontalWPerM2: solarHorizontalWPerM2,
            LoadW: Round(loadW),
            OperativeTemperatureC: Round(operativeTemperatureC),
            InternalGainsW: Round(internalGainsW),
            SolarGainsW: Round(solarGainsW));
    }

    private double GetVentilationHeatTransferCoefficient(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        double ventilationFactor,
        int hourOfDay,
        bool useNaturalVentilation)
    {
        var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ??
            (_energyOptions.DefaultAirChangesPerHour >= 0 ? _energyOptions.DefaultAirChangesPerHour : 0.5);

        var infiltrationAch = room.VentilationParameters?.InfiltrationAirChangesPerHour ?? 0.0;
        var heatRecoveryFactor = 1.0 - (room.VentilationParameters?.HeatRecoveryEfficiency ?? 0.0);
        var airHeatCapacityWhPerM3K = AirPhysicalConstants.AirHeatCapacityWhPerM3K;

        var mechanicalUa =
            airHeatCapacityWhPerM3K *
            airChangesPerHour *
            room.CalculateVolume() *
            heatRecoveryFactor *
            ventilationFactor;

        var infiltrationUa =
            airHeatCapacityWhPerM3K *
            infiltrationAch *
            room.CalculateVolume();

        var naturalUa = useNaturalVentilation
            ? _naturalVentilationAirflowService?.CalculateHeatTransferCoefficient(
                room,
                indoorTemperatureC,
                outdoorTemperatureC,
                windSpeedMPerS,
                ventilationFactor,
                hourOfDay) ?? 0.0
            : 0.0;

        return mechanicalUa + infiltrationUa + naturalUa;
    }

    private double GetAdjacentBoundaryContribution(
        Room room,
        double indoorTemperatureC,
        double outdoorTemperatureC)
    {
        var sum = 0.0;

        foreach (var wall in room.Walls)
        {
            var ua = wall.Area.SquareMeters * GetWallUValue(wall);

            switch (wall.BoundaryType)
            {
                case WallBoundaryType.Adiabatic:
                case WallBoundaryType.AdjacentConditioned:
                    break;

                case WallBoundaryType.AdjacentUnconditioned:
                {
                    var adjacentTemp = (indoorTemperatureC + outdoorTemperatureC) / 2.0;
                    sum += ua * (adjacentTemp - indoorTemperatureC);
                    break;
                }
            }
        }

        return sum;
    }

    private double GetOccupancyFactor(Room room, int hourOfDay, bool useRoomSchedules)
    {
        if (useRoomSchedules && room.OccupancySchedule?.Factors.Count == 24)
            return room.OccupancySchedule.Factors[hourOfDay];

        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return profile.OccupancyFactors[hourOfDay];
    }

    private double GetEquipmentFactor(Room room, int hourOfDay, bool useRoomSchedules)
    {
        if (useRoomSchedules && room.EquipmentSchedule?.Factors.Count == 24)
            return room.EquipmentSchedule.Factors[hourOfDay];

        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return profile.EquipmentFactors[hourOfDay];
    }

    private double GetLightingFactor(Room room, int hourOfDay, bool useRoomSchedules)
    {
        if (useRoomSchedules && room.LightingSchedule?.Factors.Count == 24)
            return room.LightingSchedule.Factors[hourOfDay];

        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return profile.LightingFactors[hourOfDay];
    }

    private double GetVentilationFactor(Room room, int hourOfDay, bool useRoomSchedules)
    {
        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return profile.VentilationFactors[hourOfDay];
    }

    private static double GetOutdoorTemperature(
        ReferenceDesignDayMode mode,
        double designDryBulbC,
        double dailyRangeC,
        int hourOfDay)
    {
        var amplitude = dailyRangeC / 2.0;

        if (mode == ReferenceDesignDayMode.Cooling)
        {
            var mean = designDryBulbC - amplitude;
            return mean + amplitude * Math.Cos(2 * Math.PI * (hourOfDay - 15) / 24.0);
        }

        var heatingMean = designDryBulbC + amplitude;
        return heatingMean - amplitude * Math.Cos(2 * Math.PI * (hourOfDay - 5) / 24.0);
    }

    private static double GetSolarHorizontalProfile(
        ReferenceDesignDayMode mode,
        double solarPeakWPerM2,
        int hourOfDay)
    {
        var normalized = Math.Max(0.0, Math.Sin(Math.PI * (hourOfDay - 6) / 12.0));
        var seasonalFactor = mode == ReferenceDesignDayMode.Cooling ? 1.0 : 0.35;
        return solarPeakWPerM2 * normalized * seasonalFactor;
    }

    private static double GetFacadeSolarMultiplier(CardinalDirection orientation, int hourOfDay)
    {
        var north = 0.20 * Math.Max(0.0, Math.Sin(Math.PI * (hourOfDay - 6) / 12.0));
        var east = Math.Exp(-Math.Pow((hourOfDay - 9.0) / 3.0, 2));
        var south = Math.Exp(-Math.Pow((hourOfDay - 13.0) / 3.5, 2));
        var west = Math.Exp(-Math.Pow((hourOfDay - 17.0) / 3.0, 2));

        return orientation switch
        {
            CardinalDirection.North => north,
            CardinalDirection.NorthEast => Math.Max(north, east * 0.8),
            CardinalDirection.East => east,
            CardinalDirection.SouthEast => Math.Max(east * 0.7, south * 0.7),
            CardinalDirection.South => south,
            CardinalDirection.SouthWest => Math.Max(west * 0.7, south * 0.7),
            CardinalDirection.West => west,
            CardinalDirection.NorthWest => Math.Max(north, west * 0.8),
            _ => south
        };
    }

    private static double GetSimpleShadingReduction(Window window)
    {
        var reduction = 1.0;

        if (window.Shading.OverhangDepthM > 0)
            reduction *= 0.90;

        if (window.Shading.SideFinDepthM > 0)
            reduction *= 0.92;

        if (window.Shading.RevealDepthM > 0)
            reduction *= 0.95;

        reduction *= Math.Clamp(window.Shading.MinimumDirectSolarReductionFactor, 0.1, 1.0);

        return Math.Clamp(reduction, 0.1, 1.0);
    }

    private static double GetPeopleHeatGain(RoomType type) => type switch
    {
        RoomType.Office => 125,
        RoomType.MeetingRoom => 125,
        RoomType.Corridor => 80,
        RoomType.ServerRoom => 125,
        RoomType.Retail => 170,
        RoomType.Residential => 80,
        _ => 125
    };

    private static double GetOutdoorEnvelopeHeatTransferCoefficient(
        Room room,
        Models.ReferenceData.BuildingEnvelopeDefaults defaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.External)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));

        envelope += room.Windows.Sum(window => window.Area.SquareMeters * window.UValue.Value);
        envelope += room.Area.SquareMeters * defaults.CeilingUValueWPerM2K;

        return envelope;
    }

    private static double GetGroundEnvelopeHeatTransferCoefficient(
        Room room,
        Models.ReferenceData.BuildingEnvelopeDefaults defaults)
    {
        var envelope = room.Walls
            .Where(wall => wall.BoundaryType == WallBoundaryType.Ground)
            .Sum(wall => wall.Area.SquareMeters * GetWallUValue(wall));

        envelope += room.Area.SquareMeters * defaults.FloorUValueWPerM2K;

        return envelope;
    }

    private static double GetWallUValue(Wall wall) =>
        wall.ConstructionAssembly is { UValueWPerM2K: > 0 } assembly
            ? assembly.UValueWPerM2K
            : wall.UValue.Value;

    private SyntheticDesignDayScopeResponse? BuildScopeResponse(
        IEnumerable<RoomSyntheticHour> source,
        int? scopeId,
        string scopeName,
        string? parentScopeName,
        double safetyFactor)
    {
        var hours = source
            .GroupBy(x => x.HourOfDay)
            .OrderBy(x => x.Key)
            .Select(group => new SyntheticDesignDayHourResponse
            {
                HourOfDay = group.Key,
                DayOfYear = group.First().DayOfYear,
                Month = group.First().Month,
                OutdoorTemperatureC = Round(group.Average(x => x.OutdoorTemperatureC)),
                SolarHorizontalIrradianceWPerM2 = Round(group.Average(x => x.SolarHorizontalWPerM2)),
                LoadW = Round(group.Sum(x => x.LoadW)),
                LoadKw = Round(group.Sum(x => x.LoadW) / 1000.0),
                OperativeTemperatureC = Round(group.Average(x => x.OperativeTemperatureC)),
                InternalGainsW = Round(group.Sum(x => x.InternalGainsW)),
                SolarGainsW = Round(group.Sum(x => x.SolarGainsW))
            })
            .ToList();

        if (hours.Count == 0)
            return null;

        var peak = hours.MaxBy(x => x.LoadW);
        if (peak is null)
            return null;

        var sized = peak.LoadW * safetyFactor;

        return new SyntheticDesignDayScopeResponse
        {
            ScopeId = scopeId,
            ScopeName = scopeName,
            ParentScopeName = parentScopeName,
            RawPeakLoadW = Round(peak.LoadW),
            RawPeakLoadKw = Round(peak.LoadW / 1000.0),
            SizedPeakLoadW = Round(sized),
            SizedPeakLoadKw = Round(sized / 1000.0),
            SafetyFactor = Round(safetyFactor),
            PeakHourOfDay = peak.HourOfDay,
            Hours = hours
        };
    }

    private double GetSafetyFactor(SyntheticDesignDayRequest request) =>
        request.Mode == ReferenceDesignDayMode.Cooling
            ? request.CoolingSafetyFactor
            : request.HeatingSafetyFactor;

    private static int GetMonthFromDayOfYear(int dayOfYear)
    {
        var days = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var sum = 0;

        for (var i = 0; i < days.Length; i++)
        {
            sum += days[i];
            if (dayOfYear <= sum)
                return i + 1;
        }

        return 12;
    }

    private static IReadOnlyList<ZoneGroup> GetZoneGroups(Building building)
    {
        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        if (building.ThermalZones.Count == 0)
            return [new ZoneGroup(null, "Building", allRooms)];

        var countedRooms = new HashSet<Room>();
        var groups = new List<ZoneGroup>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var zoneRooms = zone.AssignedRooms
                .Where(countedRooms.Add)
                .ToArray();

            if (zoneRooms.Length > 0)
                groups.Add(new ZoneGroup(zone.Id, zone.Name, zoneRooms));
        }

        var unassignedRooms = allRooms
            .Where(room => !countedRooms.Contains(room))
            .ToArray();

        if (unassignedRooms.Length > 0)
            groups.Add(new ZoneGroup(null, "Unassigned rooms", unassignedRooms));

        return groups;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ZoneGroup(int? ZoneId, string Name, IReadOnlyCollection<Room> Rooms);

    private sealed record RoomSyntheticHour(
        int RoomId,
        string RoomName,
        string ZoneName,
        int HourOfDay,
        int DayOfYear,
        int Month,
        double OutdoorTemperatureC,
        double SolarHorizontalWPerM2,
        double LoadW,
        double OperativeTemperatureC,
        double InternalGainsW,
        double SolarGainsW);
}
