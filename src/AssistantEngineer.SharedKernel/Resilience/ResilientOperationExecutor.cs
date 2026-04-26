using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.SharedKernel.Resilience;

public sealed class ResilientOperationExecutor
{
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new(StringComparer.OrdinalIgnoreCase);

    public async Task<T> ExecuteAsync<T>(
        string integrationName,
        ResilientOperationSettings settings,
        Func<CancellationToken, Task<T>> operation,
        ILogger logger,
        Func<Exception, bool>? isTransientException = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(integrationName);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(logger);

        var circuit = _circuits.GetOrAdd(integrationName, static _ => new CircuitState());
        if (circuit.TryGetRetryAfter(out var retryAfter))
        {
            logger.LogWarning(
                "Skipping integration {IntegrationName} because the circuit breaker is open for another {RetryAfter}.",
                integrationName,
                retryAfter);
            throw new CircuitBreakerOpenException(integrationName, retryAfter);
        }

        Exception? lastException = null;
        var attempts = settings.MaxRetryAttempts + 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(settings.Timeout);

            try
            {
                var result = await operation(timeoutCts.Token);
                circuit.Reset();
                return result;
            }
            catch (Exception exception) when (IsTransient(exception, cancellationToken, timeoutCts, isTransientException))
            {
                lastException = NormalizeTimeoutException(exception, cancellationToken, settings.Timeout);

                if (attempt >= attempts)
                    break;

                var delay = TimeSpan.FromMilliseconds(
                    settings.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                logger.LogWarning(
                    lastException,
                    "Transient failure while calling integration {IntegrationName}. Attempt {Attempt} of {AttemptCount}. Retrying in {Delay}.",
                    integrationName,
                    attempt,
                    attempts,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        circuit.RecordFailure(settings.CircuitBreakerFailureThreshold, settings.CircuitBreakerBreakDuration);

        logger.LogError(
            lastException,
            "Integration {IntegrationName} failed after {AttemptCount} attempts.",
            integrationName,
            attempts);

        throw lastException ?? new InvalidOperationException(
            $"Integration '{integrationName}' failed without an exception.");
    }

    private static bool IsTransient(
        Exception exception,
        CancellationToken callerToken,
        CancellationTokenSource timeoutCts,
        Func<Exception, bool>? isTransientException)
    {
        if (exception is OperationCanceledException && !callerToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
            return true;

        return isTransientException?.Invoke(exception) == true;
    }

    private static Exception NormalizeTimeoutException(
        Exception exception,
        CancellationToken callerToken,
        TimeSpan timeout)
    {
        if (exception is OperationCanceledException && !callerToken.IsCancellationRequested)
            return new TimeoutException($"The operation timed out after {timeout}.", exception);

        return exception;
    }

    private sealed class CircuitState
    {
        private readonly object _sync = new();
        private int _consecutiveFailures;
        private DateTimeOffset? _openedUntilUtc;

        public bool TryGetRetryAfter(out TimeSpan retryAfter)
        {
            lock (_sync)
            {
                if (!_openedUntilUtc.HasValue)
                {
                    retryAfter = default;
                    return false;
                }

                var remaining = _openedUntilUtc.Value - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    retryAfter = remaining;
                    return true;
                }

                _openedUntilUtc = null;
                _consecutiveFailures = 0;
                retryAfter = default;
                return false;
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _consecutiveFailures = 0;
                _openedUntilUtc = null;
            }
        }

        public void RecordFailure(int threshold, TimeSpan breakDuration)
        {
            lock (_sync)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures < threshold)
                    return;

                _openedUntilUtc = DateTimeOffset.UtcNow.Add(breakDuration);
                _consecutiveFailures = 0;
            }
        }
    }
}
