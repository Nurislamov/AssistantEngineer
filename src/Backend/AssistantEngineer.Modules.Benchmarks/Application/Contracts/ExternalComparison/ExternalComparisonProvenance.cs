namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public sealed class ExternalComparisonProvenance
{
    public string SourceTool { get; init; } = string.Empty;
    public string SourceVersion { get; init; } = string.Empty;
    public string ArtifactPath { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
