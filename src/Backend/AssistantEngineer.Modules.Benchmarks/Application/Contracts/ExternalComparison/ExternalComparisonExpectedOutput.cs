namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public sealed class ExternalComparisonExpectedOutput
{
    public string OutputPath { get; init; } = string.Empty;
    public string Format { get; init; } = "json";
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
}
