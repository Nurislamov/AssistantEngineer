namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillPlanFinding
{
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public string? RecordType { get; init; }
    public string? RecordId { get; init; }
}
