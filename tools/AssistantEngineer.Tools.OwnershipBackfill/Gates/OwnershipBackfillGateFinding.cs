namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public sealed class OwnershipBackfillGateFinding
{
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public string? RecordType { get; init; }
    public string? Metric { get; init; }
    public string? Expected { get; init; }
    public string? Actual { get; init; }
}
