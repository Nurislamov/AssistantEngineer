using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyExecutionResult
{
    public bool Succeeded { get; init; }
    public required string ExecutionId { get; init; }
    public required string Mode { get; init; }
    public int TotalRecordsPlanned { get; init; }
    public int TotalRecordsUpdated { get; init; }
    public int TotalRecordsSkipped { get; init; }
    public int TotalRecordsFailed { get; init; }
    public required IReadOnlyList<OwnershipBackfillApplyExecutionFinding> Findings { get; init; }
    public required IReadOnlyList<OwnershipBackfillPreviousValueSnapshot> PreviousValues { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}

