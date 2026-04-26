using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Climate;

public sealed class PvgisAnnualClimateDataImportService
{
    private const int ExpectedAnnualHours = 8760;

    private readonly HttpClient _httpClient;
    private readonly IClimateZoneRepository _climateZones;
    private readonly IAnnualClimateDataRepository _annualClimateData;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PvgisApiOptions _options;
    private readonly ResilientOperationExecutor _executor;
    private readonly ILogger<PvgisAnnualClimateDataImportService> _logger;

    public PvgisAnnualClimateDataImportService(
        HttpClient httpClient,
        IClimateZoneRepository climateZones,
        IAnnualClimateDataRepository annualClimateData,
        IUnitOfWork unitOfWork,
        IOptions<PvgisApiOptions> options,
        ResilientOperationExecutor executor,
        ILogger<PvgisAnnualClimateDataImportService>? logger = null)
    {
        _httpClient = httpClient;
        _climateZones = climateZones;
        _annualClimateData = annualClimateData;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _executor = executor;
        _logger = logger ?? NullLogger<PvgisAnnualClimateDataImportService>.Instance;
    }

    public async Task<Result<AnnualClimateDataImportResponse>> ImportAsync(
        int climateZoneId,
        ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateRequest(request);
        if (validation.IsFailure)
            return Result<AnnualClimateDataImportResponse>.Failure(validation);

        var climateZone = await _climateZones.GetByIdAsync(climateZoneId, cancellationToken);
        if (climateZone is null)
            return Result<AnnualClimateDataImportResponse>.NotFound($"Climate zone with id {climateZoneId} not found.");

        var pvgis = await DownloadTmyAsync(request, cancellationToken);
        if (pvgis.IsFailure)
            return Result<AnnualClimateDataImportResponse>.Failure(pvgis);

        var annualData = AnnualClimateData.Create(climateZone, request.Year);
        if (annualData.IsFailure)
            return Result<AnnualClimateDataImportResponse>.Failure(annualData);

        for (var hourOfYear = 0; hourOfYear < pvgis.Value.Hours.Count; hourOfYear++)
        {
            var hour = pvgis.Value.Hours[hourOfYear];

            var addResult = annualData.Value.AddHourlyData(
                hourOfYear,
                hour.AirTemperatureC,
                NormalizeRadiation(hour.DirectNormalRadiationWPerM2),
                NormalizeRadiation(hour.DiffuseHorizontalRadiationWPerM2),
                hour.RelativeHumidityPercent,
                hour.SurfacePressurePa,
                hour.WindSpeedMPerS,
                hour.WindDirectionDegrees,
                hour.HorizontalInfraredRadiationWPerM2,
                skyTemperatureC: null,
                totalSkyCoverTenths: null,
                opaqueSkyCoverTenths: null);

            if (addResult.IsFailure)
                return Result<AnnualClimateDataImportResponse>.Failure(addResult);
        }

        await _annualClimateData.ReplaceForClimateZoneAsync(annualData.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Imported {HourlyRecordCount} PVGIS TMY weather records for climate zone {ClimateZoneId}, year {Year}, coordinates {Latitude}, {Longitude}.",
            pvgis.Value.Hours.Count,
            climateZoneId,
            request.Year,
            request.Latitude,
            request.Longitude);

        return Result<AnnualClimateDataImportResponse>.Success(new AnnualClimateDataImportResponse
        {
            ClimateZoneId = climateZoneId,
            Year = request.Year,
            HourlyRecordsImported = pvgis.Value.Hours.Count,
            SourceFileName = $"PVGIS TMY ({request.Latitude.ToString("F4", CultureInfo.InvariantCulture)}, {request.Longitude.ToString("F4", CultureInfo.InvariantCulture)})",
            ImportedFields =
            [
                "DryBulbTemperature",
                "DirectSolarRadiation",
                "DiffuseSolarRadiation",
                "RelativeHumidity",
                "AtmosphericPressure",
                "WindSpeed",
                "WindDirection",
                "HorizontalInfraredRadiation"
            ]
        });
    }

