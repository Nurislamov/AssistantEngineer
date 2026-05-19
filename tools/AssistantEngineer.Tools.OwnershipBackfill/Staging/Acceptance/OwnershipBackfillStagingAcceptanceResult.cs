namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingAcceptanceResult
{
    public bool Accepted { get; init; }
    public required string AcceptanceId { get; init; }
    public required string StagingRunHash { get; init; }
    public required string ApplyInputHash { get; init; }
    public required string PlanHash { get; init; }
    public required string SignoffId { get; init; }
    public required string ReadinessId { get; init; }
    public required string OperatorId { get; init; }
    public required string StagingChangeId { get; init; }
    public required IReadOnlyList<OwnershipBackfillStagingAcceptanceFinding> Findings { get; init; }
    public required IReadOnlyDictionary<string, string> Metrics { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
