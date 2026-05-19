namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public sealed class OwnershipBackfillRecordTypeMetrics
{
    public required string RecordType { get; init; }
    public int TotalRecords { get; init; }
    public int ResolvableRecords { get; init; }
    public int UnresolvedRecords { get; init; }
    public int AmbiguousRecords { get; init; }
    public double ResolvableRate { get; init; }
    public required IReadOnlyDictionary<string, int> UnresolvedByReason { get; init; }
}
