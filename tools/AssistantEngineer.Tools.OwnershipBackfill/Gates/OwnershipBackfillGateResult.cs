namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillGateResult
{
    public bool Passed { get; init; }
    public required IReadOnlyList<OwnershipBackfillGateFinding> Findings { get; init; }
    public required IReadOnlyDictionary<string, string> Metrics { get; init; }
    public required string Summary { get; init; }
    public required string RunId { get; init; }
    public required IReadOnlyDictionary<string, string> Thresholds { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
