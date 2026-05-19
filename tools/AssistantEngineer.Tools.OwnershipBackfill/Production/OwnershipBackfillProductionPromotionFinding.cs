namespace AssistantEngineer.Tools.OwnershipBackfill.Production;

public sealed class OwnershipBackfillProductionPromotionFinding
{
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public string? Category { get; init; }
    public string? Expected { get; init; }
    public string? Actual { get; init; }
}
