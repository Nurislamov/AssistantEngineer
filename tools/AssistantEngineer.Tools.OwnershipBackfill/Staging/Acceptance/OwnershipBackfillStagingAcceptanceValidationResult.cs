namespace AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

public sealed class OwnershipBackfillStagingAcceptanceValidationResult
{
    public required OwnershipBackfillStagingAcceptanceResult Result { get; init; }
    public int ExitCode { get; init; }
}
