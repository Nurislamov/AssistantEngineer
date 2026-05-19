using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Scanning;

public interface IOwnershipBackfillDryRunScanner
{
    Task<OwnershipBackfillDryRunResult> ScanAsync(
        OwnershipBackfillOptions options,
        CancellationToken cancellationToken = default);
}
