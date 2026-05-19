namespace AssistantEngineer.Tools.OwnershipBackfill.Plan;

public sealed class OwnershipBackfillPlannedRecord
{
    public required string RecordType { get; init; }
    public required string RecordId { get; init; }
    public int? CurrentProjectId { get; init; }
    public int? CurrentBuildingId { get; init; }
    public int? CurrentOrganizationId { get; init; }
    public int? CurrentOwnerUserId { get; init; }
    public int? ProposedProjectId { get; init; }
    public int? ProposedBuildingId { get; init; }
    public int? ProposedOrganizationId { get; init; }
    public int? ProposedOwnerUserId { get; init; }
    public required string Reason { get; init; }
    public required string SourceEvidence { get; init; }
    public required string DeterministicRecordHash { get; init; }
}
