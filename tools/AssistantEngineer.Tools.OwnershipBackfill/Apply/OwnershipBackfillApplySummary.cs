namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplySummary
{
    public required string RunId { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
    public required string Mode { get; init; }
    public int TotalRecordsPlanned { get; init; }
    public int TotalRecordsUpdated { get; init; }
    public int TotalRecordsSkipped { get; init; }
    public int TotalRecordsFailed { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
