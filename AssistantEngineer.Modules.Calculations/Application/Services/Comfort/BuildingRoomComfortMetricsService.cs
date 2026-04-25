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

public sealed class BuildingRoomComfortMetricsService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;

    public BuildingRoomComfortMetricsService(
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

    public async Task<Result<BuildingRoomComfortMetricsResponse>> CalculateAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingRoomComfortMetricsResponse>.Failure(readiness.Error, readiness.ErrorType);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingRoomComfortMetricsResponse>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);

        var energyNeed = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year,
            cancellationToken,
            annualProfileOptions: null);

        if (energyNeed is null)
            return Result<BuildingRoomComfortMetricsResponse>.Validation("Complete annual climate data is required for comfort analysis.");

        var roomHourlyResults = energyNeed.RoomHourlyResults?.ToArray() ?? Array.Empty<Iso52016RoomHourlyEnergyNeed>();
        if (roomHourlyResults.Length == 0)
            return Result<BuildingRoomComfortMetricsResponse>.Validation("Room-level hourly results are not available for comfort analysis.");

        var roomsById = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToDictionary(room => room.Id);

        var response = new BuildingRoomComfortMetricsResponse
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

        foreach (var roomGroup in roomHourlyResults
                     .GroupBy(x => new { x.RoomId, x.RoomName, x.ZoneName })
                     .OrderBy(x => x.Key.RoomId))
        {
            if (!roomsById.TryGetValue(roomGroup.Key.RoomId, out var room))
                continue;

            var occupancyMap = BuildOccupancyMap(room);

            var roomResponse = new RoomComfortMetricsResponse
            {
                RoomId = roomGroup.Key.RoomId,
                RoomName = roomGroup.Key.RoomName,
                ZoneName = roomGroup.Key.ZoneName,
                PeakOperativeTemperatureC = double.MinValue,
                Monthly = InitializeMonthly()
            };

            foreach (var hour in roomGroup.OrderBy(x => x.HourOfYear))
            {
                var occupancyFactor = occupancyMap.TryGetValue(hour.HourOfYear, out var factor) ? factor : 0.0;
                var isOccupied = occupancyFactor >= request.OccupancyThreshold;

                if (isOccupied)
                    roomResponse.OccupiedHoursDetected++;

                if (request.OccupiedHoursOnly && !isOccupied)
                    continue;

                roomResponse.HoursEvaluated++;

                var monthBucket = roomResponse.Monthly[hour.Month - 1];
                monthBucket.HoursEvaluated++;
                if (isOccupied)
                    monthBucket.OccupiedHoursDetected++;

                if (hour.OperativeTemperatureC > roomResponse.PeakOperativeTemperatureC)
                {
                    roomResponse.PeakOperativeTemperatureC = hour.OperativeTemperatureC;
                    roomResponse.PeakHourOfYear = hour.HourOfYear;
                }

                if (hour.OperativeTemperatureC > monthBucket.PeakOperativeTemperatureC)
                    monthBucket.PeakOperativeTemperatureC = hour.OperativeTemperatureC;

                ApplyOverheating(roomResponse, monthBucket, hour, request);
                ApplyUnderheating(roomResponse, monthBucket, hour, request);
            }

            if (roomResponse.HoursEvaluated == 0)
                continue;

            foreach (var month in roomResponse.Monthly.Where(m => m.PeakOperativeTemperatureC == double.MinValue))
                month.PeakOperativeTemperatureC = 0;

            roomResponse.PeakOperativeTemperatureC = Round(roomResponse.PeakOperativeTemperatureC);
            roomResponse.DegreeHoursAboveOverheatingThreshold = Round(roomResponse.DegreeHoursAboveOverheatingThreshold);
            roomResponse.DegreeHoursAboveSevereOverheatingThreshold = Round(roomResponse.DegreeHoursAboveSevereOverheatingThreshold);
            roomResponse.DegreeHoursBelowUnderheatingThreshold = Round(roomResponse.DegreeHoursBelowUnderheatingThreshold);
            roomResponse.CoolingSeasonDegreeHoursAboveOverheatingThreshold = Round(roomResponse.CoolingSeasonDegreeHoursAboveOverheatingThreshold);

            response.Rooms.Add(roomResponse);
        }

        if (response.Rooms.Count == 0)
            return Result<BuildingRoomComfortMetricsResponse>.Validation("No room hours matched the selected comfort analysis filters.");

        return Result<BuildingRoomComfortMetricsResponse>.Success(response);
    }

    private void ApplyOverheating(
        RoomComfortMetricsResponse room,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016RoomHourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var overheatingExcess = hour.OperativeTemperatureC - request.OverheatingThresholdC;
        if (overheatingExcess > 0)
        {
            room.HoursAboveOverheatingThreshold++;
            room.DegreeHoursAboveOverheatingThreshold += overheatingExcess;

            monthBucket.HoursAboveOverheatingThreshold++;
            monthBucket.DegreeHoursAboveOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveOverheatingThreshold + overheatingExcess);

            if (IsMonthInCoolingSeason(hour.Month, request.CoolingSeasonStartMonth, request.CoolingSeasonEndMonth))
            {
                room.CoolingSeasonHoursAboveOverheatingThreshold++;
                room.CoolingSeasonDegreeHoursAboveOverheatingThreshold += overheatingExcess;
            }
        }

        var severeExcess = hour.OperativeTemperatureC - request.SevereOverheatingThresholdC;
        if (severeExcess > 0)
        {
            room.HoursAboveSevereOverheatingThreshold++;
            room.DegreeHoursAboveSevereOverheatingThreshold += severeExcess;

            monthBucket.HoursAboveSevereOverheatingThreshold++;
            monthBucket.DegreeHoursAboveSevereOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveSevereOverheatingThreshold + severeExcess);
        }
    }

    private void ApplyUnderheating(
        RoomComfortMetricsResponse room,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016RoomHourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var underheatingExcess = request.UnderheatingThresholdC - hour.OperativeTemperatureC;
        if (underheatingExcess <= 0)
            return;

        room.HoursBelowUnderheatingThreshold++;
        room.DegreeHoursBelowUnderheatingThreshold += underheatingExcess;

        monthBucket.HoursBelowUnderheatingThreshold++;
        monthBucket.DegreeHoursBelowUnderheatingThreshold =
            Round(monthBucket.DegreeHoursBelowUnderheatingThreshold + underheatingExcess);
    }

    private Dictionary<int, double> BuildOccupancyMap(Room room)
    {
        var map = new Dictionary<int, double>(capacity: 8760);

        for (var hourOfYear = 0; hourOfYear < 8760; hourOfYear++)
        {
            var hourOfDay = hourOfYear % 24;
            map[hourOfYear] = GetOccupancyFactor(room, hourOfDay);
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

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
