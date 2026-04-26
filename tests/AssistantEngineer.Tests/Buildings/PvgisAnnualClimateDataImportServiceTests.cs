using System.Net;
using System.Text;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Resilience;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class PvgisAnnualClimateDataImportServiceTests
{
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

        var result = await service.ImportAsync(1, CreateRequest());

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

        var first = await firstService.ImportAsync(1, CreateRequest());
        var second = await secondService.ImportAsync(1, CreateRequest());
        var third = await thirdService.ImportAsync(1, CreateRequest());

        Assert.True(first.IsFailure);
        Assert.True(second.IsFailure);
        Assert.True(third.IsFailure);
        Assert.Equal(2, handler.CallCount);
        Assert.Contains("temporarily unavailable", third.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static PvgisAnnualClimateDataImportService CreateService(
        HttpMessageHandler handler,
        PvgisApiOptions options,
        ResilientOperationExecutor? executor = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://re.jrc.ec.europa.eu/api/")
        };

        return new PvgisAnnualClimateDataImportService(
            httpClient,
            new ClimateZoneRepositoryStub(CreateClimateZone()),
            new AnnualClimateDataRepositoryStub(),
            new UnitOfWorkStub(),
            Options.Create(options),
            executor ?? new ResilientOperationExecutor(),
            NullLogger<PvgisAnnualClimateDataImportService>.Instance);
    }

    private static ClimateZone CreateClimateZone() =>
        ClimateZone.Create(
            "Test climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-12).Value).Value;

    private static ImportPvgisWeatherRequest CreateRequest() =>
        new()
        {
            Year = 2020,
            Latitude = 41.3111,
            Longitude = 69.2797,
            UseHorizon = true
        };

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

    private static string CreateValidTmyPayload()
    {
        var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = Enumerable.Range(0, 8760).Select(hour =>
        {
            var timestamp = start.AddHours(hour).ToString("yyyyMMdd:HHmm");
            return $"{{\"time(UTC)\":\"{timestamp}\",\"T2m\":25.0,\"RH\":50.0,\"Gb(n)\":500.0,\"Gd(h)\":120.0,\"IR(h)\":300.0,\"WS10m\":2.5,\"WD10m\":180.0,\"SP\":101325.0}}";
        });

        return $"{{\"outputs\":{{\"tmy_hourly\":[{string.Join(",", rows)}]}}}}";
    }

    private sealed class ClimateZoneRepositoryStub : IClimateZoneRepository
    {
        private readonly ClimateZone _zone;

        public ClimateZoneRepositoryStub(ClimateZone zone)
        {
            _zone = zone;
        }

        public Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClimateZone?>(_zone);
    }

    private sealed class AnnualClimateDataRepositoryStub : IAnnualClimateDataRepository
    {
        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(null);

        public Task ReplaceForClimateZoneAsync(
            AnnualClimateData annualClimateData,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
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
