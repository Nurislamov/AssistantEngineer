namespace AssistantEngineer.Tools.OwnershipBackfill.Readiness;

public sealed class OwnershipBackfillApplyReadinessValidationResult
{
    public required OwnershipBackfillApplyReadinessResult Result { get; init; }
    public int ExitCode { get; init; }
}

