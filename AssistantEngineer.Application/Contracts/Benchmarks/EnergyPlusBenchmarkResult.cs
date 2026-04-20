namespace AssistantEngineer.Application.Contracts.Benchmarks;

public sealed class EnergyPlusBenchmarkResult
{
    public bool Succeeded { get; init; }
    public int ExitCode { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
}
