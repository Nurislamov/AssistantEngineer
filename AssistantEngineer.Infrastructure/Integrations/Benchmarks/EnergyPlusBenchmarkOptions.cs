namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed class EnergyPlusBenchmarkOptions
{
    public string ExecutablePath { get; set; } = "energyplus";
    public bool UseDocker { get; set; } = true;
    public string DockerUri { get; set; } = string.Empty;
    public string DockerImage { get; set; } = "assistant-engineer-energyplus:24.1";
    public int MaxCapturedLogCharacters { get; set; } = 64 * 1024;
    public string ArtifactRootDirectory { get; set; } = string.Empty;
    public int ExecutionTimeoutSeconds { get; set; } = 900;
    public int MaxRetryAttempts { get; set; } = 1;
    public int InitialRetryDelayMilliseconds { get; set; } = 1_000;
    public int CircuitBreakerFailureThreshold { get; set; } = 3;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 60;
}
