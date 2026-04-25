using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Comfort;

public sealed class BuildingComfortMetricsService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly IEn16798ProfileCatalog _profileCatalog;
    private readonly Iso52016EnergyNeedOptions _energyOptions;
    private readonly En16798ProfileOptions _profileOptions;

    public BuildingComfortMetricsService(
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

    public async Task<Result<BuildingComfortMetricsResponse>> CalculateAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingComfortMetricsResponse>.Failure(readiness.Error, readiness.ErrorType);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingComfortMetricsResponse>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);

        var energyNeed = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year,
            annualProfileOptions: null,
            cancellationToken);

        if (energyNeed is null)
        {
            return Result<BuildingComfortMetricsResponse>.Validation(
                "Complete annual climate data is required for comfort analysis.");
        }

        if (energyNeed.HourlyResults.Count == 0)
            return Result<BuildingComfortMetricsResponse>.Validation("No hourly results available for comfort analysis.");

        var occupancyByHour = BuildOccupancyMap(building);
        var monthly = InitializeMonthly();
        var response = new BuildingComfortMetricsResponse
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
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            PeakOperativeTemperatureC = double.MinValue,
            PeakHourOfYear = 0,
            Monthly = monthly
        };

        foreach (var hour in energyNeed.HourlyResults.OrderBy(x => x.HourOfYear))
        {
            var occupancyFactor = occupancyByHour.TryGetValue(hour.HourOfYear, out var factor) ? factor : 0.0;
            var isOccupied = occupancyFactor >= request.OccupancyThreshold;

            if (isOccupied)
                response.OccupiedHoursDetected++;

            if (request.OccupiedHoursOnly && !isOccupied)
                continue;

            response.HoursEvaluated++;

            var monthBucket = response.Monthly[hour.Month - 1];
            monthBucket.HoursEvaluated++;
            if (isOccupied)
                monthBucket.OccupiedHoursDetected++;

            if (hour.OperativeTemperatureC > response.PeakOperativeTemperatureC)
            {
                response.PeakOperativeTemperatureC = hour.OperativeTemperatureC;
                response.PeakHourOfYear = hour.HourOfYear;
            }

            if (hour.OperativeTemperatureC > monthBucket.PeakOperativeTemperatureC)
                monthBucket.PeakOperativeTemperatureC = hour.OperativeTemperatureC;

            ApplyOverheating(
                response,
                monthBucket,
                hour,
                request);

            ApplyUnderheating(
                response,
                monthBucket,
                hour,
                request);
        }

        if (response.HoursEvaluated == 0)
        {
            return Result<BuildingComfortMetricsResponse>.Validation(
                "No hours matched the selected comfort analysis filters.");
        }

        foreach (var month in response.Monthly.Where(m => m.PeakOperativeTemperatureC == double.MinValue))
            month.PeakOperativeTemperatureC = 0;

        response.PeakOperativeTemperatureC = Round(response.PeakOperativeTemperatureC);
        response.DegreeHoursAboveOverheatingThreshold = Round(response.DegreeHoursAboveOverheatingThreshold);
        response.DegreeHoursAboveSevereOverheatingThreshold = Round(response.DegreeHoursAboveSevereOverheatingThreshold);
        response.DegreeHoursBelowUnderheatingThreshold = Round(response.DegreeHoursBelowUnderheatingThreshold);
        response.CoolingSeasonDegreeHoursAboveOverheatingThreshold = Round(response.CoolingSeasonDegreeHoursAboveOverheatingThreshold);

        return Result<BuildingComfortMetricsResponse>.Success(response);
    }

    private void ApplyOverheating(
        BuildingComfortMetricsResponse response,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016HourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var overheatingExcess = hour.OperativeTemperatureC - request.OverheatingThresholdC;
        if (overheatingExcess > 0)
        {
            response.HoursAboveOverheatingThreshold++;
            response.DegreeHoursAboveOverheatingThreshold += overheatingExcess;

            monthBucket.HoursAboveOverheatingThreshold++;
            monthBucket.DegreeHoursAboveOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveOverheatingThreshold + overheatingExcess);

            if (IsMonthInCoolingSeason(hour.Month, request.CoolingSeasonStartMonth, request.CoolingSeasonEndMonth))
            {
                response.CoolingSeasonHoursAboveOverheatingThreshold++;
                response.CoolingSeasonDegreeHoursAboveOverheatingThreshold += overheatingExcess;
            }
        }

        var severeExcess = hour.OperativeTemperatureC - request.SevereOverheatingThresholdC;
        if (severeExcess > 0)
        {
            response.HoursAboveSevereOverheatingThreshold++;
            response.DegreeHoursAboveSevereOverheatingThreshold += severeExcess;

            monthBucket.HoursAboveSevereOverheatingThreshold++;
            monthBucket.DegreeHoursAboveSevereOverheatingThreshold =
                Round(monthBucket.DegreeHoursAboveSevereOverheatingThreshold + severeExcess);
        }
    }

    private void ApplyUnderheating(
        BuildingComfortMetricsResponse response,
        MonthlyComfortMetricsResponse monthBucket,
        Iso52016HourlyEnergyNeed hour,
        BuildingComfortMetricsRequest request)
    {
        var underheatingExcess = request.UnderheatingThresholdC - hour.OperativeTemperatureC;
        if (underheatingExcess <= 0)
            return;

        response.HoursBelowUnderheatingThreshold++;
        response.DegreeHoursBelowUnderheatingThreshold += underheatingExcess;

        monthBucket.HoursBelowUnderheatingThreshold++;
        monthBucket.DegreeHoursBelowUnderheatingThreshold =
            Round(monthBucket.DegreeHoursBelowUnderheatingThreshold + underheatingExcess);
    }

    private Dictionary<int, double> BuildOccupancyMap(Building building)
    {
        var rooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToArray();

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

    private static double WeightedAverage(IReadOnlyCollection<Room> rooms, Func<Room, double> valueSelector)
    {
        var totalArea = rooms.Sum(room => room.Area.SquareMeters);
        if (totalArea <= 0)
            return rooms.Count == 0 ? 0 : rooms.Average(valueSelector);

        return rooms.Sum(room => valueSelector(room) * room.Area.SquareMeters) / totalArea;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
