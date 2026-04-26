namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public sealed class EnergyPlusBenchmarkRequest
{
    public string ModelArtifactId { get; init; } = string.Empty;
    public string WeatherArtifactId { get; init; } = string.Empty;
    public string? RunName { get; init; }
    public IReadOnlyList<string> AdditionalArguments { get; init; } = [];
}
