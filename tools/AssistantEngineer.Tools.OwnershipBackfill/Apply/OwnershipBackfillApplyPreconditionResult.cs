namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyPreconditionResult
{
    public bool Passed { get; init; }
    public required IReadOnlyList<OwnershipBackfillApplyPreconditionFinding> Findings { get; init; }
}
