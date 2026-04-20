using System.Globalization;
using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Abstractions.Repositories;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Models.Climate;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Climate;

public sealed class EpwWeatherImportService
{
    private const int EpwHeaderLineCount = 8;
    private const int ExpectedAnnualHours = 8760;

    private readonly IClimateZoneRepository _climateZones;
    private readonly IAnnualClimateDataRepository _annualClimateData;
    private readonly IAppDbContext _context;
    private readonly ILogger<EpwWeatherImportService> _logger;

    public EpwWeatherImportService(
        IClimateZoneRepository climateZones,
        IAnnualClimateDataRepository annualClimateData,
        IAppDbContext context,
        ILogger<EpwWeatherImportService>? logger = null)
    {
        _climateZones = climateZones;
        _annualClimateData = annualClimateData;
        _context = context;
        _logger = logger ?? NullLogger<EpwWeatherImportService>.Instance;
    }

    public async Task<Result<AnnualClimateDataImportResponse>> ImportAsync(
        int climateZoneId,
        ImportEpwWeatherRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FilePath))
            return Result<AnnualClimateDataImportResponse>.Validation("EPW file path is required.");

        if (!File.Exists(request.FilePath))
            return Result<AnnualClimateDataImportResponse>.NotFound($"EPW file '{request.FilePath}' was not found.");

        var climateZone = await _climateZones.GetByIdAsync(climateZoneId, cancellationToken);
        if (climateZone is null)
            return Result<AnnualClimateDataImportResponse>.NotFound($"Climate zone with id {climateZoneId} not found.");

        var parsedWeather = await ParseEpwAsync(request.FilePath, cancellationToken);
        if (parsedWeather.IsFailure)
            return Result<AnnualClimateDataImportResponse>.Failure(parsedWeather);

        var annualData = AnnualClimateData.Create(climateZone, request.Year);
        if (annualData.IsFailure)
            return Result<AnnualClimateDataImportResponse>.Failure(annualData);

        for (var hourOfYear = 0; hourOfYear < parsedWeather.Value.Count; hourOfYear++)
        {
            var weatherHour = parsedWeather.Value[hourOfYear];
            var addResult = annualData.Value.AddHourlyData(
                hourOfYear,
                weatherHour.DryBulbTemperatureC,
                weatherHour.DirectNormalRadiationWPerM2,
                weatherHour.DiffuseHorizontalRadiationWPerM2,
                weatherHour.RelativeHumidityPercent,
                weatherHour.AtmosphericPressurePa,
                weatherHour.WindSpeedMPerS,
                weatherHour.WindDirectionDegrees,
                weatherHour.HorizontalInfraredRadiationWPerM2,
                weatherHour.SkyTemperatureC,
                weatherHour.TotalSkyCoverTenths,
                weatherHour.OpaqueSkyCoverTenths);
            if (addResult.IsFailure)
                return Result<AnnualClimateDataImportResponse>.Failure(addResult);
        }

        await _annualClimateData.ReplaceForClimateZoneAsync(annualData.Value, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported {HourlyRecordCount} EPW weather records for climate zone {ClimateZoneId}, year {Year}.",
            parsedWeather.Value.Count,
            climateZoneId,
            request.Year);

        return Result<AnnualClimateDataImportResponse>.Success(new AnnualClimateDataImportResponse
        {
            ClimateZoneId = climateZoneId,
            Year = request.Year,
            HourlyRecordsImported = parsedWeather.Value.Count,
            SourcePath = Path.GetFullPath(request.FilePath),
            ImportedFields =
            [
                "DryBulbTemperature",
                "DirectSolarRadiation",
                "DiffuseSolarRadiation",
                "RelativeHumidity",
                "AtmosphericPressure",
                "WindSpeed",
                "WindDirection",
                "HorizontalInfraredRadiation",
                "SkyTemperature",
                "TotalSkyCover",
                "OpaqueSkyCover"
            ]
        });
    }

    private static async Task<Result<IReadOnlyList<EpwWeatherHour>>> ParseEpwAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var result = new List<EpwWeatherHour>(ExpectedAnnualHours);
        var lineNumber = 0;

        await using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (lineNumber <= EpwHeaderLineCount || string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split(',');
            if (fields.Length < 24)
                return Result<IReadOnlyList<EpwWeatherHour>>.Validation(
                    $"EPW line {lineNumber} has too few fields.");

            var month = ParseInt(fields[1], lineNumber, "month");
            if (month.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(month);

            var day = ParseInt(fields[2], lineNumber, "day");
            if (day.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(day);

            if (month.Value == 2 && day.Value == 29)
                continue;

            if (result.Count >= ExpectedAnnualHours)
                return Result<IReadOnlyList<EpwWeatherHour>>.Validation(
                    "EPW file contains more than 8760 non-leap-day hourly records.");

            var dryBulb = ParseDouble(fields[6], lineNumber, "dry bulb temperature");
            if (dryBulb.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(dryBulb);

            var directNormal = ParseDouble(fields[14], lineNumber, "direct normal radiation");
            if (directNormal.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(directNormal);

            var diffuseHorizontal = ParseDouble(fields[15], lineNumber, "diffuse horizontal radiation");
            if (diffuseHorizontal.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(diffuseHorizontal);
            var horizontalInfrared = ParseOptionalDouble(
                fields[12],
                lineNumber,
                "horizontal infrared radiation",
                missingThreshold: 9999,
                minInclusive: 0,
                maxInclusive: 1000);
            if (horizontalInfrared.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(horizontalInfrared);
            var relativeHumidity = ParseOptionalDouble(
                fields[8],
                lineNumber,
                "relative humidity",
                missingThreshold: 999,
                minInclusive: 0,
                maxInclusive: 100);
            if (relativeHumidity.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(relativeHumidity);
            var pressure = ParseOptionalDouble(
                fields[9],
                lineNumber,
                "atmospheric pressure",
                missingThreshold: 999999,
                minInclusive: 30_000,
                maxInclusive: 120_000);
            if (pressure.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(pressure);
            var windDirection = ParseOptionalDouble(
                fields[20],
                lineNumber,
                "wind direction",
                missingThreshold: 999,
                minInclusive: 0,
                maxInclusive: 360);
            if (windDirection.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(windDirection);
            var windSpeed = ParseOptionalDouble(
                fields[21],
                lineNumber,
                "wind speed",
                missingThreshold: 999,
                minInclusive: 0,
                maxInclusive: 100);
            if (windSpeed.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(windSpeed);
            var totalSkyCover = ParseOptionalDouble(
                fields[22],
                lineNumber,
                "total sky cover",
                missingThreshold: 99,
                minInclusive: 0,
                maxInclusive: 10);
            if (totalSkyCover.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(totalSkyCover);
            var opaqueSkyCover = ParseOptionalDouble(
                fields[23],
                lineNumber,
                "opaque sky cover",
                missingThreshold: 99,
                minInclusive: 0,
                maxInclusive: 10);
            if (opaqueSkyCover.IsFailure)
                return Result<IReadOnlyList<EpwWeatherHour>>.Failure(opaqueSkyCover);

            result.Add(new EpwWeatherHour(
                DryBulbTemperatureC: dryBulb.Value,
                DirectNormalRadiationWPerM2: NormalizeRadiation(directNormal.Value),
                DiffuseHorizontalRadiationWPerM2: NormalizeRadiation(diffuseHorizontal.Value),
                RelativeHumidityPercent: relativeHumidity.Value.Value,
                AtmosphericPressurePa: pressure.Value.Value,
                WindSpeedMPerS: windSpeed.Value.Value,
                WindDirectionDegrees: windDirection.Value.Value,
                HorizontalInfraredRadiationWPerM2: horizontalInfrared.Value.Value,
                SkyTemperatureC: CalculateSkyTemperatureC(horizontalInfrared.Value.Value),
                TotalSkyCoverTenths: totalSkyCover.Value.Value,
                OpaqueSkyCoverTenths: opaqueSkyCover.Value.Value));
        }

        if (result.Count != ExpectedAnnualHours)
            return Result<IReadOnlyList<EpwWeatherHour>>.Validation(
                $"EPW file must contain 8760 hourly records after leap-day normalization; found {result.Count}.");

        return Result<IReadOnlyList<EpwWeatherHour>>.Success(result);
    }

    private static Result<int> ParseInt(string value, int lineNumber, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return Result<int>.Validation($"EPW line {lineNumber} has invalid {fieldName}.");

        return Result<int>.Success(parsed);
    }

    private static Result<double> ParseDouble(string value, int lineNumber, string fieldName)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            !double.IsFinite(parsed))
        {
            return Result<double>.Validation($"EPW line {lineNumber} has invalid {fieldName}.");
        }

        return Result<double>.Success(parsed);
    }

    private static Result<OptionalEpwDouble> ParseOptionalDouble(
        string value,
        int lineNumber,
        string fieldName,
        double missingThreshold,
        double minInclusive,
        double maxInclusive)
    {
        var parsed = ParseDouble(value, lineNumber, fieldName);
        if (parsed.IsFailure)
            return Result<OptionalEpwDouble>.Failure(parsed);

        if (parsed.Value >= missingThreshold)
            return Result<OptionalEpwDouble>.Success(new OptionalEpwDouble(null));

        if (parsed.Value < minInclusive || parsed.Value > maxInclusive)
            return Result<OptionalEpwDouble>.Validation($"EPW line {lineNumber} has {fieldName} outside the supported range.");

        return Result<OptionalEpwDouble>.Success(new OptionalEpwDouble(parsed.Value));
    }

    private static double NormalizeRadiation(double value) =>
        value is < 0 or >= 9999 ? 0 : value;

    private static double? CalculateSkyTemperatureC(double? horizontalInfraredRadiationWPerM2)
    {
        if (horizontalInfraredRadiationWPerM2 is null or <= 0)
            return null;

        const double stefanBoltzmann = 5.670374419e-8;
        var skyTemperatureK = Math.Pow(horizontalInfraredRadiationWPerM2.Value / stefanBoltzmann, 0.25);
        return Math.Round(skyTemperatureK - 273.15, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record EpwWeatherHour(
        double DryBulbTemperatureC,
        double DirectNormalRadiationWPerM2,
        double DiffuseHorizontalRadiationWPerM2,
        double? RelativeHumidityPercent,
        double? AtmosphericPressurePa,
        double? WindSpeedMPerS,
        double? WindDirectionDegrees,
        double? HorizontalInfraredRadiationWPerM2,
        double? SkyTemperatureC,
        double? TotalSkyCoverTenths,
        double? OpaqueSkyCoverTenths);

    private sealed record OptionalEpwDouble(double? Value);
}
