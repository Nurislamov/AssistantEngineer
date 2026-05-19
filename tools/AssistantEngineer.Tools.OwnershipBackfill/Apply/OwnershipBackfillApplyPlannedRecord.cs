namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class OwnershipBackfillApplyPlannedRecord
{
    public required string RecordType { get; init; }
    public required string RecordId { get; init; }
    public int? ProposedProjectId { get; init; }
    public int? ProposedBuildingId { get; init; }
    public int? ProposedOrganizationId { get; init; }
    public int? ProposedOwnerUserId { get; init; }
    public required string Reason { get; init; }
}