    private async Task<Result<PvgisTmyWeather>> DownloadTmyAsync(
        ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var uri = BuildTmyUri(request);
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        HttpResponseMessage response;

        try
        {
            response = await _executor.ExecuteAsync(
                integrationName: "pvgis-tmy",
                settings: CreateResilienceSettings(),
                operation: async ct =>
                {
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
                    requestMessage.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);

                    var httpResponse = await _httpClient.SendAsync(
                        requestMessage,
                        HttpCompletionOption.ResponseHeadersRead,
                        ct);

                    if (IsTransientStatusCode(httpResponse.StatusCode))
                    {
                        httpResponse.Dispose();
                        throw new HttpRequestException(
                            $"PVGIS returned transient status {(int)httpResponse.StatusCode}.",
                            inner: null,
                            httpResponse.StatusCode);
                    }

                    return httpResponse;
                },
                logger: _logger,
                isTransientException: static exception => exception is HttpRequestException,
                cancellationToken: cancellationToken);
        }
        catch (CircuitBreakerOpenException exception)
        {
            _logger.LogWarning(
                exception,
                "PVGIS circuit breaker is open for correlation id {CorrelationId}.",
                correlationId);
            return Result<PvgisTmyWeather>.Failure("PVGIS is temporarily unavailable. Please retry later.");
        }
        catch (TimeoutException exception)
        {
            _logger.LogWarning(
                exception,
                "PVGIS request timed out for correlation id {CorrelationId}.",
                correlationId);
            return Result<PvgisTmyWeather>.Failure("PVGIS request timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "PVGIS request failed after retries for correlation id {CorrelationId}.",
                correlationId);
            return Result<PvgisTmyWeather>.Failure("PVGIS is temporarily unavailable. Please retry later.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "PVGIS request failed with status {StatusCode}. CorrelationId: {CorrelationId}. Response: {ResponseBody}",
                    (int)response.StatusCode,
                    correlationId,
                    responseBody);

                return Result<PvgisTmyWeather>.Validation(
                    $"PVGIS request failed with status {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<PvgisTmyResponse>(cancellationToken: cancellationToken);
            if (payload is null)
                return Result<PvgisTmyWeather>.Validation("PVGIS returned an empty response.");

            if (payload.Outputs?.TmyHourly is null || payload.Outputs.TmyHourly.Count == 0)
                return Result<PvgisTmyWeather>.Validation("PVGIS returned no hourly TMY records.");

            var mappedHours = new List<PvgisTmyHour>(ExpectedAnnualHours);

            foreach (var row in payload.Outputs.TmyHourly.OrderBy(static item => item.TimeUtc))
            {
                var mapped = MapHour(row);
                if (mapped.IsFailure)
                    return Result<PvgisTmyWeather>.Failure(mapped);

                mappedHours.Add(mapped.Value);
            }

            if (mappedHours.Count != ExpectedAnnualHours)
            {
                return Result<PvgisTmyWeather>.Validation(
                    $"PVGIS TMY returned {mappedHours.Count} hourly records instead of {ExpectedAnnualHours}.");
            }

            return Result<PvgisTmyWeather>.Success(new PvgisTmyWeather(mappedHours));
        }
    }

    private Uri BuildTmyUri(ImportPvgisWeatherRequest request)
    {
        var query = new Dictionary<string, string?>
        {
            ["lat"] = request.Latitude.ToString("F6", CultureInfo.InvariantCulture),
            ["lon"] = request.Longitude.ToString("F6", CultureInfo.InvariantCulture),
            ["outputformat"] = "json",
            ["usehorizon"] = request.UseHorizon ? "1" : "0"
        };

        if (request.StartYear.HasValue)
            query["startyear"] = request.StartYear.Value.ToString(CultureInfo.InvariantCulture);

        if (request.EndYear.HasValue)
            query["endyear"] = request.EndYear.Value.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(request.RadiationDatabase))
            query["raddatabase"] = request.RadiationDatabase!.Trim();

        var baseUrl = _options.BaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _options.BaseUrl
            : _options.BaseUrl + "/";

        var queryString = string.Join(
            "&",
            query
                .Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(static kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

        return new Uri($"{baseUrl}tmy?{queryString}", UriKind.Absolute);
    }

    private static Result ValidateRequest(ImportPvgisWeatherRequest request)
    {
        if (request.Year < 1900 || request.Year > 2100)
            return Result.Validation("Year must be between 1900 and 2100.");

        if (request.Latitude is < -90 or > 90)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.Longitude is < -180 or > 180)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.StartYear.HasValue && request.EndYear.HasValue && request.StartYear > request.EndYear)
            return Result.Validation("StartYear cannot be greater than EndYear.");

