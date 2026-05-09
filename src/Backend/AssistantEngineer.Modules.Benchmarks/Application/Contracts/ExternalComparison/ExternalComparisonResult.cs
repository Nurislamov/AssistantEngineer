namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public sealed class ExternalComparisonResult
{
    public string CaseId { get; init; } = string.Empty;
    public ExternalComparisonStatus Status { get; init; }
    public bool? PassedTolerance { get; init; }
    public IReadOnlyDictionary<string, double> ActualMetrics { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, double> ExpectedMetrics { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
