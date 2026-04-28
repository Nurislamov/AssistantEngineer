using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Comfort;

public sealed class BuildingZoneComfortMetricsService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;

    public BuildingZoneComfortMetricsService(
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

    public async Task<Result<BuildingZoneComfortMetricsResponse>> CalculateAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingZoneComfortMetricsResponse>.Failure(readiness.Error, readiness.ErrorType);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingZoneComfortMetricsResponse>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);

        var energyNeed = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year,
            cancellationToken,
            annualProfileOptions: null);

        if (energyNeed is null)
        {
            return Result<BuildingZoneComfortMetricsResponse>.Validation(
                "Complete annual climate data is required for comfort analysis.");
        }

        var zoneHourlyResults = energyNeed.ZoneHourlyResults?.ToArray() ?? Array.Empty<Iso52016ZoneHourlyEnergyNeed>();
        if (zoneHourlyResults.Length == 0)
        {
            return Result<BuildingZoneComfortMetricsResponse>.Validation(
                "Zone-level hourly results are not available for comfort analysis.");
        }

        var zoneGroups = GetZoneGroups(building);

        var response = new BuildingZoneComfortMetricsResponse
        {
            BuildingId = energyNeed.BuildingId,
            BuildingName = energyNeed.BuildingName,
            Year = energyNeed.Year,
            OverheatingThresholdC = request.OverheatingThresholdC,
            SevereOverheatingThresholdC = request.SevereOverheatingThresholdC,
            UnderheatingThresholdC = request.UnderheatingThresholdC,
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth
        };

        foreach (var zoneGroup in zoneGroups)
        {
            var zoneHours = zoneHourlyResults
                .Where(x => string.Equals(x.ZoneName, zoneGroup.Name, StringComparison.Ordinal))
                .OrderBy(x => x.HourOfYear)
                .ToArray();

            if (zoneHours.Length == 0)
                continue;

            var occupancyMap = BuildOccupancyMap(zoneGroup.Rooms);
            var zoneResponse = new ZoneComfortMetricsResponse
            {
                ZoneName = zoneGroup.Name,
                PeakOperativeTemperatureC = double.MinValue,
                PeakHourOfYear = 0,
                Monthly = InitializeMonthly()
            };

            foreach (var hour in zoneHours)
            {
                var occupancyFactor = occupancyMap.TryGetValue(hour.HourOfYear, out var factor) ? factor : 0.0;
                var isOccupied = occupancyFactor >= request.OccupancyThreshold;

                if (isOccupied)
                    zoneResponse.OccupiedHoursDetected++;

                if (request.OccupiedHoursOnly && !isOccupied)
                    continue;

                zoneResponse.HoursEvaluated++;

                var monthBucket = zoneResponse.Monthly[hour.Month - 1];
                monthBucket.HoursEvaluated++;
                if (isOccupied)
                    monthBucket.OccupiedHoursDetected++;

                if (hour.OperativeTemperatureC > zoneResponse.PeakOperativeTemperatureC)
                {
                    zoneResponse.PeakOperativeTemperatureC = hour.OperativeTemperatureC;
                    zoneResponse.PeakHourOfYear = hour.HourOfYear;
                }

                if (hour.OperativeTemperatureC > monthBucket.PeakOperativeTemperatureC)
                    monthBucket.PeakOperativeTemperatureC = hour.OperativeTemperatureC;

                ApplyOverheating(zoneResponse, monthBucket, hour, request);
                ApplyUnderheating(zoneResponse, monthBucket, hour, request);
            }

            if (zoneResponse.HoursEvaluated == 0)
                continue;

            foreach (var month in zoneResponse.Monthly.Where(m => m.PeakOperativeTemperatureC == double.MinValue))
                month.PeakOperativeTemperatureC = 0;

            zoneResponse.PeakOperativeTemperatureC = Round(zoneResponse.PeakOperativeTemperatureC);
            zoneResponse.DegreeHoursAboveOverheatingThreshold = Round(zoneResponse.DegreeHoursAboveOverheatingThreshold);
            zoneResponse.DegreeHoursAboveSevereOverheatingThreshold = Round(zoneResponse.DegreeHoursAboveSevereOverheatingThreshold);
            zoneResponse.DegreeHoursBelowUnderheatingThreshold = Round(zoneResponse.DegreeHoursBelowUnderheatingThreshold);
            zoneResponse.CoolingSeasonDegreeHoursAboveOverheatingThreshold = Round(zoneResponse.CoolingSeasonDegreeHoursAboveOverheatingThreshold);

            response.Zones.Add(zoneResponse);
        }

        if (response.Zones.Count == 0)
        {
            return Result<BuildingZoneComfortMetricsResponse>.Validation(
                "No zone hours matched the selected comfort analysis filters.");
        }

        return Result<BuildingZoneComfortMetricsResponse>.Success(response);
    }

    private void ApplyOverheating(
        ZoneComfortMetricsResponse zone,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016ZoneHourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var overheatingExcess = hour.OperativeTemperatureC - request.OverheatingThresholdC;
        if (overheatingExcess > 0)
        {
            zone.HoursAboveOverheatingThreshold++;
            zone.DegreeHoursAboveOverheatingThreshold += overheatingExcess;

            monthBucket.HoursAboveOverheatingThreshold++;
            monthBucket.DegreeHoursAboveOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveOverheatingThreshold + overheatingExcess);

            if (IsMonthInCoolingSeason(hour.Month, request.CoolingSeasonStartMonth, request.CoolingSeasonEndMonth))
            {
                zone.CoolingSeasonHoursAboveOverheatingThreshold++;
                zone.CoolingSeasonDegreeHoursAboveOverheatingThreshold += overheatingExcess;
            }
        }

        var severeExcess = hour.OperativeTemperatureC - request.SevereOverheatingThresholdC;
        if (severeExcess > 0)
        {
            zone.HoursAboveSevereOverheatingThreshold++;
            zone.DegreeHoursAboveSevereOverheatingThreshold += severeExcess;

            monthBucket.HoursAboveSevereOverheatingThreshold++;
            monthBucket.DegreeHoursAboveSevereOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveSevereOverheatingThreshold + severeExcess);
        }
    }

    private void ApplyUnderheating(
        ZoneComfortMetricsResponse zone,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016ZoneHourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var underheatingExcess = request.UnderheatingThresholdC - hour.OperativeTemperatureC;
        if (underheatingExcess <= 0)
            return;

        zone.HoursBelowUnderheatingThreshold++;
        zone.DegreeHoursBelowUnderheatingThreshold += underheatingExcess;

        monthBucket.HoursBelowUnderheatingThreshold++;
        monthBucket.DegreeHoursBelowUnderheatingThreshold =
            Round(monthBucket.DegreeHoursBelowUnderheatingThreshold + underheatingExcess);
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

    private static List<MonthlyComfortMetricsResponse> InitializeMonthly() =>
        Enumerable.Range(1, 12)
            .Select(month => new MonthlyComfortMetricsResponse
            {
                Month = month,
                PeakOperativeTemperatureC = double.MinValue
            })
            .ToList();

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
            .Where(issue => issue.Severity == BuildingCalculationReadinessSeverity.Error)
            .ToArray();

        if (errors.Length == 0)
            return Result.Success();

        return Result.Validation(
            "Building is not ready for calculation: " +
            string.Join("; ", errors.Select(issue => $"{issue.Location}: {issue.Message}")));
    }

    private static bool IsMonthInCoolingSeason(int month, int startMonth, int endMonth)
    {
        return startMonth <= endMonth
            ? month >= startMonth && month <= endMonth
            : month >= startMonth || month <= endMonth;
    }

    private static IReadOnlyList<ZoneGroup> GetZoneGroups(Building building)
    {
        var allRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

        if (building.ThermalZones.Count == 0)
            return [new ZoneGroup("Building", allRooms)];

        var countedRooms = new HashSet<Room>();
        var groups = new List<ZoneGroup>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var zoneRooms = zone.AssignedRooms
                .Where(countedRooms.Add)
                .ToArray();

            if (zoneRooms.Length > 0)
                groups.Add(new ZoneGroup(zone.Name, zoneRooms));
        }

        var unassignedRooms = allRooms
            .Where(room => !countedRooms.Contains(room))
            .ToArray();

        if (unassignedRooms.Length > 0)
            groups.Add(new ZoneGroup("Unassigned rooms", unassignedRooms));

        return groups;
    }

    private static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> valueSelector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(valueSelector);

        return rooms.Sum(room => valueSelector(room) * room.Area.SquareMeters) / totalArea;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record ZoneGroup(string Name, IReadOnlyCollection<Room> Rooms);
}
