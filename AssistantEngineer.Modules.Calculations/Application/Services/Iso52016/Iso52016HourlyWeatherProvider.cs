using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed class Iso52016HourlyWeatherProvider
{
    private readonly IAnnualClimateDataProvider _climateDataProvider;
    private readonly Iso52016EnergyNeedOptions _options;
    private readonly ILogger _logger;

    public Iso52016HourlyWeatherProvider(
        IAnnualClimateDataProvider climateDataProvider,
        Iso52016EnergyNeedOptions options,
        ILogger? logger = null)
    {
        _climateDataProvider = climateDataProvider;
        _options = options;
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task<Iso52016HourlyWeatherContext?> GetBuildingWeatherAsync(
        Building building,
        int? year,
        CancellationToken cancellationToken)
    {
        var climateZone = building.ClimateZone
                          ?? throw new InvalidOperationException("Building must have a climate zone assigned.");

        var weatherYear = year ?? _options.DefaultWeatherYear;
        var annualData = await _climateDataProvider.GetForClimateZoneAsync(
            climateZone.Id,
            weatherYear,
            cancellationToken);

        if (!Iso52016HourlyCalculatorMath.HasCompleteAnnualWeatherData(annualData))
        {
            _logger.LogWarning(
                "No complete annual climate data found for climate zone {ClimateZoneId} and year {Year}.",
                climateZone.Id,
                weatherYear);
            return null;
        }

        return new Iso52016HourlyWeatherContext(
            weatherYear,
            annualData!.HourlyData
                .Where(hour => hour.HourOfYear.HasValue)
                .OrderBy(hour => hour.HourOfYear!.Value)
                .ToArray());
    }
}
