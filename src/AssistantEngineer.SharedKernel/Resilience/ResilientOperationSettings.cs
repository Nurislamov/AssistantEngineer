namespace AssistantEngineer.SharedKernel.Resilience;

public sealed record ResilientOperationSettings(
    TimeSpan Timeout,
    int MaxRetryAttempts,
    TimeSpan InitialRetryDelay,
    int CircuitBreakerFailureThreshold,
    TimeSpan CircuitBreakerBreakDuration);
