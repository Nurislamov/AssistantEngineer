namespace AssistantEngineer.Tools.OwnershipBackfill.Readiness;

public sealed class OwnershipBackfillApplyReadinessResult
{
    public bool Passed { get; init; }
    public required string ReadinessId { get; init; }
    public required string ApplyInputHash { get; init; }
    public required string PlanHash { get; init; }
    public required string SignoffPlanHash { get; init; }
    public required string RulesetVersion { get; init; }
    public required IReadOnlyList<OwnershipBackfillApplyReadinessFinding> Findings { get; init; }
    public required IReadOnlyDictionary<string, string> Metrics { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}

