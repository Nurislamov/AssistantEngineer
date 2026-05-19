namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public sealed class OwnershipBackfillUnresolvedRecord
{
    public required string RecordType { get; init; }
    public required string RecordId { get; init; }
    public required string Reason { get; init; }
    public int? CandidateProjectId { get; init; }
    public int? CandidateBuildingId { get; init; }
    public int? CandidateOrganizationId { get; init; }
    public string? Notes { get; init; }
}
