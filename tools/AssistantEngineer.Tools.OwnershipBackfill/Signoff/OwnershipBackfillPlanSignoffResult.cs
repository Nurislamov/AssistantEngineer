namespace AssistantEngineer.Tools.OwnershipBackfill.Signoff;

public sealed class OwnershipBackfillPlanSignoffResult
{
    public bool Succeeded { get; init; }
    public required string SignoffId { get; init; }
    public required string PlanId { get; init; }
    public required string PlanHash { get; init; }
    public required string Reviewer { get; init; }
    public required string Ticket { get; init; }
    public DateTimeOffset SignedAtUtc { get; init; }
    public DateTimeOffset? ExpiresAtUtc { get; init; }
    public required IReadOnlyList<OwnershipBackfillPlanSignoffFinding> Findings { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
