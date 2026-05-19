namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionDecision
{
    public bool Ready { get; init; }
    public required string DecisionId { get; init; }
    public required string DecisionStatus { get; init; }
    public required string ProductionPromotionHash { get; init; }
    public required string StagingRunHash { get; init; }
    public required string ProductionApplyInputHash { get; init; }
    public required string ProductionPlanHash { get; init; }
    public required string ProductionChangeRequestId { get; init; }
    public required IReadOnlyList<OwnershipBackfillProductionPromotionFinding> Findings { get; init; }
    public required IReadOnlyDictionary<string, string> Metrics { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
