namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;

public sealed class ExternalComparisonCase
{
    public string CaseId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Workflow { get; init; } = string.Empty;
    public string ValidationAnchor { get; init; } = string.Empty;
    public string ModelInputPath { get; init; } = string.Empty;
    public ExternalComparisonExpectedOutput? ExpectedOutput { get; init; }
    public ExternalComparisonTolerance? Tolerance { get; init; }
    public ExternalComparisonProvenance? Provenance { get; init; }
    public ExternalComparisonStatus Status { get; init; }
    public string ClaimBoundary { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = [];
}
