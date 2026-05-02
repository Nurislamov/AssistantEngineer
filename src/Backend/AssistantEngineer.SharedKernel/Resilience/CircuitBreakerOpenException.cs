namespace AssistantEngineer.SharedKernel.Resilience;

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string integrationName, TimeSpan retryAfter)
        : base($"Integration '{integrationName}' is temporarily unavailable because its circuit breaker is open.")
    {
        IntegrationName = integrationName;
        RetryAfter = retryAfter;
    }

    public string IntegrationName { get; }

    public TimeSpan RetryAfter { get; }
}
