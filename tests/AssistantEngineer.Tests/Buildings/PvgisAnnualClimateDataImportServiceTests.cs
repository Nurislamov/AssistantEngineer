using System.Globalization;
using System.Net;
using System.Text;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.Resilience;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class PvgisAnnualClimateDataImportServiceTests
{
    [Fact]
    public async Task ImportAsyncStoresAnnualClimateDataFromPvgisTmy()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.OK, CreateValidTmyPayload()));

        var annualRepository = new AnnualClimateDataRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();

        var service = CreateService(
            handler,
            new PvgisApiOptions
            {
                TimeoutSeconds = 5,
                MaxRetryAttempts = 0,
                InitialRetryDelayMilliseconds = 1,
                CircuitBreakerFailureThreshold = 5
            },
            annualRepository: annualRepository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(
            climateZoneId: 1,
            CreateRequest(year: 2021));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(8760, result.Value.HourlyRecordsImported);
        Assert.Equal(2021, result.Value.Year);
        Assert.Contains("PVGIS TMY", result.Value.SourceFileName, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("DryBulbTemperature", result.Value.ImportedFields);
        Assert.Contains("DirectSolarRadiation", result.Value.ImportedFields);
        Assert.Contains("DiffuseSolarRadiation", result.Value.ImportedFields);
        Assert.Contains("RelativeHumidity", result.Value.ImportedFields);
        Assert.Contains("AtmosphericPressure", result.Value.ImportedFields);
        Assert.Contains("WindSpeed", result.Value.ImportedFields);
        Assert.Contains("WindDirection", result.Value.ImportedFields);
        Assert.Contains("HorizontalInfraredRadiation", result.Value.ImportedFields);

        Assert.NotNull(annualRepository.StoredAnnualData);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var storedHours = annualRepository.StoredAnnualData.HourlyData
            .OrderBy(hour => hour.HourOfYear)
            .ToArray();

        Assert.Equal(8760, storedHours.Length);
        Assert.Equal(0, storedHours.First().HourOfYear);
        Assert.Equal(8759, storedHours.Last().HourOfYear);
        Assert.Equal(Enumerable.Range(0, 8760), storedHours.Select(hour => hour.HourOfYear));

        var firstHour = storedHours.First();

        Assert.Equal(25, firstHour.DryBulbTemperature);
        Assert.Equal(500, firstHour.DirectSolarRadiation);
        Assert.Equal(120, firstHour.DiffuseSolarRadiation);
        Assert.Equal(50, firstHour.RelativeHumidityPercent);
        Assert.Equal(101325, firstHour.AtmosphericPressurePa);
        Assert.Equal(2.5, firstHour.WindSpeedMPerS);
        Assert.Equal(180, firstHour.WindDirectionDegrees);
        Assert.Equal(300, firstHour.HorizontalInfraredRadiationWPerM2);
    }

    [Fact]
    public async Task ImportAsyncSortsPvgisRowsByTimestampIntoJanDec8760Profile()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.OK, CreateEncodedTmyPayload(reverseRows: true)));

        var annualRepository = new AnnualClimateDataRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();

        var service = CreateService(
            handler,
            new PvgisApiOptions
            {
                TimeoutSeconds = 5,
                MaxRetryAttempts = 0,
                InitialRetryDelayMilliseconds = 1,
                CircuitBreakerFailureThreshold = 5
            },
            annualRepository: annualRepository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(
            climateZoneId: 1,
            CreateRequest(year: 2021));

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(annualRepository.StoredAnnualData);

        var storedHours = annualRepository.StoredAnnualData.HourlyData
            .OrderBy(hour => hour.HourOfYear)
            .ToArray();

        Assert.Equal(8760, storedHours.Length);
        Assert.Equal(Enumerable.Range(0, 8760), storedHours.Select(hour => hour.HourOfYear));

        var jan1Hour0 = storedHours[0];
        var feb28Hour23 = storedHours[1415];
        var mar1Hour0 = storedHours[1416];
        var dec31Hour23 = storedHours[8759];

        Assert.Equal(1, jan1Hour0.DryBulbTemperature);
        Assert.Equal(1, jan1Hour0.DirectSolarRadiation);
        Assert.Equal(0, jan1Hour0.DiffuseSolarRadiation);

        Assert.Equal(2, feb28Hour23.DryBulbTemperature);
        Assert.Equal(28, feb28Hour23.DirectSolarRadiation);
        Assert.Equal(23, feb28Hour23.DiffuseSolarRadiation);

        Assert.Equal(3, mar1Hour0.DryBulbTemperature);
        Assert.Equal(1, mar1Hour0.DirectSolarRadiation);
        Assert.Equal(0, mar1Hour0.DiffuseSolarRadiation);

        Assert.Equal(12, dec31Hour23.DryBulbTemperature);
        Assert.Equal(31, dec31Hour23.DirectSolarRadiation);
        Assert.Equal(23, dec31Hour23.DiffuseSolarRadiation);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ImportAsyncRejectsPvgisTmyWhenHourlyCountIsNot8760()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.OK, CreateValidTmyPayload(hourCount: 8759)));

        var annualRepository = new AnnualClimateDataRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();

        var service = CreateService(
            handler,
            new PvgisApiOptions
            {
                TimeoutSeconds = 5,
                MaxRetryAttempts = 0,
                InitialRetryDelayMilliseconds = 1,
                CircuitBreakerFailureThreshold = 5
            },
            annualRepository: annualRepository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(
            climateZoneId: 1,
            CreateRequest(year: 2021));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("8760", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(annualRepository.StoredAnnualData);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ImportAsyncNormalizesNegativeRadiationAndOutOfRangeOptionalFields()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.OK, CreateTmyPayload(
                year: 2021,
                hourCount: 8760,
                rowFactory: timestamp => CreateOutOfRangeTmyRow(timestamp))));

        var annualRepository = new AnnualClimateDataRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();

        var service = CreateService(
            handler,
            new PvgisApiOptions
            {
                TimeoutSeconds = 5,
                MaxRetryAttempts = 0,
                InitialRetryDelayMilliseconds = 1,
                CircuitBreakerFailureThreshold = 5
            },
            annualRepository: annualRepository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(
            climateZoneId: 1,
            CreateRequest(year: 2021));

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(annualRepository.StoredAnnualData);

        var firstHour = annualRepository.StoredAnnualData.HourlyData
            .OrderBy(hour => hour.HourOfYear)
            .First();

        Assert.Equal(0, firstHour.DirectSolarRadiation);
        Assert.Equal(0, firstHour.DiffuseSolarRadiation);
        Assert.Null(firstHour.RelativeHumidityPercent);
        Assert.Null(firstHour.AtmosphericPressurePa);
        Assert.Null(firstHour.WindSpeedMPerS);
        Assert.Null(firstHour.WindDirectionDegrees);
        Assert.Null(firstHour.HorizontalInfraredRadiationWPerM2);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ImportAsyncReturnsValidationWhenPvgisPayloadHasNoHourlyRows()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.OK, "{\"outputs\":{\"tmy_hourly\":[]}}"));

        var annualRepository = new AnnualClimateDataRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();

        var service = CreateService(
            handler,
            new PvgisApiOptions
            {
                TimeoutSeconds = 5,
                MaxRetryAttempts = 0,
                InitialRetryDelayMilliseconds = 1,
                CircuitBreakerFailureThreshold = 5
            },
            annualRepository: annualRepository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(
            climateZoneId: 1,
            CreateRequest(year: 2021));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("no hourly", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(annualRepository.StoredAnnualData);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ImportAsyncRetriesTransientPvgisFailures()
    {
        var handler = new SequenceHttpMessageHandler(
            _ => CreateResponse(HttpStatusCode.ServiceUnavailable, "unavailable"),
            _ => CreateResponse(HttpStatusCode.TooManyRequests, "retry"),
            _ => CreateResponse(HttpStatusCode.OK, CreateValidTmyPayload()));

        var service = CreateService(handler, new PvgisApiOptions
        {
            TimeoutSeconds = 5,
            MaxRetryAttempts = 2,
            InitialRetryDelayMilliseconds = 1,
            CircuitBreakerFailureThreshold = 5
        });

        var result = await service.ImportAsync(1, CreateRequest(year: 2021));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task ImportAsyncOpensCircuitBreakerAfterRepeatedTransientFailures()
    {
        var handler = new SequenceHttpMessageHandler(_ => CreateResponse(HttpStatusCode.ServiceUnavailable, "unavailable"));
        var executor = new ResilientOperationExecutor();
        var options = new PvgisApiOptions
        {
            TimeoutSeconds = 5,
            MaxRetryAttempts = 0,
            InitialRetryDelayMilliseconds = 1,
            CircuitBreakerFailureThreshold = 2,
            CircuitBreakerBreakDurationSeconds = 60
        };

        var firstService = CreateService(handler, options, executor);
        var secondService = CreateService(handler, options, executor);
        var thirdService = CreateService(handler, options, executor);

        var first = await firstService.ImportAsync(1, CreateRequest(year: 2021));
        var second = await secondService.ImportAsync(1, CreateRequest(year: 2021));
        var third = await thirdService.ImportAsync(1, CreateRequest(year: 2021));

        Assert.True(first.IsFailure);
        Assert.True(second.IsFailure);
        Assert.True(third.IsFailure);
        Assert.Equal(2, handler.CallCount);
        Assert.Contains("temporarily unavailable", third.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static PvgisAnnualClimateDataImportService CreateService(
        HttpMessageHandler handler,
        PvgisApiOptions options,
        ResilientOperationExecutor? executor = null,
        AnnualClimateDataRepositoryStub? annualRepository = null,
        UnitOfWorkStub? unitOfWork = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://re.jrc.ec.europa.eu/api/")
        };

        return new PvgisAnnualClimateDataImportService(
            httpClient,
            new ClimateZoneRepositoryStub(CreateClimateZone()),
            annualRepository ?? new AnnualClimateDataRepositoryStub(),
            unitOfWork ?? new UnitOfWorkStub(),
            Options.Create(options),
            executor ?? new ResilientOperationExecutor(),
            NullLogger<PvgisAnnualClimateDataImportService>.Instance);
    }

    private static ClimateZone CreateClimateZone() =>
        ClimateZone.Create(
            "Test climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-12).Value).Value;

    private static ImportPvgisWeatherRequest CreateRequest(int year) =>
        new()
        {
            Year = year,
            Latitude = 41.3111,
            Longitude = 69.2797,
            UseHorizon = true
        };

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

    private static string CreateValidTmyPayload(int year = 2021, int hourCount = 8760) =>
        CreateTmyPayload(
            year,
            hourCount,
            timestamp => CreateStandardTmyRow(timestamp));

    private static string CreateEncodedTmyPayload(bool reverseRows)
    {
        var rows = CreateTmyRows(
            year: 2021,
            hourCount: 8760,
            rowFactory: timestamp => CreateEncodedTmyRow(timestamp));

        if (reverseRows)
            rows = rows.Reverse().ToArray();

        return $"{{\"outputs\":{{\"tmy_hourly\":[{string.Join(",", rows)}]}}}}";
    }

    private static string CreateTmyPayload(
        int year,
        int hourCount,
        Func<DateTime, string> rowFactory)
    {
        var rows = CreateTmyRows(year, hourCount, rowFactory);
        return $"{{\"outputs\":{{\"tmy_hourly\":[{string.Join(",", rows)}]}}}}";
    }

    private static string[] CreateTmyRows(
        int year,
        int hourCount,
        Func<DateTime, string> rowFactory)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return Enumerable.Range(0, hourCount)
            .Select(hour => rowFactory(start.AddHours(hour)))
            .ToArray();
    }

    private static string CreateStandardTmyRow(DateTime timestamp)
    {
        var time = timestamp.ToString("yyyyMMdd:HHmm", CultureInfo.InvariantCulture);

        return $$"""
        {
          "time(UTC)":"{{time}}",
          "T2m":25.0,
          "RH":50.0,
          "Gb(n)":500.0,
          "Gd(h)":120.0,
          "IR(h)":300.0,
          "WS10m":2.5,
          "WD10m":180.0,
          "SP":101325.0
        }
        """;
    }

    private static string CreateEncodedTmyRow(DateTime timestamp)
    {
        var time = timestamp.ToString("yyyyMMdd:HHmm", CultureInfo.InvariantCulture);

        return $$"""
        {
          "time(UTC)":"{{time}}",
          "T2m":{{timestamp.Month.ToString(CultureInfo.InvariantCulture)}}.0,
          "RH":50.0,
          "Gb(n)":{{timestamp.Day.ToString(CultureInfo.InvariantCulture)}}.0,
          "Gd(h)":{{timestamp.Hour.ToString(CultureInfo.InvariantCulture)}}.0,
          "IR(h)":300.0,
          "WS10m":2.5,
          "WD10m":180.0,
          "SP":101325.0
        }
        """;
    }

    private static string CreateOutOfRangeTmyRow(DateTime timestamp)
    {
        var time = timestamp.ToString("yyyyMMdd:HHmm", CultureInfo.InvariantCulture);

        if (timestamp.Month == 1 &&
            timestamp.Day == 1 &&
            timestamp.Hour == 0)
        {
            return $$"""
            {
              "time(UTC)":"{{time}}",
              "T2m":25.0,
              "RH":150.0,
              "Gb(n)":-20.0,
              "Gd(h)":-10.0,
              "IR(h)":1200.0,
              "WS10m":150.0,
              "WD10m":400.0,
              "SP":1000.0
            }
            """;
        }

        return CreateStandardTmyRow(timestamp);
    }

    private sealed class ClimateZoneRepositoryStub : IClimateZoneRepository
    {
        private readonly ClimateZone _zone;

        public ClimateZoneRepositoryStub(ClimateZone zone)
        {
            _zone = zone;
        }

        public Task<ClimateZone?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ClimateZone?>(_zone);
    }

    private sealed class AnnualClimateDataRepositoryStub : IAnnualClimateDataRepository
    {
        public AnnualClimateData? StoredAnnualData { get; private set; }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(StoredAnnualData);

        public Task ReplaceForClimateZoneAsync(
            AnnualClimateData annualClimateData,
            CancellationToken cancellationToken = default)
        {
            StoredAnnualData = annualClimateData;
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _responseFactory;

        public SequenceHttpMessageHandler(params Func<int, HttpResponseMessage>[] responses)
        {
            _responseFactory = attempt =>
            {
                var index = Math.Min(attempt - 1, responses.Length - 1);
                return responses[index](attempt);
            };
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responseFactory(CallCount));
        }
    }
}