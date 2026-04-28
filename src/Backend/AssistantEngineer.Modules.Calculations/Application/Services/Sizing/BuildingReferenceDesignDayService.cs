using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class BuildingReferenceDesignDayService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;

    public BuildingReferenceDesignDayService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        Iso52016HourlySteadyStateCalculator iso52016,
        BuildingCalculationReadinessService readiness,
        IEn16798ProfileCatalog profileCatalog,
        IOptions<Iso52016EnergyNeedOptions> energyOptions,
        IOptions<En16798ProfileOptions> profileOptions)
    {
        _buildings = buildings;
        _preferences = preferences;
        _iso52016 = iso52016;
        _readiness = readiness;
        _profileCatalog = profileCatalog;
        _energyOptions = energyOptions.Value;
        _profileOptions = profileOptions.Value;
    }

    public async Task<Result<BuildingReferenceDesignDayResponse>> CalculateAsync(
        int buildingId,
        int? year,
        ReferenceDesignDayRequest request,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingReferenceDesignDayResponse>.Failure(readiness.Error, readiness.ErrorType);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingReferenceDesignDayResponse>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);

        var energyNeed = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year,
            cancellationToken,
            annualProfileOptions: null);

        if (energyNeed is null)
            return Result<BuildingReferenceDesignDayResponse>.Validation(
                "Complete annual climate data is required for reference design day extraction.");

        if (energyNeed.HourlyResults.Count == 0)
            return Result<BuildingReferenceDesignDayResponse>.Validation(
                "No hourly results available for reference design day extraction.");

        var response = new BuildingReferenceDesignDayResponse
        {
            BuildingId = energyNeed.BuildingId,
            BuildingName = energyNeed.BuildingName,
            Year = energyNeed.Year,
            Mode = request.Mode,
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            HeatingSeasonStartMonth = request.HeatingSeasonStartMonth,
            HeatingSeasonEndMonth = request.HeatingSeasonEndMonth
        };

        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        var buildingOccupancy = BuildOccupancyMap(allRooms);

        response.Building = SelectReferenceDay(
            energyNeed.HourlyResults,
            request,
            buildingOccupancy,
            hour => hour.HourOfYear,
            hour => hour.Month,
            hour => GetLoadByMode(hour, request.Mode),
            hour => hour.OperativeTemperatureC,
            hour => hour.OutdoorTemperatureC,
            scopeId: null,
            scopeName: building.Name,
            parentScopeName: null);

        var zoneGroups = GetZoneGroups(building)
            .ToDictionary(group => group.Name, StringComparer.Ordinal);

        foreach (var zoneGroup in energyNeed.ZoneHourlyResults?
                     .GroupBy(x => x.ZoneName)
                     .OrderBy(x => x.Key) ?? Enumerable.Empty<IGrouping<string, Iso52016ZoneHourlyEnergyNeed>>())
        {
            if (!zoneGroups.TryGetValue(zoneGroup.Key, out var group))
                continue;

            var zoneOccupancy = BuildOccupancyMap(group.Rooms);

            var scope = SelectReferenceDay(
                zoneGroup,
                request,
                zoneOccupancy,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => GetLoadByMode(hour, request.Mode),
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                scopeId: group.ZoneId,
                scopeName: group.Name,
                parentScopeName: null);

            if (scope is not null)
                response.Zones.Add(scope);
        }

        var roomsById = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToDictionary(room => room.Id);

        var roomHourlyResults = energyNeed.RoomHourlyResults ?? Array.Empty<Iso52016RoomHourlyEnergyNeed>();

        foreach (var roomGroup in roomHourlyResults
                     .GroupBy(x => x.RoomId)
                     .OrderBy(x => x.Key))
        {
            if (!roomsById.TryGetValue(roomGroup.Key, out var room))
                continue;

            var first = roomGroup.First();
            var roomOccupancy = BuildOccupancyMap([room]);

            var scope = SelectReferenceDay(
                roomGroup,
                request,
                roomOccupancy,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => GetLoadByMode(hour, request.Mode),
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                scopeId: room.Id,
                scopeName: first.RoomName,
                parentScopeName: first.ZoneName);

            if (scope is not null)
                response.Rooms.Add(scope);
        }

        if (response.Building is null)
            return Result<BuildingReferenceDesignDayResponse>.Validation(
                "No reference design day matched the selected filters.");

        return Result<BuildingReferenceDesignDayResponse>.Success(response);
    }

    private ReferenceDesignDayScopeResponse? SelectReferenceDay<T>(
        IEnumerable<T> source,
        ReferenceDesignDayRequest request,
        IReadOnlyDictionary<int, double> occupancyMap,
        Func<T, int> hourOfYearSelector,
        Func<T, int> monthSelector,
        Func<T, double> loadSelector,
        Func<T, double> operativeSelector,
        Func<T, double> outdoorSelector,
        int? scopeId,
        string scopeName,
        string? parentScopeName)
    {
        var seasonStart = request.Mode == ReferenceDesignDayMode.Cooling
            ? request.CoolingSeasonStartMonth
            : request.HeatingSeasonStartMonth;

        var seasonEnd = request.Mode == ReferenceDesignDayMode.Cooling
            ? request.CoolingSeasonEndMonth
            : request.HeatingSeasonEndMonth;

        var safetyFactor = request.Mode == ReferenceDesignDayMode.Cooling
            ? request.CoolingSafetyFactor
            : request.HeatingSafetyFactor;

        var candidates = source
            .Where(item => IsMonthInSeason(monthSelector(item), seasonStart, seasonEnd))
            .Where(item =>
            {
                if (!request.OccupiedHoursOnly)
                    return true;

                var hour = hourOfYearSelector(item);
                return occupancyMap.TryGetValue(hour, out var factor) && factor >= request.OccupancyThreshold;
            })
            .Where(item => loadSelector(item) > 0)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var peak = candidates.MaxBy(loadSelector);
        if (peak is null)
            return null;

        var peakHourOfYear = hourOfYearSelector(peak);
        var dayOfYear = peakHourOfYear / 24;
        var dayHours = source
            .Where(item => hourOfYearSelector(item) / 24 == dayOfYear)
            .OrderBy(hourOfYearSelector)
            .ToArray();

        if (dayHours.Length == 0)
            return null;

        var rawPeak = loadSelector(peak);
        var sizedPeak = rawPeak * safetyFactor;

        return new ReferenceDesignDayScopeResponse
        {
            ScopeId = scopeId,
            ScopeName = scopeName,
            ParentScopeName = parentScopeName,
            DayOfYear = dayOfYear,
            PeakHourOfYear = peakHourOfYear,
            PeakMonth = monthSelector(peak),
            RawPeakLoadW = Round(rawPeak),
            RawPeakLoadKw = Round(rawPeak / 1000.0),
            SizedPeakLoadW = Round(sizedPeak),
            SizedPeakLoadKw = Round(sizedPeak / 1000.0),
            SafetyFactor = Round(safetyFactor),
            Hours = dayHours.Select(item =>
            {
                var hourOfYear = hourOfYearSelector(item);
                return new ReferenceDesignDayHourResponse
                {
                    HourOfDay = hourOfYear % 24,
                    HourOfYear = hourOfYear,
                    DayOfYear = hourOfYear / 24,
                    Month = monthSelector(item),
                    LoadW = Round(loadSelector(item)),
                    LoadKw = Round(loadSelector(item) / 1000.0),
                    OperativeTemperatureC = Round(operativeSelector(item)),
                    OutdoorTemperatureC = Round(outdoorSelector(item))
                };
            }).ToList()
        };
    }

    private static double GetLoadByMode(Iso52016HourlyEnergyNeed hour, ReferenceDesignDayMode mode) =>
        mode == ReferenceDesignDayMode.Cooling ? hour.CoolingLoadW : hour.HeatingLoadW;

    private static double GetLoadByMode(Iso52016ZoneHourlyEnergyNeed hour, ReferenceDesignDayMode mode) =>
        mode == ReferenceDesignDayMode.Cooling ? hour.CoolingLoadW : hour.HeatingLoadW;

    private static double GetLoadByMode(Iso52016RoomHourlyEnergyNeed hour, ReferenceDesignDayMode mode) =>
        mode == ReferenceDesignDayMode.Cooling ? hour.CoolingLoadW : hour.HeatingLoadW;

    private Dictionary<int, double> BuildOccupancyMap(IReadOnlyCollection<Room> rooms)
    {
        var map = new Dictionary<int, double>(capacity: 8760);

        for (var hourOfYear = 0; hourOfYear < 8760; hourOfYear++)
        {
            var hourOfDay = hourOfYear % 24;
            map[hourOfYear] = WeightedAverage(rooms, room => GetOccupancyFactor(room, hourOfDay));
        }

        return map;
    }

    private double GetOccupancyFactor(Room room, int hourOfDay)
    {
        if (room.OccupancySchedule?.Factors.Count == 24)
            return room.OccupancySchedule.Factors[hourOfDay];

        if (!_profileOptions.UseStandardProfilesWhenMissingSchedules)
            return 1.0;

        var profile = _profileCatalog.GetProfile(room.Type, _profileOptions.DefaultCategory);
        return profile.OccupancyFactors[hourOfDay];
    }

    private async Task<Result> EnsureCalculationReadyAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken)
    {
        var effectiveWeatherYear = year ?? _energyOptions.DefaultWeatherYear;
        var report = await _readiness.CheckAsync(buildingId, effectiveWeatherYear, cancellationToken);

        if (report.IsFailure)
            return Result.Failure(report.Error, report.ErrorType);

        var errors = report.Value.Issues
            .Where(issue => issue.Severity == AssistantEngineer.Modules.Buildings.Application.Contracts.Common.BuildingCalculationReadinessSeverity.Error)
            .ToArray();

        if (errors.Length == 0)
            return Result.Success();

        return Result.Validation(
            "Building is not ready for calculation: " +
            string.Join("; ", errors.Select(issue => $"{issue.Location}: {issue.Message}")));
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

    private static bool IsMonthInSeason(int month, int startMonth, int endMonth)
    {
        return startMonth <= endMonth
            ? month >= startMonth && month <= endMonth
            : month >= startMonth || month <= endMonth;
    }

    private static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> selector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(selector);

        return rooms.Sum(room => selector(room) * room.Area.SquareMeters) / totalArea;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ZoneGroup(int? ZoneId, string Name, IReadOnlyCollection<Room> Rooms);
}
