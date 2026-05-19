namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionValidationResult
{
    public required OwnershipBackfillProductionPromotionDecision Decision { get; init; }
    public int ExitCode { get; init; }
}
