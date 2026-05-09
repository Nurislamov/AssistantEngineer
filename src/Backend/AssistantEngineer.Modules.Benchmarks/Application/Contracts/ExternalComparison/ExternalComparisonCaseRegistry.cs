namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public sealed class ExternalComparisonCaseRegistry
{
    public string RegistryName { get; init; } = string.Empty;
    public string Version { get; init; } = "v1";
    public string Purpose { get; init; } = string.Empty;
    public IReadOnlyList<ExternalComparisonCase> Cases { get; init; } = [];
}
