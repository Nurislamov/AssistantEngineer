using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillEvidenceBundle
{
    public required OwnershipBackfillDryRunSummary Summary { get; init; }
    public required IReadOnlyList<OwnershipBackfillUnresolvedRecord> UnresolvedRecords { get; init; }
    public required IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> PreviousValues { get; init; }
    public required IReadOnlySet<string> UnresolvedRecordPropertyNames { get; init; }
}
