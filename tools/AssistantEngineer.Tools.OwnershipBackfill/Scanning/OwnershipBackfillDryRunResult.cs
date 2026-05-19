using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Scanning;

public sealed class OwnershipBackfillDryRunResult
{
    public required OwnershipBackfillDryRunSummary Summary { get; init; }
    public required IReadOnlyList<OwnershipBackfillUnresolvedRecord> UnresolvedRecords { get; init; }
    public required IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> PreviousValues { get; init; }
}
