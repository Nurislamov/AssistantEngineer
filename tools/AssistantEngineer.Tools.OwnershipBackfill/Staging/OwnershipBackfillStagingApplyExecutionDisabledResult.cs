namespace AssistantEngineer.Tools.OwnershipBackfill.Staging;

public sealed class OwnershipBackfillStagingApplyExecutionDisabledResult
{
    public bool Executed { get; init; } = false;
    public required string Message { get; init; }
    public required IReadOnlyList<string> NonClaims { get; init; }
}
