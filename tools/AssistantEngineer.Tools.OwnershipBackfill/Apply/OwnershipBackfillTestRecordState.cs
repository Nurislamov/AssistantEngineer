namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillTestRecordState
{
    public required string RecordType { get; init; }
    public required string RecordId { get; init; }
    public int? ProjectId { get; init; }
    public int? BuildingId { get; init; }
    public int? OrganizationId { get; init; }
    public int? OwnerUserId { get; init; }
    public bool SimulateFailure { get; init; }
}

