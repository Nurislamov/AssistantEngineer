namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public sealed class OwnershipBackfillDryRunSummary
{
    public required string RunId { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
    public required string Mode { get; init; }
    public int TotalRecordsScanned { get; init; }
    public int TotalRecordsResolvable { get; init; }
    public int TotalRecordsUnresolved { get; init; }
    public required IReadOnlyDictionary<string, int> UnresolvedByReason { get; init; }
    public required IReadOnlyList<OwnershipBackfillRecordTypeMetrics> RecordTypeMetrics { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