        return Result.Success();
    }

    private ResilientOperationSettings CreateResilienceSettings() =>
        new(
            Timeout: TimeSpan.FromSeconds(_options.TimeoutSeconds),
            MaxRetryAttempts: _options.MaxRetryAttempts,
            InitialRetryDelay: TimeSpan.FromMilliseconds(_options.InitialRetryDelayMilliseconds),
            CircuitBreakerFailureThreshold: _options.CircuitBreakerFailureThreshold,
            CircuitBreakerBreakDuration: TimeSpan.FromSeconds(_options.CircuitBreakerBreakDurationSeconds));

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static Result<PvgisTmyHour> MapHour(PvgisTmyHourDto row)
    {
        if (string.IsNullOrWhiteSpace(row.TimeUtc))
            return Result<PvgisTmyHour>.Validation("PVGIS hourly record does not contain time(UTC).");

        if (!DateTime.TryParseExact(
                row.TimeUtc,
                "yyyyMMdd:HHmm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _))
        {
            return Result<PvgisTmyHour>.Validation($"PVGIS time value '{row.TimeUtc}' has invalid format.");
        }

        return Result<PvgisTmyHour>.Success(new PvgisTmyHour(
            AirTemperatureC: row.AirTemperatureC,
            RelativeHumidityPercent: ClampOrNull(row.RelativeHumidityPercent, 0, 100),
            DirectNormalRadiationWPerM2: Math.Max(0, row.DirectNormalRadiationWPerM2),
            DiffuseHorizontalRadiationWPerM2: Math.Max(0, row.DiffuseHorizontalRadiationWPerM2),
            HorizontalInfraredRadiationWPerM2: ClampOrNull(row.HorizontalInfraredRadiationWPerM2, 0, 1000),
            WindSpeedMPerS: ClampOrNull(row.WindSpeedMPerS, 0, 100),
            WindDirectionDegrees: ClampOrNull(row.WindDirectionDegrees, 0, 360),
            SurfacePressurePa: ClampOrNull(row.SurfacePressurePa, 30_000, 120_000)));
    }

    private static double NormalizeRadiation(double value) =>
        double.IsFinite(value) ? Math.Max(0, value) : 0;

    private static double? ClampOrNull(double? value, double min, double max)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
            return null;

        return value.Value < min || value.Value > max
            ? null
            : value.Value;
    }

    private sealed record PvgisTmyWeather(IReadOnlyList<PvgisTmyHour> Hours);

    private sealed record PvgisTmyHour(
        double AirTemperatureC,
        double? RelativeHumidityPercent,
        double DirectNormalRadiationWPerM2,
        double DiffuseHorizontalRadiationWPerM2,
        double? HorizontalInfraredRadiationWPerM2,
        double? WindSpeedMPerS,
        double? WindDirectionDegrees,
        double? SurfacePressurePa);

    private sealed class PvgisTmyResponse
    {
        [JsonPropertyName("outputs")]
        public PvgisTmyOutputsDto? Outputs { get; set; }
    }

    private sealed class PvgisTmyOutputsDto
    {
        [JsonPropertyName("tmy_hourly")]
        public List<PvgisTmyHourDto>? TmyHourly { get; set; }
    }

    private sealed class PvgisTmyHourDto
    {
        [JsonPropertyName("time(UTC)")]
        public string TimeUtc { get; set; } = string.Empty;

        [JsonPropertyName("T2m")]
        public double AirTemperatureC { get; set; }

        [JsonPropertyName("RH")]
        public double? RelativeHumidityPercent { get; set; }

        [JsonPropertyName("Gb(n)")]
        public double DirectNormalRadiationWPerM2 { get; set; }

        [JsonPropertyName("Gd(h)")]
        public double DiffuseHorizontalRadiationWPerM2 { get; set; }

        [JsonPropertyName("IR(h)")]
        public double? HorizontalInfraredRadiationWPerM2 { get; set; }

        [JsonPropertyName("WS10m")]
        public double? WindSpeedMPerS { get; set; }

        [JsonPropertyName("WD10m")]
        public double? WindDirectionDegrees { get; set; }

        [JsonPropertyName("SP")]
        public double? SurfacePressurePa { get; set; }
    }
}
