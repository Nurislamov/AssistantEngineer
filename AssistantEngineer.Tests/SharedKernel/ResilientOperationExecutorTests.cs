using AssistantEngineer.SharedKernel.Resilience;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Tests;

public class ResilientOperationExecutorTests
{
    [Fact]
    public async Task ExecuteAsyncRetriesTransientExceptions()
    {
        var executor = new ResilientOperationExecutor();
        var attempts = 0;

        var result = await executor.ExecuteAsync(
            integrationName: "test-retry",
            settings: new ResilientOperationSettings(
                Timeout: TimeSpan.FromSeconds(5),
                MaxRetryAttempts: 2,
                InitialRetryDelay: TimeSpan.FromMilliseconds(1),
                CircuitBreakerFailureThreshold: 3,
                CircuitBreakerBreakDuration: TimeSpan.FromSeconds(10)),
            operation: _ =>
            {
                attempts++;
                if (attempts < 3)
                    throw new HttpRequestException("transient");

                return Task.FromResult(42);
            },
            logger: NullLogger.Instance,
            isTransientException: static exception => exception is HttpRequestException);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsyncOpensCircuitAfterThresholdFailures()
    {
        var executor = new ResilientOperationExecutor();
        var attempts = 0;
        var settings = new ResilientOperationSettings(
            Timeout: TimeSpan.FromSeconds(5),
            MaxRetryAttempts: 0,
            InitialRetryDelay: TimeSpan.FromMilliseconds(1),
            CircuitBreakerFailureThreshold: 2,
            CircuitBreakerBreakDuration: TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<HttpRequestException>(() => executor.ExecuteAsync<int>(
            "test-circuit",
            settings,
            _ =>
            {
                attempts++;
                throw new HttpRequestException("transient");
            },
            NullLogger.Instance,
            static exception => exception is HttpRequestException));

        await Assert.ThrowsAsync<HttpRequestException>(() => executor.ExecuteAsync<int>(
            "test-circuit",
            settings,
            _ =>
            {
                attempts++;
                throw new HttpRequestException("transient");
            },
            NullLogger.Instance,
            static exception => exception is HttpRequestException));

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => executor.ExecuteAsync<int>(
            "test-circuit",
            settings,
            _ =>
            {
                attempts++;
                throw new HttpRequestException("transient");
            },
            NullLogger.Instance,
            static exception => exception is HttpRequestException));

        Assert.Equal(2, attempts);
    }
}
