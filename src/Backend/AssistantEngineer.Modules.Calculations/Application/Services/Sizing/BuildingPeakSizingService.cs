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

public sealed class BuildingPeakSizingService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;

    public BuildingPeakSizingService(
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

    public async Task<Result<BuildingPeakSizingResponse>> CalculateAsync(
        int buildingId,
        int? year,
        PeakSizingRequest request,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingPeakSizingResponse>.Failure(readiness.Error, readiness.ErrorType);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingPeakSizingResponse>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);

        var energyNeed = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year,
            cancellationToken,
            annualProfileOptions: null);

        if (energyNeed is null)
        {
            return Result<BuildingPeakSizingResponse>.Validation(
                "Complete annual climate data is required for sizing analysis.");
        }

        if (energyNeed.HourlyResults.Count == 0)
        {
            return Result<BuildingPeakSizingResponse>.Validation(
                "No hourly results available for sizing analysis.");
        }

        var response = new BuildingPeakSizingResponse
        {
            BuildingId = energyNeed.BuildingId,
            BuildingName = energyNeed.BuildingName,
            Year = energyNeed.Year,
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

        response.BuildingCoolingPeak = SelectPeak(
            energyNeed.HourlyResults,
            hour => hour.HourOfYear,
            hour => hour.Month,
            hour => hour.CoolingLoadW,
            hour => hour.OperativeTemperatureC,
            hour => hour.OutdoorTemperatureC,
            buildingOccupancy,
            request.OccupiedHoursOnly,
            request.OccupancyThreshold,
            request.CoolingSeasonStartMonth,
            request.CoolingSeasonEndMonth,
            scopeId: null,
            scopeName: building.Name,
            parentScopeName: null,
            safetyFactor: request.CoolingSafetyFactor);

        response.BuildingHeatingPeak = SelectPeak(
            energyNeed.HourlyResults,
            hour => hour.HourOfYear,
            hour => hour.Month,
            hour => hour.HeatingLoadW,
            hour => hour.OperativeTemperatureC,
            hour => hour.OutdoorTemperatureC,
            buildingOccupancy,
            request.OccupiedHoursOnly,
            request.OccupancyThreshold,
            request.HeatingSeasonStartMonth,
            request.HeatingSeasonEndMonth,
            scopeId: null,
            scopeName: building.Name,
            parentScopeName: null,
            safetyFactor: request.HeatingSafetyFactor);

        var zoneGroups = GetZoneGroups(building)
            .ToDictionary(group => group.Name, StringComparer.Ordinal);

        foreach (var zoneGroup in energyNeed.ZoneHourlyResults?
                     .GroupBy(x => x.ZoneName)
                     .OrderBy(x => x.Key) ?? Enumerable.Empty<IGrouping<string, Iso52016ZoneHourlyEnergyNeed>>())
        {
            if (!zoneGroups.TryGetValue(zoneGroup.Key, out var group))
                continue;

            var zoneOccupancy = BuildOccupancyMap(group.Rooms);
            var zoneId = group.ZoneId;

            var coolingPeak = SelectPeak(
                zoneGroup,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => hour.CoolingLoadW,
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                zoneOccupancy,
                request.OccupiedHoursOnly,
                request.OccupancyThreshold,
                request.CoolingSeasonStartMonth,
                request.CoolingSeasonEndMonth,
                scopeId: zoneId,
                scopeName: group.Name,
                parentScopeName: null,
                safetyFactor: request.CoolingSafetyFactor);

            if (coolingPeak is not null)
                response.ZoneCoolingPeaks.Add(coolingPeak);

            var heatingPeak = SelectPeak(
                zoneGroup,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => hour.HeatingLoadW,
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                zoneOccupancy,
                request.OccupiedHoursOnly,
                request.OccupancyThreshold,
                request.HeatingSeasonStartMonth,
                request.HeatingSeasonEndMonth,
                scopeId: zoneId,
                scopeName: group.Name,
                parentScopeName: null,
                safetyFactor: request.HeatingSafetyFactor);

            if (heatingPeak is not null)
                response.ZoneHeatingPeaks.Add(heatingPeak);
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

            var coolingPeak = SelectPeak(
                roomGroup,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => hour.CoolingLoadW,
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                roomOccupancy,
                request.OccupiedHoursOnly,
                request.OccupancyThreshold,
                request.CoolingSeasonStartMonth,
                request.CoolingSeasonEndMonth,
                scopeId: room.Id,
                scopeName: first.RoomName,
                parentScopeName: first.ZoneName,
                safetyFactor: request.CoolingSafetyFactor);

            if (coolingPeak is not null)
                response.RoomCoolingPeaks.Add(coolingPeak);

            var heatingPeak = SelectPeak(
                roomGroup,
                hour => hour.HourOfYear,
                hour => hour.Month,
                hour => hour.HeatingLoadW,
                hour => hour.OperativeTemperatureC,
                hour => hour.OutdoorTemperatureC,
                roomOccupancy,
                request.OccupiedHoursOnly,
                request.OccupancyThreshold,
                request.HeatingSeasonStartMonth,
                request.HeatingSeasonEndMonth,
                scopeId: room.Id,
                scopeName: first.RoomName,
                parentScopeName: first.ZoneName,
                safetyFactor: request.HeatingSafetyFactor);

            if (heatingPeak is not null)
                response.RoomHeatingPeaks.Add(heatingPeak);
        }
        
        return Result<BuildingPeakSizingResponse>.Success(response);
    }

    private static PeakLoadSummaryResponse? SelectPeak<T>(
        IEnumerable<T> source,
        Func<T, int> hourSelector,
        Func<T, int> monthSelector,
        Func<T, double> loadSelector,
        Func<T, double> operativeSelector,
        Func<T, double> outdoorSelector,
        IReadOnlyDictionary<int, double> occupancyMap,
        bool occupiedHoursOnly,
        double occupancyThreshold,
        int seasonStartMonth,
        int seasonEndMonth,
        int? scopeId,
        string scopeName,
        string? parentScopeName,
        double safetyFactor)
    {
        var candidates = source
            .Where(item => IsMonthInSeason(monthSelector(item), seasonStartMonth, seasonEndMonth))
            .Where(item =>
            {
                if (!occupiedHoursOnly)
                    return true;

                var hour = hourSelector(item);
                return occupancyMap.TryGetValue(hour, out var factor) && factor >= occupancyThreshold;
            })
            .Where(item => loadSelector(item) > 0)
            .ToArray();

        if (candidates.Length == 0)
            return null;

        var peak = candidates.MaxBy(loadSelector);
        if (peak is null)
            return null;

        var rawPeak = loadSelector(peak);
        var sizedPeak = rawPeak * safetyFactor;

        return new PeakLoadSummaryResponse
        {
            ScopeId = scopeId,
            ScopeName = scopeName,
            ParentScopeName = parentScopeName,
            RawPeakLoadW = Round(rawPeak),
            RawPeakLoadKw = Round(rawPeak / 1000.0),
            SizedPeakLoadW = Round(sizedPeak),
            SizedPeakLoadKw = Round(sizedPeak / 1000.0),
            SafetyFactor = Round(safetyFactor),
            PeakHourOfYear = hourSelector(peak),
            Month = monthSelector(peak),
            OperativeTemperatureC = Round(operativeSelector(peak)),
            OutdoorTemperatureC = Round(outdoorSelector(peak))
        };
    }

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
