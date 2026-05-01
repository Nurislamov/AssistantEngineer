using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundTemperatureProfilePreviewService
{
    private readonly IBuildingRepository _buildings;
    private readonly IAnnualClimateDataProvider _annualClimateData;
    private readonly IGroundTemperatureService _groundTemperatureService;
    private readonly Iso52016EnergyNeedOptions _energyNeedOptions;

    public GroundTemperatureProfilePreviewService(
        IBuildingRepository buildings,
        IAnnualClimateDataProvider annualClimateData,
        IGroundTemperatureService groundTemperatureService,
        IOptions<Iso52016EnergyNeedOptions> energyNeedOptions)
    {
        _buildings = buildings;
        _annualClimateData = annualClimateData;
        _groundTemperatureService = groundTemperatureService;
        _energyNeedOptions = energyNeedOptions.Value;
    }

    public async Task<Result<GroundTemperatureProfileResponse>> PreviewAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetByIdAsync(buildingId, false, cancellationToken);
        if (building is null)
            return Result<GroundTemperatureProfileResponse>.NotFound($"Building with id {buildingId} not found.");

        if (building.ClimateZone is null)
            return Result<GroundTemperatureProfileResponse>.Validation("Building climate zone is not assigned.");

        var weatherYear = year ?? _energyNeedOptions.DefaultWeatherYear;

        var annualData = await _annualClimateData.GetForClimateZoneAsync(
            building.ClimateZone.Id,
            weatherYear,
            cancellationToken);

        if (annualData is null || annualData.HourlyData.Count == 0)
        {
            return Result<GroundTemperatureProfileResponse>.Validation(
                "Complete annual climate data is required to preview ground temperature profile.");
        }

        var ordered = annualData.HourlyData
            .OrderBy(h => h.HourOfYear)
            .ToArray();

        if (ordered.Length == 0)
        {
            return Result<GroundTemperatureProfileResponse>.Validation(
                "Annual climate data does not contain hourly weather records with HourOfYear.");
        }

        var hourly = _groundTemperatureService.BuildHourlyProfile(ordered);
        if (hourly.Length == 0)
        {
            return Result<GroundTemperatureProfileResponse>.Validation(
                "Ground temperature profile could not be generated.");
        }

        var minValue = hourly.Min();
        var maxValue = hourly.Max();
        var minHour = Array.IndexOf(hourly, minValue);
        var maxHour = Array.IndexOf(hourly, maxValue);

        var response = new GroundTemperatureProfileResponse
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            Year = weatherYear,
            TotalHours = hourly.Length,
            AnnualAverageGroundTemperatureC = Math.Round(hourly.Average(), 4),
            MinimumGroundTemperatureC = Math.Round(minValue, 4),
            MinimumHourOfYear = minHour,
            MaximumGroundTemperatureC = Math.Round(maxValue, 4),
            MaximumHourOfYear = maxHour,
            HourlyValues = hourly.Select(v => Math.Round(v, 4)).ToList(),
            MonthlyAverages = BuildMonthlyAverages(ordered)
        };

        return Result<GroundTemperatureProfileResponse>.Success(response);
    }

    private List<GroundTemperatureMonthlyPoint> BuildMonthlyAverages(
        IReadOnlyList<AssistantEngineer.Modules.Buildings.Domain.Climate.AnnualHourlyData> hourlyClimateData)
    {
        var result = new List<GroundTemperatureMonthlyPoint>(12);

        for (var month = 1; month <= 12; month++)
        {
            result.Add(new GroundTemperatureMonthlyPoint
            {
                Month = month,
                AverageTemperatureC = Math.Round(
                    _groundTemperatureService.GetMonthlyAverageTemperature(hourlyClimateData, month),
                    4)
            });
        }

        return result;
    }
}
