using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tools.OwnershipBackfill.Evidence;

public interface IOwnershipBackfillEvidenceWriter
{
    Task WriteAsync(
        OwnershipBackfillDryRunResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
