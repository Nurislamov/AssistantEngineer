namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public interface IOwnershipBackfillEvidenceLoader
{
    Task<OwnershipBackfillEvidenceBundle> LoadAsync(
        OwnershipBackfillGateOptions options,
        CancellationToken cancellationToken = default);
}
