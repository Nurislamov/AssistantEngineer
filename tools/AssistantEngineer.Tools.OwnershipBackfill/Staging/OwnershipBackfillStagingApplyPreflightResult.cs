namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class OwnershipBackfillStagingApplyPreflightResult
{
    public bool Passed { get; init; }
    public required IReadOnlyList<OwnershipBackfillStagingApplyPreflightFinding> Findings { get; init; }
    public required IReadOnlyDictionary<string, string> Metrics { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
