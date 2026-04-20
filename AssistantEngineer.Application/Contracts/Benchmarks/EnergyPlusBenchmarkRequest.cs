namespace AssistantEngineer.Application.Contracts.Benchmarks;

public sealed class EnergyPlusBenchmarkRequest
{
    public string ModelPath { get; init; } = string.Empty;
    public string WeatherFilePath { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public IReadOnlyList<string> AdditionalArguments { get; init; } = [];
}
