namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillPlanResult
{
    public bool Succeeded { get; init; }
    public required string RunId { get; init; }
    public required string PlanId { get; init; }
    public required string PlanHash { get; init; }
    public required string RulesetVersion { get; init; }
    public required IReadOnlyList<OwnershipBackfillPlannedRecord> PlannedRecords { get; init; }
    public required OwnershipBackfillApplySummaryDraft SummaryDraft { get; init; }
    public required IReadOnlyList<OwnershipBackfillPlanFinding> Findings { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
