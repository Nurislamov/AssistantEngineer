namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public sealed class OwnershipBackfillPreviousValueSnapshot
{
    public required string RecordType { get; init; }
    public required string RecordId { get; init; }
    public int? PreviousProjectId { get; init; }
    public int? PreviousBuildingId { get; init; }
    public int? PreviousOrganizationId { get; init; }
    public int? PreviousOwnerUserId { get; init; }
}
