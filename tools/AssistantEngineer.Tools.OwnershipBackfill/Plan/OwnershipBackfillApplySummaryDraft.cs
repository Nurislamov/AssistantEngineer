namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillApplySummaryDraft
{
    public required string PlanId { get; init; }
    public required string PlanHash { get; init; }
    public required string Mode { get; init; }
    public int TotalRecordsPlanned { get; init; }
    public int TotalRecordsSkipped { get; init; }
    public int TotalRecordsUnresolved { get; init; }
    public required IReadOnlyDictionary<string, int> PlannedByRecordType { get; init; }
    public required IReadOnlyDictionary<string, int> SkippedByReason { get; init; }
    public required IReadOnlyList<string> RequiredFutureApplyPreconditions { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
